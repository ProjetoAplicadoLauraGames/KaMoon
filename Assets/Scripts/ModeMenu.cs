using System;
using System.Collections.Generic;
using Mirror;
using Mirror.Discovery;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Escolha de modo (local/multiplayer) e lobby em rede sobre Mirror.</summary>
public class ModeMenu : MonoBehaviour
{
    [Header("Paineis")]
    public GameObject modePanel;
    public GameObject lobbyPanel;

    [Header("Lobby")]
    public Button createButton;
    public Button joinButton;
    public Button startButton;
    public Button readyButton;
    public TMP_InputField nameField;
    public TMP_InputField ipField;
    public TMP_Text lobbyStatus;
    public TMP_Text playersLabel;
    public TMP_Text netStatus;
    public GameObject roomChoice;
    [Tooltip("Opcional: grupo onde a lista de salas é construída (criado pelo builder).")]
    public RectTransform roomsContainer;
    public Button roomOneButton;
    public Button roomTwoButton;
    public TMP_Text roomOneLabel;
    public TMP_Text roomTwoLabel;
    public Color roomNormalColor = new Color(0.87f, 0.94f, 0.91f);
    public Color roomSelectedColor = new Color(0.9f, 0.6f, 0.25f);

    [Header("Navegacao")]
    public MainMenu mainMenu;
    public Button modeFirstButton;

    private bool tentouLigar;
    private bool erroLigacao;
    private float proximaAtualizacao;
    private RectTransform listaSalas;
    private TMP_Text estadoSalas;
    private string assinaturaSalas;

    private static bool Ligado => NetworkClient.active || NetworkServer.active;

    public static bool AbrirLobbyAoIniciar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimparFlagRegresso() => AbrirLobbyAoIniciar = false;

    private void OnEnable()
    {
        MenuLocalization.LanguageChanged += AtualizarLobby;
        ReporLobby();

        if (AbrirLobbyAoIniciar)
        {
            AbrirLobbyAoIniciar = false;
            if (mainMenu != null) mainMenu.gameObject.SetActive(false);
            AbrirLobby();
        }
    }

    private void OnDisable()
    {
        MenuLocalization.LanguageChanged -= AtualizarLobby;
    }

    private void Update()
    {
        if (lobbyPanel == null || !lobbyPanel.activeSelf)
            return;
        if (Time.unscaledTime < proximaAtualizacao)
            return;

        proximaAtualizacao = Time.unscaledTime + 0.2f;
        if (tentouLigar && !Ligado)
            erroLigacao = true;

        AtualizarLobby();
        AtualizarSalas();
    }

    public void Local()
    {
        ReporLobby();
        GameSession.multiplayer = false;
        GameSession.room = 0;
        modePanel.SetActive(false);
        if (mainMenu != null) mainMenu.OpenBriefing();
    }

    public void AbrirLobby()
    {
        modePanel.SetActive(false);
        lobbyPanel.SetActive(true);
        ReporLobby();
        EsconderCamposManuais();
        PrepararLista();
        assinaturaSalas = null;
        LobbyDiscovery.Procurar();
        AtualizarSalas();
        Focar(createButton);
    }

    public void CriarSala()
    {
        if (Ligado || !TemNetworkManager()) return;

        GuardarNome();
        // O IP não é pedido ao anfitrião: quem cria a sala não escreve o próprio endereço.
        // A partida le GameSession em Awake: tem de estar posto antes de a cena carregar.
        GameSession.multiplayer = true;
        GameSession.room = LobbySession.Sala;
        NetworkManager.singleton.StartHost();
        LobbyDiscovery.Anunciar();
        tentouLigar = true;
        erroLigacao = false;
        MatchGuard.Aviso = null;
        AtualizarLobby();
    }

