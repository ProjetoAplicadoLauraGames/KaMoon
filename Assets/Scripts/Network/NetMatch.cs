using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Autoridade da partida em rede (arquitetura cliente-servidor sobre Mirror).
///
/// Vive no objeto do NetworkManager (DontDestroyOnLoad) e é instalado em runtime,
/// por isso não obriga a reeditar cenas nem prefabs:
/// - no servidor (host) corre o relógio, a energia e as regras dos puzzles;
/// - os clientes enviam pose (20 Hz) e pedidos de ação;
/// - o servidor devolve o estado autoritativo (10 Hz) e reencaminha as poses.
///
/// O modo local (split-screen) não passa por aqui: todos os sistemas de gameplay
/// mantêm o comportamento original quando NetMatch.EmPartida é falso.
/// </summary>
[DisallowMultipleComponent]
public class NetMatch : MonoBehaviour
{
    private const float FrequenciaPose = 20f;
    private const float FrequenciaEstado = 10f;

    public static NetMatch Instance { get; private set; }

    public static bool Ligado => NetworkServer.active || NetworkClient.active;

    /// <summary>Verdadeiro apenas numa partida em rede com a cena de jogo ativa.</summary>
    public static bool EmPartida =>
        Instance != null && Ligado && GameSession.multiplayer &&
        SceneManager.GetActiveScene().name == LobbyPlayer.CenaPartida;

    // ---- Estado replicado (lido pelos sistemas de gameplay) ----
    public static float TempoRestante { get; private set; } = 60f;
    public static float BateriaUm { get; private set; } = 1f;
    public static float BateriaDois { get; private set; } = 1f;
    public static int LanternaDono { get; private set; }
    public static bool KeypadResolvido { get; private set; }
    public static bool CaboResolvido { get; private set; }
    public static bool Derrota { get; private set; }

    public static float Bateria(int jogador) => jogador == 2 ? BateriaDois : BateriaUm;

    // ---- Estado autoritativo do servidor ----
    private bool partidaAtiva;
    private float tempo;
    private readonly float[] bateria = { 1f, 1f };
    private readonly bool[] falando = { false, false };
    private int lanternaDono;
    private bool keypadResolvido;
    private bool caboResolvido;
    private bool derrota;
    private float proximoEnvioEstado;
    private bool estadoSujo;
    private readonly Dictionary<int, int> salaPorConexao = new Dictionary<int, int>();
    private readonly HashSet<string> objetosRemovidos = new HashSet<string>();

    // ---- Lado cliente ----
    private Transform avatarLocal;
    private Transform avatarRemoto;
    private float proximaPose;
    private Vector3 alvoPosicao;
    private float alvoYaw;
    private float alvoVelocidade;
    private Animator animadorRemoto;
    private bool animadorPronto;
    private bool temAlvo;
    private bool derrotaAplicada;
    private LocalVoiceChat voz;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Instalar()
    {
        if (NetworkManager.singleton == null) return;
        if (NetworkManager.singleton.GetComponent<NetMatch>() == null)
            NetworkManager.singleton.gameObject.AddComponent<NetMatch>();
    }

    /// <summary>
    /// Reinstala os componentes de rede no NetworkManager atual.
    /// Necessário quando o manager antigo é destruído ao voltar ao menu
    /// (o atributo RuntimeInitializeOnLoadMethod só corre uma vez por sessão).
    /// </summary>
    internal static void InstalarNoManager()
    {
        Instalar();
        LobbyDiscovery.Instalar();
        MatchGuard.Instalar();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        SceneManager.sceneLoaded += OnCenaCarregada;

        NetworkServer.RegisterHandler<PoseJogadorMessage>(OnPoseServidor);
        NetworkServer.RegisterHandler<AcaoJogadorMessage>(OnAcaoServidor);
        NetworkClient.RegisterHandler<PoseRemotaMessage>(OnPoseRemota);
        NetworkClient.RegisterHandler<EstadoPartidaMessage>(OnEstadoRecebido);
        NetworkClient.RegisterHandler<ObjetoRemovidoMessage>(OnObjetoRemovido);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnCenaCarregada;
        if (Instance == this) Instance = null;
    }

