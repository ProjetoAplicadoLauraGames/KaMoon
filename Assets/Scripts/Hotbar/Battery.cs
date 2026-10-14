using UnityEngine;

/// <summary>
/// Bateria por jogador (task #17). Descarrega ao longo de durationSeconds;
/// esgotada, bloqueia a lanterna e o voice desse jogador. Recarrega ao
/// usar o item "battery" na hotbar (task #21).
/// Uso: um componente por player (Player Number 1 ou 2).
/// </summary>
public class Battery : MonoBehaviour
{
    [Header("Jogador")]
    [Tooltip("1 = P1 (teclado). 2 = P2 (comando).")]
    [SerializeField] private int playerNumber = 1;

    [Header("Carga")]
    [Tooltip("Segundos de uso (lanterna ou voz) até esgotar. Igual para os dois.")]
    [SerializeField] private float lanternSeconds = 120f;
    [Tooltip("Segundos a falar até esgotar. Igual ao da lanterna.")]
    [SerializeField] private float voiceSeconds = 120f;
    [Tooltip("Para testes rápidos, baixa para 10-20.")]
    [SerializeField] private bool startFull = true;

    [Header("Lanterna")]
    [Tooltip("Spotlight própria (opcional). Apaga-se sozinha ao esgotar.")]
    [SerializeField] private Light flashlight;

    [Header("UI")]
    [Tooltip("Barra de carga no canto do ecrã (útil no protótipo).")]
    [SerializeField] private bool showChargeBar = true;

    /// <summary>Carga atual, 0 (vazia) a 1 (cheia).</summary>
    public float Charge { get; private set; } = 1f;

    public bool IsDepleted => Charge <= 0f;
    public int PlayerNumber => playerNumber;

    private GestorLanternas lanternManager;
    private LocalVoiceChat voice;

    private void Awake()
    {
        Charge = startFull ? 1f : 0f;
        lanternManager = FindFirstObjectByType<GestorLanternas>();
        voice = FindFirstObjectByType<LocalVoiceChat>();
    }

    /// <summary>Verdadeiro se a lanterna deste jogador está ligada.</summary>
    private bool IsLanternOn()
    {
        if (flashlight != null && flashlight.enabled)
            return true;
        if (lanternManager == null)
            lanternManager = FindFirstObjectByType<GestorLanternas>();
        if (lanternManager != null)
        {
            if (playerNumber == 1 && lanternManager.lanternaP1 != null && lanternManager.lanternaP1.enabled)
                return true;
            if (playerNumber == 2 && lanternManager.lanternaP2 != null && lanternManager.lanternaP2.enabled)
                return true;
        }
        return false;
    }

    /// <summary>Verdadeiro se este jogador está a falar agora.</summary>
    private bool IsTalking()
    {
        if (voice == null)
            voice = FindFirstObjectByType<LocalVoiceChat>();
        return voice != null && voice.TalkingPlayer == playerNumber;
    }

    private void Update()
    {
        if (IsDepleted) return;

        // Só gasta a usar: lanterna ligada e/ou a falar.
        float drain = 0f;
        if (IsLanternOn() && lanternSeconds > 0f)
            drain += 1f / lanternSeconds;
        if (IsTalking() && voiceSeconds > 0f)
            drain += 1f / voiceSeconds;
        if (drain <= 0f) return;

        Charge -= drain * Time.deltaTime;
        if (Charge <= 0f)
        {
            Charge = 0f;
            OnDepleted();
        }
    }

    private void OnDepleted()
    {
        Debug.Log($"[Bateria P{playerNumber}] Esgotada: lanterna e voice bloqueados.");

        // Apaga a lanterna deste jogador, esteja onde estiver.
        if (flashlight != null)
            flashlight.enabled = false;

        if (lanternManager == null)
            lanternManager = FindFirstObjectByType<GestorLanternas>();
        if (lanternManager != null)
        {
            if (playerNumber == 1 && lanternManager.lanternaP1 != null)
                lanternManager.lanternaP1.enabled = false;
            if (playerNumber == 2 && lanternManager.lanternaP2 != null)
                lanternManager.lanternaP2.enabled = false;
        }
    }

    /// <summary>Recarrega a 100% (ao usar item bateria na hotbar).</summary>
    public void Recharge()
    {
        Charge = 1f;
        Debug.Log($"[Bateria P{playerNumber}] Recarregada.");
    }

    private void OnGUI()
    {
        if (!showChargeBar) return;

        float w = 180f, h = 18f;
        float x = playerNumber == 1 ? 10f : Screen.width - w - 10f;
        Rect back = new Rect(x, 10f, w, h);
        GUI.Box(back, GUIContent.none);
        Rect fill = new Rect(x + 2f, 12f, (w - 4f) * Charge, h - 4f);
        GUI.color = Charge > 0.3f ? Color.green : Color.red;
        GUI.Box(fill, GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, 30f, w, 20f), $"Bateria P{playerNumber}: {(int)(Charge * 100f)}%");
        if (IsDepleted)
            GUI.Label(new Rect(x, 50f, w, 20f), $"P{playerNumber} SEM BATERIA");
    }
}