    /// <summary>Fallback do botão antigo: entra na primeira sala encontrada na rede.</summary>
    public void Entrar()
    {
        if (Ligado) return;

        List<LobbyServerResponse> salas = LobbyDiscovery.Lista();
        if (salas.Count == 0)
        {
            // Sem salas ainda: volta a procurar na rede.
            LobbyDiscovery.Procurar();
            assinaturaSalas = null;
            AtualizarLobby();
            AtualizarSalas();
            return;
        }

        EntrarEm(salas[0].uri.ToString());
    }

    /// <summary>Entra numa sala anunciada na rede (sem escrever IP).</summary>
    public void EntrarEm(string endereco)
    {
        if (Ligado || !TemNetworkManager()) return;
        if (string.IsNullOrEmpty(endereco)) return;

        GuardarNome();
        // A partida le GameSession em Awake: tem de estar posto antes de a cena carregar.
        GameSession.multiplayer = true;
        GameSession.room = LobbySession.Sala;
        NetworkManager.singleton.StartClient(new Uri(endereco));
        LobbyDiscovery.Parar();
        tentouLigar = true;
        erroLigacao = false;
        MatchGuard.Aviso = null;
        assinaturaSalas = null;
        AtualizarLobby();
    }

    public void AlternarPronto()
    {
        if (!LobbyRules.SalaValida(LobbySession.Sala)) return;   // escolhe a sala primeiro

        LobbySession.Pronto = !LobbySession.Pronto;
        EnviarEscolhas();
        AtualizarLobby();
    }

    public void EscolherSala1() => EscolherSala(1);

    public void EscolherSala2() => EscolherSala(2);

    public void Iniciar()
    {
        // O servidor valida de novo (incluindo se o pedido vem do anfitrião);
        // um cliente a chamar isto não arranca nada.
        LobbyDiscovery.Parar();
        LobbyPlayer.Local?.CmdIniciar();
    }

    public void VoltarLobby()
    {
        ReporLobby();
        lobbyPanel.SetActive(false);
        modePanel.SetActive(true);
        Focar(modeFirstButton);
    }

    public void Voltar()
    {
        ReporLobby();
        modePanel.SetActive(false);
        if (mainMenu != null) mainMenu.gameObject.SetActive(true);
    }

    private void EscolherSala(int sala)
    {
        LobbySession.Sala = sala;
        GameSession.multiplayer = true;
        GameSession.room = sala;
        EnviarEscolhas();
        AtualizarLobby();
        Focar(readyButton);
    }