    private void OnCenaCarregada(Scene cena, LoadSceneMode modo)
    {
        LimparDuplicados();

        if (cena.name != LobbyPlayer.CenaPartida)
        {
            ReporTudo();
            return;
        }

        ReporPartida();
        LigarAvatares();
        if (NetworkServer.active) IniciarServidor();
    }

    private void Update()
    {
        if (NetworkServer.active) AtualizarServidor();

        if (EmPartida)
            AtualizarCliente();

        // O input da lanterna pertence apenas ao GestorLanternas; lê-lo também aqui
        // enviava dois pedidos por tecla e o servidor alternava duas vezes (1→2→0).
    }

    // ---------------------------------------------------------------- servidor

    private void IniciarServidor()
    {
        salaPorConexao.Clear();
        foreach (KeyValuePair<int, int> par in LobbyPlayer.SalaPorConexao)
            salaPorConexao[par.Key] = par.Value;

        tempo = TempoInicial();
        bateria[0] = 1f;
        bateria[1] = 1f;
        falando[0] = false;
        falando[1] = false;
        lanternaDono = 1;
        keypadResolvido = false;
        caboResolvido = false;
        derrota = false;
        objetosRemovidos.Clear();
        derrotaAplicada = false;
        estadoSujo = true;
        proximoEnvioEstado = 0f;
        partidaAtiva = salaPorConexao.Count > 0;
        TempoRestante = tempo;
    }

    private void AtualizarServidor()
    {
        if (!partidaAtiva) return;

        float dt = Time.deltaTime;

        if (!derrota && tempo > 0f)
        {
            tempo = Mathf.Max(0f, tempo - dt);
            if (tempo <= 0f)
            {
                derrota = true;
                estadoSujo = true;
            }
        }

        for (int i = 0; i < 2; i++)
        {
            int jogador = i + 1;
            float drenagem = 0f;
            if (lanternaDono == jogador) drenagem += DrenagemLanterna(jogador);
            if (falando[i]) drenagem += DrenagemVoz(jogador);
            if (drenagem <= 0f) continue;

            float antes = bateria[i];
            bateria[i] = Mathf.Max(0f, bateria[i] - drenagem * dt);
            if (!Mathf.Approximately(antes, bateria[i])) estadoSujo = true;
        }

        if (lanternaDono != 0 && bateria[lanternaDono - 1] <= 0f)
        {
            lanternaDono = 0;
            estadoSujo = true;
        }

        proximoEnvioEstado -= dt;
        if (estadoSujo || proximoEnvioEstado <= 0f)
        {
            proximoEnvioEstado = 1f / FrequenciaEstado;
            estadoSujo = false;
            NetworkServer.SendToAll(EstadoAtual());
        }
    }

    private void OnPoseServidor(NetworkConnectionToClient conn, PoseJogadorMessage msg)
    {
        int sala = SalaDoCliente(conn);
        if (sala == 0) return;

        falando[sala - 1] = msg.aFalar;

        NetworkServer.SendToAll(new PoseRemotaMessage
        {
            sala = (byte)sala,
            posicao = msg.posicao,
            yaw = msg.yaw,
            pitch = msg.pitch,
            velocidade = msg.velocidade
        }, Channels.Unreliable);
    }

