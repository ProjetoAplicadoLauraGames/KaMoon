using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Relogio unico da partida. Em local a contagem acontece aqui; em rede o
/// valor vem do servidor (NetMatch). HUD e relogio de parede apenas leem.
/// Os resultados dos puzzles alteram o tempo pelas variaveis do Inspector.
/// </summary>
public class GameTimer : MonoBehaviour
{
    [Header("Relogio")]
    [SerializeField, Min(600f)] private float tempoInicial = 600f; // 10 minutos no minimo

    [Header("Resultado dos puzzles (segundos)")]
    [SerializeField] private float penalidadeErro = 20f;
    [SerializeField] private float recompensaAcerto = 20f;

    private static GameTimer instancia;

    public static float TempoInicial => instancia != null ? instancia.tempoInicial : 600f;
    public static float PenalidadeErro => instancia != null ? instancia.penalidadeErro : 20f;
    public static float RecompensaAcerto => instancia != null ? instancia.recompensaAcerto : 20f;

    public static float TempoRestante { get; private set; } = 600f;
    public static bool EmAlerta => TempoRestante > 0f && TempoRestante <= 60f;

    private bool terminado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReporEstaticos()
    {
        instancia = null;
        TempoRestante = 600f;
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

        TempoRestante = Mathf.Max(0f, TempoRestante - Time.deltaTime);
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