    private void ReporLobby()
    {
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active) NetworkManager.singleton.StopHost();
            else if (NetworkClient.active) NetworkManager.singleton.StopClient();
        }

        LobbyDiscovery.Parar();
        assinaturaSalas = null;
        tentouLigar = false;
        erroLigacao = false;
        LobbySession.Sala = 0;
        LobbySession.Pronto = false;

        if (nameField != null && string.IsNullOrWhiteSpace(nameField.text))
            nameField.SetTextWithoutNotify(Environment.MachineName);
        if (ipField != null && string.IsNullOrWhiteSpace(ipField.text))
            ipField.SetTextWithoutNotify("127.0.0.1");

        AtualizarLobby();

        // Sair do lobby devolve o jogo ao modo local (split-screen) ate nova escolha online.
        GameSession.multiplayer = false;
        GameSession.room = 0;
    }

    private bool TemNetworkManager()
    {
        if (NetworkManager.singleton != null) return true;

        Debug.LogError("[Lobby] NetworkManager em falta. Corre Tools > KaMoon > Build Networking.");
        erroLigacao = true;
        AtualizarLobby();
        return false;
    }

    private void GuardarNome()
    {
        string nome = nameField != null ? nameField.text.Trim() : "";
        LobbySession.Nome = nome.Length > 0 ? nome : Environment.MachineName;
    }

    private static string NomeAtual() => string.IsNullOrEmpty(LobbySession.Nome)
        ? Environment.MachineName
        : LobbySession.Nome;

    /// <summary>Empurra sala/prontidão para o servidor; avisa quando ainda não há jogador local.</summary>
    private void EnviarEscolhas()
    {
        LobbyPlayer local = LobbyPlayer.Local;
        if (local == null)
        {
            Debug.LogWarning("[Lobby] Ainda sem LobbyPlayer local; as escolhas serão reenviadas ao entrar.");
            return;
        }

        Debug.Log($"[Lobby] a enviar escolhas: sala {LobbySession.Sala}, pronto {LobbySession.Pronto}.");
        local.EnviarEscolhas(NomeAtual(), LobbySession.Sala, LobbySession.Pronto);
    }

    private void AtualizarLobby()
    {
        if (lobbyPanel == null) return;

        bool ligado = Ligado;
        bool confirmado = NetworkClient.isConnected;
        bool anfitriao = NetworkServer.active && NetworkServer.activeHost;
        List<LobbyPlayer> jogadores = Jogadores();

        if (createButton != null) createButton.gameObject.SetActive(!ligado);
        // Entrar fica sempre clicável: sem salas, repete a procura na rede.
        if (joinButton != null) joinButton.gameObject.SetActive(!ligado);
        if (nameField != null) nameField.gameObject.SetActive(false);
        if (ipField != null) ipField.gameObject.SetActive(false);
        if (roomChoice != null) roomChoice.SetActive(confirmado);
        if (readyButton != null)
            readyButton.gameObject.SetActive(confirmado && LobbyRules.SalaValida(LobbySession.Sala));
        if (lobbyStatus != null) lobbyStatus.gameObject.SetActive(anfitriao);

        if (startButton != null)
        {
            startButton.gameObject.SetActive(confirmado);
            startButton.interactable = anfitriao && PodeArrancar(jogadores);
        }

        if (lobbyStatus != null)
            lobbyStatus.text = MenuLocalization.GetText(MenuTextKey.LobbyHost);

        if (playersLabel != null)
            playersLabel.text = confirmado ? DescreverSalas(jogadores) : "";

        if (netStatus != null)
            netStatus.text = TextoEstado(ligado, confirmado, anfitriao, jogadores);

        if (readyButton != null)
        {
            TMP_Text rotulo = readyButton.GetComponentInChildren<TMP_Text>();
            if (rotulo != null)
                rotulo.text = MenuLocalization.GetText(
                    LobbySession.Pronto ? MenuTextKey.LobbyNotReady : MenuTextKey.LobbyReady);
        }

        PintarSala(roomOneLabel, LobbySession.Sala == 1);
        PintarSala(roomTwoLabel, LobbySession.Sala == 2);
    }

    private string TextoEstado(bool ligado, bool confirmado, bool anfitriao, List<LobbyPlayer> jogadores)
    {
        if (!ligado)
        {
            if (!string.IsNullOrEmpty(MatchGuard.Aviso)) return MatchGuard.Aviso;
            if (erroLigacao) return MenuLocalization.GetText(MenuTextKey.LobbyFailed);
            return MenuLocalization.GetText(tentouLigar ? MenuTextKey.LobbyConnecting : MenuTextKey.LobbyHelp);
        }

        if (!confirmado)
            return MenuLocalization.GetText(MenuTextKey.LobbyConnecting);

        // O anfitrião anuncia-se na rede; não há IP para escrever.
        if (anfitriao)
            return MenuLocalization.GetText(MenuTextKey.LobbyHostAdvertised);

        if (!PodeArrancar(jogadores))
            return MesmaSala(jogadores)
                ? MenuLocalization.GetText(MenuTextKey.LobbyRoomConflict)
                : MenuLocalization.GetText(MenuTextKey.LobbyWaiting);

        return string.Empty;
    }

    private static List<LobbyPlayer> Jogadores() => LobbyPlayer.Todos();

    private static bool PodeArrancar(List<LobbyPlayer> jogadores)
    {
        if (jogadores.Count != LobbyRules.MaxPlayers) return false;

        bool prontos = true;
        foreach (LobbyPlayer jogador in jogadores)
            prontos &= jogador.ready;

        return LobbyRules.PodeComecar(jogadores.Count, prontos, jogadores[0].room, jogadores[1].room);
    }

    private static bool MesmaSala(List<LobbyPlayer> jogadores)
        => jogadores.Count == LobbyRules.MaxPlayers && jogadores[0].room == jogadores[1].room;

    private static string DescreverSalas(List<LobbyPlayer> jogadores)
        => LinhaSala(jogadores, 1, MenuTextKey.LobbyRoomOne) + "\n" + LinhaSala(jogadores, 2, MenuTextKey.LobbyRoomTwo);

    private static string LinhaSala(List<LobbyPlayer> jogadores, int sala, MenuTextKey chave)
    {
        string titulo = MenuLocalization.GetText(chave);
        foreach (LobbyPlayer jogador in jogadores)
        {
            if (jogador.room != sala) continue;
            // A Rajdhani-Menu não tem o glifo ✓: usar um marcador ASCII presente na fonte.
            return titulo + " — " + jogador.displayName + (jogador.ready ? "  *" : "");
        }
        return titulo + " — " + MenuLocalization.GetText(MenuTextKey.LobbyFree);
    }

    private void PintarSala(TMP_Text rotulo, bool escolhida)
    {
        if (rotulo != null) rotulo.color = escolhida ? roomSelectedColor : roomNormalColor;
    }

    /// <summary>Esconde as etiquetas antigas de Nome/IP e o botão Entrar do painel atual.</summary>
    private void EsconderCamposManuais()
    {
        // Criar e Entrar ficam lado a lado no lugar dos antigos campos de nome/IP.
        Posicionar(createButton, new Vector2(-150f, 240f));
        Posicionar(joinButton, new Vector2(150f, 240f));

        if (nameField != null) nameField.gameObject.SetActive(false);
        if (ipField != null) ipField.gameObject.SetActive(false);
        OcultarPorNome("NameLabel");
        OcultarPorNome("IpLabel");
    }

    private void OcultarPorNome(string nome)
    {
        if (lobbyPanel == null) return;

        foreach (Transform filho in lobbyPanel.GetComponentsInChildren<Transform>(true))
            if (filho.name == nome) filho.gameObject.SetActive(false);
    }

    /// <summary>Cria (uma vez) o grupo com o título e o estado da lista de salas.</summary>
    private void PrepararLista()
    {
        if (listaSalas != null || lobbyPanel == null) return;

        if (roomsContainer == null)
        {
            var grupo = new GameObject("RoomsList", typeof(RectTransform));
            var rt = grupo.GetComponent<RectTransform>();
            rt.SetParent(lobbyPanel.transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 140f);
            rt.sizeDelta = new Vector2(620f, 160f);
            roomsContainer = rt;
        }

        listaSalas = roomsContainer;

        TMP_Text titulo = CriarTextoLista("RoomsTitle", new Vector2(0f, 56f), 20, FontStyles.Bold);
        titulo.gameObject.AddComponent<MenuLocalizedText>().SetKey(MenuTextKey.LobbyRoomsTitle);

        estadoSalas = CriarTextoLista("RoomsStatus", new Vector2(0f, 28f), 18, FontStyles.Normal);
        estadoSalas.gameObject.AddComponent<MenuLocalizedText>().SetKey(MenuTextKey.LobbyNoRooms);
    }

    private TMP_Text CriarTextoLista(string nome, Vector2 posicao, int tamanho, FontStyles estilo)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(listaSalas, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(620f, 34f);

        var texto = go.GetComponent<TextMeshProUGUI>();
        TMP_Text modelo = playersLabel != null ? playersLabel : lobbyStatus;
        if (modelo != null) texto.font = modelo.font;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = roomNormalColor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        return texto;
    }

    /// <summary>Reconstrói a lista quando o conjunto de salas encontradas muda.</summary>
    private void AtualizarSalas()
    {
        if (lobbyPanel == null || !lobbyPanel.activeSelf) return;

        PrepararLista();
        if (listaSalas == null) return;

        bool ligado = Ligado;
        List<LobbyServerResponse> encontradas = ligado ? new List<LobbyServerResponse>() : LobbyDiscovery.Lista();

        string assinatura = ligado ? "ligado" : Assinatura(encontradas);
        if (assinatura == assinaturaSalas) return;
        assinaturaSalas = assinatura;

        for (int i = listaSalas.childCount - 1; i >= 0; i--)
        {
            Transform filho = listaSalas.GetChild(i);
            if (filho.name == "RoomsTitle" || filho.name == "RoomsStatus") continue;
            Destroy(filho.gameObject);
        }

        if (estadoSalas != null)
        {
            estadoSalas.gameObject.SetActive(!ligado);
            MenuTextKey chave = encontradas.Count == 0 ? MenuTextKey.LobbyNoRooms : MenuTextKey.LobbySearching;
            var localizado = estadoSalas.GetComponent<MenuLocalizedText>();
            if (localizado != null) localizado.SetKey(chave);
            else estadoSalas.text = MenuLocalization.GetText(chave);
        }

        if (ligado) return;

        for (int i = 0; i < encontradas.Count && i < 2; i++)
            CriarBotaoSala(encontradas[i].uri, encontradas[i].anfitrao, i);
    }

    private static string Assinatura(List<LobbyServerResponse> salas)
    {
        var texto = new System.Text.StringBuilder();
        foreach (LobbyServerResponse sala in salas)
            texto.Append(sala.serverId).Append('|');
        return texto.ToString();
    }

    private void CriarBotaoSala(Uri uri, string anfitrao, int indice)
    {
        var go = new GameObject("Room" + indice, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(listaSalas, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -10f - indice * 44f);
        rt.sizeDelta = new Vector2(560f, 40f);

        var imagem = go.GetComponent<Image>();
        var botao = go.GetComponent<Button>();
        botao.targetGraphic = imagem;
        botao.colors = new ColorBlock
        {
            normalColor = new Color(0.08f, 0.17f, 0.18f, 1f),
            highlightedColor = new Color(0.3f, 0.48f, 0.39f, 1f),
            pressedColor = new Color(0.9f, 0.6f, 0.25f, 1f),
            selectedColor = new Color(0.65f, 0.4f, 0.16f, 1f),
            disabledColor = new Color(0.15f, 0.18f, 0.18f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };

        var rotulo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rotuloRt = rotulo.GetComponent<RectTransform>();
        rotuloRt.SetParent(go.transform, false);
        rotuloRt.anchorMin = Vector2.zero;
        rotuloRt.anchorMax = Vector2.one;
        rotuloRt.offsetMin = rotuloRt.offsetMax = Vector2.zero;

        var texto = rotulo.GetComponent<TextMeshProUGUI>();
        TMP_Text modelo = playersLabel != null ? playersLabel : lobbyStatus;
        if (modelo != null) texto.font = modelo.font;
        texto.fontSize = 20;
        texto.color = roomNormalColor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        string nome = string.IsNullOrWhiteSpace(anfitrao)
            ? MenuLocalization.GetText(MenuTextKey.LobbyHost)
            : anfitrao;
        texto.text = MenuLocalization.GetText(MenuTextKey.LobbyJoinRoom) + "  ·  " + nome;

        string endereco = uri.ToString();
        botao.onClick.AddListener(() => EntrarEm(endereco));
    }

    private static void Posicionar(Button botao, Vector2 posicao)
    {
        if (botao == null) return;
        var rt = botao.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = posicao;
    }

    private void Focar(Button botao)
    {
        if (botao != null && botao.gameObject.activeInHierarchy && EventSystem.current != null)
            botao.Select();
    }
}