    private void OnAcaoServidor(NetworkConnectionToClient conn, AcaoJogadorMessage msg)
    {
        if (!partidaAtiva) return;

        int sala = SalaDoCliente(conn);
        if (sala == 0) return;

        switch (msg.acao)
        {
            case NetAcao.Lanterna:
                if (bateria[sala - 1] <= 0f) return;
                lanternaDono = lanternaDono == sala ? 0 : sala;
                estadoSujo = true;
                return;

            case NetAcao.Keypad:
                if (keypadResolvido || derrota) return;
                if (CodigoKeypad() == msg.texto) keypadResolvido = true;
                else derrota = true;
                estadoSujo = true;
                return;

            case NetAcao.Cabo:
                if (caboResolvido || derrota) return;
                if (msg.indice == IndiceCaboCorreto()) caboResolvido = true;
                else derrota = true;
                estadoSujo = true;
                return;

            case NetAcao.CaboFisico:
                if (derrota) return;
                ProcessarCaboFisico(msg.texto);
                return;

            case NetAcao.Reabastecer:
                bateria[sala - 1] = 1f;
                estadoSujo = true;
                return;

            case NetAcao.ApanharItem:
                RemoverObjeto(msg.texto);
                return;
        }
    }

    private void ProcessarCaboFisico(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return;

        GameObject cabo = GameObject.Find(nome);
        if (cabo == null) return;

        if (cabo.CompareTag("WrongCable"))
        {
            derrota = true;
            estadoSujo = true;
            return;
        }

        if (!cabo.CompareTag("CorrectCable")) return;
        RemoverObjeto(nome);
    }

    /// <summary>Remove o objeto na partida inteira (cabos cortados, itens apanhados).</summary>
    private void RemoverObjeto(string nome)
    {
        if (string.IsNullOrEmpty(nome) || !objetosRemovidos.Add(nome)) return;

        NetworkServer.SendToAll(new ObjetoRemovidoMessage { nome = nome });

        GameObject obj = GameObject.Find(nome);
        if (obj != null) Destroy(obj);
    }

    private EstadoPartidaMessage EstadoAtual() => new EstadoPartidaMessage
    {
        tempo = tempo,
        bateriaUm = bateria[0],
        bateriaDois = bateria[1],
        lanternaDono = (byte)lanternaDono,
        keypadResolvido = keypadResolvido,
        caboResolvido = caboResolvido,
        derrota = derrota
    };

    private int SalaDoCliente(NetworkConnectionToClient conn)
        => conn != null && salaPorConexao.TryGetValue(conn.connectionId, out int sala) ? sala : 0;

    // ----------------------------------------------------------------- cliente

    private void AtualizarCliente()
    {
        if (NetworkClient.active && avatarLocal != null && Time.time >= proximaPose)
        {
            proximaPose = Time.time + 1f / FrequenciaPose;
            EnviarPose();
        }

        AplicarPoseRemota();
    }

    private void EnviarPose()
    {
        Camera camara = avatarLocal.GetComponentInChildren<Camera>(true);
        float pitch = 0f;
        if (camara != null)
        {
            float x = camara.transform.localEulerAngles.x;
            pitch = x > 180f ? x - 360f : x;
        }

        float velocidade = 0f;
        Rigidbody corpo = avatarLocal.GetComponent<Rigidbody>();
        if (corpo != null)
        {
            Vector3 v = corpo.linearVelocity;
            v.y = 0f;
            velocidade = v.magnitude;
        }

        NetworkClient.Send(new PoseJogadorMessage
        {
            posicao = avatarLocal.position,
            yaw = avatarLocal.eulerAngles.y,
            pitch = pitch,
            velocidade = velocidade,
            aFalar = AFalar()
        }, Channels.Unreliable);
    }

    private bool AFalar()
    {
        if (voz == null) voz = FindAnyObjectByType<LocalVoiceChat>();
        return voz != null && voz.TalkingPlayer == GameSession.room;
    }

    private void OnPoseRemota(PoseRemotaMessage msg)
    {
        if (!EmPartida) return;
        if (msg.sala == GameSession.room) return;   // eco da própria pose (host)

        alvoPosicao = msg.posicao;
        alvoYaw = msg.yaw;
        alvoVelocidade = msg.velocidade;
        temAlvo = true;
    }

