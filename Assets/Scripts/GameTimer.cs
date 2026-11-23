using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Relogio unico da partida. Em local a contagem acontece aqui; em rede o
/// valor vem do servidor (NetMatch). HUD e relogio de parede apenas leem.
/// Os resultados dos puzzles alteram o tempo pelas variaveis do Inspector.
/// </summary>
public class GameTimer : MonoBehaviour
{
    private const float LimiteAlerta = 60f;

    [Header("Relogio (segundos)")]
    [SerializeField, Min(1f)] private float tempoInicial = 600f;

    [Header("Resultado dos puzzles (segundos)")]
    [SerializeField] private float penalidadeErro = 20f;
    [SerializeField] private float recompensaAcerto = 20f;

    [Header("Alerta no ultimo minuto")]
    [SerializeField] private Color corAlerta = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Pulsos por segundo."), SerializeField, Min(0.1f)] private float velocidadePulso = 1.2f;
    [Tooltip("Escala maxima do texto no ponto do pulso."), SerializeField, Min(1f)] private float escalaMaxima = 1.08f;

    private static GameTimer instancia;

    public static float TempoInicial => instancia != null ? instancia.tempoInicial : 600f;
    public static float PenalidadeErro => instancia != null ? instancia.penalidadeErro : 20f;
    public static float RecompensaAcerto => instancia != null ? instancia.recompensaAcerto : 20f;

    public static float TempoRestante { get; private set; } = 600f;
    public static bool EmAlerta => TempoRestante > 0f && TempoRestante <= LimiteAlerta;
    public static bool EmCurtoCircuito { get; private set; } = false;
    public static float MultiplicadorVelocidade { get; set; } = 1f;

    public static void DefinirCurtoCircuito(bool ativo, float multiplicador = 2f)
    {
        EmCurtoCircuito = ativo;
        MultiplicadorVelocidade = ativo ? multiplicador : 1f;
    }

    /// <summary>Pulso suave de cor e escala, aplicado pelos ecras do relogio.</summary>
    public static void AplicarAlerta(TMP_Text texto, Color corNormal, Vector3 escalaNormal)
    {
        if (texto == null) return;

        if (!EmAlerta)
        {
            texto.color = corNormal;
            texto.rectTransform.localScale = escalaNormal;
            return;
        }

        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * VelocidadePulso * Mathf.PI * 2f);
        texto.color = Color.Lerp(corNormal, CorAlerta, pulso);
        texto.rectTransform.localScale = escalaNormal * Mathf.Lerp(1f, EscalaMaxima, pulso);
    }

    private static Color CorAlerta => instancia != null ? instancia.corAlerta : new Color(1f, 0.2f, 0.2f);
    private static float VelocidadePulso => instancia != null ? instancia.velocidadePulso : 1.2f;
    private static float EscalaMaxima => instancia != null ? instancia.escalaMaxima : 1.08f;

    private bool terminado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReporEstaticos()
    {
        instancia = null;
        TempoRestante = 600f;

        EmCurtoCircuito = false;
        MultiplicadorVelocidade = 1f;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            // O componente da cena tem os valores do Inspector e substitui o automatico.
            Destroy(instancia.gameObject);
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnCenaCarregada;
    }

    // Cenarios sem o componente na cena recebem um automaticamente.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        if (instancia != null) return;
        if (FindAnyObjectByType<GameTimer>() != null) return;

        new GameObject("GameTimer").AddComponent<GameTimer>();
    }

    private static void OnCenaCarregada(Scene cena, LoadSceneMode modo)
    {
        if (cena.name == LobbyPlayer.CenaPartida) Reiniciar();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnCenaCarregada;
        if (instancia == this) instancia = null;
    }

    private void Start() => Reiniciar();

    public static void Reiniciar()
    {
        TempoRestante = TempoInicial;
        if (instancia != null) instancia.terminado = false;
        EmCurtoCircuito = false;
        MultiplicadorVelocidade = 1f;
    }

    private void Update()
    {
        // Em rede o relogio e do servidor; aqui so se espelha o valor.
        if (NetMatch.EmPartida)
        {
            TempoRestante = NetMatch.TempoRestante;
            return;
        }

        if (terminado) return;

        if (TempoRestante <= 0f)
        {
            terminado = true;
            TempoRestante = 0f;
            GameOverManager fim = FindAnyObjectByType<GameOverManager>();
            if (fim != null) fim.GameOver();
            return;
        }

        TempoRestante = Mathf.Max(0f, TempoRestante - Time.deltaTime * MultiplicadorVelocidade);
    }

    // Valores unicos para todos os puzzles, configuraveis no GameTimer da cena.
    public static void RegistarErro() => Ajustar(-PenalidadeErro);

    public static void RegistarAcerto() => Ajustar(RecompensaAcerto);

    private static void Ajustar(float delta)
    {
        if (delta == 0f) return;

        if (NetMatch.EmPartida)
        {
            // O servidor aplica o delta recebido ao relogio autoritativo.
            NetMatch.PedirAjusteTempo(delta);
            return;
        }

        if (instancia == null || instancia.terminado) return;
        TempoRestante = Mathf.Clamp(TempoRestante + delta, 0f, TempoInicial);
    }

    public static string Formatar(float segundos)
    {
        float seguro = Mathf.Max(0f, segundos);
        return string.Format("{0:00}:{1:00}", Mathf.FloorToInt(seguro / 60f), Mathf.FloorToInt(seguro % 60f));
    }
}