    private void AplicarPoseRemota()
    {
        if (!temAlvo || avatarRemoto == null) return;

        float t = 1f - Mathf.Exp(-12f * Time.deltaTime);
        avatarRemoto.position = Vector3.Lerp(avatarRemoto.position, alvoPosicao, t);
        avatarRemoto.rotation = Quaternion.Slerp(avatarRemoto.rotation, Quaternion.Euler(0f, alvoYaw, 0f), t);

        // O CapsuleMovement remoto esta desligado; a marcha vem da velocidade recebida.
        if (animadorPronto && animadorRemoto != null)
            animadorRemoto.SetFloat("Speed", alvoVelocidade);
    }

    private void OnEstadoRecebido(EstadoPartidaMessage msg)
    {
        TempoRestante = msg.tempo;
        BateriaUm = msg.bateriaUm;
        BateriaDois = msg.bateriaDois;
        LanternaDono = msg.lanternaDono;
        AplicarLuzes(msg.lanternaDono);
        KeypadResolvido = msg.keypadResolvido;
        CaboResolvido = msg.caboResolvido;
        Derrota = msg.derrota;

        if (msg.derrota && !derrotaAplicada)
        {
            derrotaAplicada = true;
            GameOverManager manager = FindAnyObjectByType<GameOverManager>();
            if (manager != null) manager.GameOver();
        }
    }

    private void OnObjetoRemovido(ObjetoRemovidoMessage msg)
    {
        if (string.IsNullOrEmpty(msg.nome)) return;

        GameObject obj = GameObject.Find(msg.nome);
        if (obj != null) Destroy(obj);
    }

    // -------------------------------------------------------------- API pública

    public static void PedirLanterna() => Enviar(new AcaoJogadorMessage { acao = NetAcao.Lanterna });

    public static void PedirKeypad(string codigo)
        => Enviar(new AcaoJogadorMessage { acao = NetAcao.Keypad, texto = codigo ?? string.Empty });

    public static void PedirCabo(int indice)
        => Enviar(new AcaoJogadorMessage { acao = NetAcao.Cabo, indice = indice });

    public static void PedirCaboFisico(string nome)
        => Enviar(new AcaoJogadorMessage { acao = NetAcao.CaboFisico, texto = nome ?? string.Empty });

    public static void PedirReabastecimento()
        => Enviar(new AcaoJogadorMessage { acao = NetAcao.Reabastecer });

    public static void PedirItem(string nome)
        => Enviar(new AcaoJogadorMessage { acao = NetAcao.ApanharItem, texto = nome ?? string.Empty });

    private static void Enviar(AcaoJogadorMessage msg)
    {
        if (!EmPartida || !NetworkClient.active) return;
        NetworkClient.Send(msg);
    }

    // ------------------------------------------------------------------ apoio

    private void LigarAvatares()
    {
        int sala = GameSession.room == 2 ? 2 : 1;

        Transform p1 = AvatarDe(1);
        Transform p2 = AvatarDe(2);

        avatarLocal = sala == 2 ? p2 : p1;
        avatarRemoto = sala == 2 ? p1 : p2;

        if (avatarLocal == null || avatarRemoto == null)
            Debug.LogWarning($"[Rede] avatares em falta (sala {sala}): local={(avatarLocal != null)} remoto={(avatarRemoto != null)}");

        AplicarLuzes(lanternaDono);
        temAlvo = false;
        proximaPose = 0f;
        voz = FindAnyObjectByType<LocalVoiceChat>();
        PrepararAnimador();

        TempoRestante = TempoInicial();
        LanternaDono = 1;
        BateriaUm = 1f;
        BateriaDois = 1f;
        KeypadResolvido = false;
        CaboResolvido = false;
        Derrota = false;
        derrotaAplicada = false;
    }

    /// <summary>So anima o avatar remoto se o Animator tiver o parametro float "Speed".</summary>
    private void PrepararAnimador()
    {
        animadorRemoto = avatarRemoto != null ? avatarRemoto.GetComponentInChildren<Animator>() : null;
        animadorPronto = false;
        if (animadorRemoto == null) return;

        foreach (AnimatorControllerParameter parametro in animadorRemoto.parameters)
        {
            if (parametro.type == AnimatorControllerParameterType.Float && parametro.name == "Speed")
            {
                animadorPronto = true;
                return;
            }
        }
    }

    private static Transform Procurar(string nome)
    {
        GameObject obj = GameObject.Find(nome);
        return obj != null ? obj.transform : null;
    }

    /// <summary>
    /// Depois de trocar de cena fica apenas um EventSystem e um NetworkManager.
    /// O NetworkManager sobrevive em DontDestroyOnLoad e a cena nova traz outra cópia:
    /// o Mirror destrói-a, mas o EventSystem extra ficava (dois EventSystems quebram a UI).
    /// </summary>
    private static void LimparDuplicados()
    {
        EventSystem[] eventos = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem principal = null;
        foreach (EventSystem evento in eventos)
        {
            if (evento == null) continue;
            if (principal == null) { principal = evento; continue; }
            Destroy(evento.gameObject);
        }

        NetworkManager[] managers = FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NetworkManager manager in managers)
            if (manager != null && manager != NetworkManager.singleton)
                Destroy(manager.gameObject);
    }

    /// <summary>Avatar de um jogador, ancorado na bateria dele (PlayerNumber) e não no nome.</summary>
    private static Transform AvatarDe(int jogador)
    {
        Battery bateria = BateriaDe(jogador);
        if (bateria != null) return bateria.transform.root;

        return Procurar(jogador == 1 ? "Player" : "Player2");
    }

    /// <summary>Lanterna de capacete (primeira luz spot) do avatar.</summary>
    private static Light LanternaDe(int jogador)
    {
        Transform avatar = AvatarDe(jogador);
        if (avatar == null) return null;

        foreach (Light luz in avatar.GetComponentsInChildren<Light>(true))
            if (luz.type == LightType.Spot) return luz;

        return null;
    }

    /// <summary>Aplica a lanterna ativa (1, 2 ou 0) aos dois avatares locais.</summary>
    internal static void AplicarLuzes(int dono)
    {
        Light p1 = LanternaDe(1);
        Light p2 = LanternaDe(2);
        if (p1 != null) p1.enabled = dono == 1;
        if (p2 != null) p2.enabled = dono == 2;
    }

    private static float TempoInicial()
    {
        TimerScript timer = FindAnyObjectByType<TimerScript>();
        return timer != null ? timer.startTime : 60f;
    }

    private static float DrenagemLanterna(int jogador)
    {
        Battery b = BateriaDe(jogador);
        return b != null ? b.DrenagemPorSegundoLanterna : 0f;
    }

    private static float DrenagemVoz(int jogador)
    {
        Battery b = BateriaDe(jogador);
        return b != null ? b.DrenagemPorSegundoVoz : 0f;
    }

    private static Battery BateriaDe(int jogador)
    {
        foreach (Battery b in FindObjectsByType<Battery>(FindObjectsInactive.Include))
            if (b.PlayerNumber == jogador) return b;
        return null;
    }

    private static string CodigoKeypad()
    {
        PuzzleKeypad keypad = FindAnyObjectByType<PuzzleKeypad>();
        return keypad != null ? keypad.codigoCorreto : string.Empty;
    }

    private static int IndiceCaboCorreto()
    {
        PuzzleCabosTV puzzle = FindAnyObjectByType<PuzzleCabosTV>();
        return puzzle != null ? puzzle.indiceCaboCorreto : -1;
    }

    private void ReporTudo()
    {
        partidaAtiva = false;
        avatarLocal = null;
        avatarRemoto = null;
        temAlvo = false;
        voz = null;
        ReporPartida();
    }

    private static void ReporPartida()
    {
        TempoRestante = 60f;
        BateriaUm = 1f;
        BateriaDois = 1f;
        LanternaDono = 0;
        KeypadResolvido = false;
        CaboResolvido = false;
        Derrota = false;
    }
}
