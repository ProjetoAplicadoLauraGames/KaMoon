using UnityEngine;

public class EventoCurtoCircuitoTimer : MonoBehaviour
{
    [Header("Tempos de Disparo (segundos)")]
    [Tooltip("Tempo mínimo até acontecer o primeiro curto-circuito")]
    public float tempoMinimoPrimeiro = 60f;
    [Tooltip("Tempo máximo até acontecer o primeiro curto-circuito")]
    public float tempoMaximoPrimeiro = 120f;

    [Tooltip("Tempo mínimo entre avarias depois de ser consertado")]
    public float cooldownMinimoProximo = 90f;
    [Tooltip("Tempo máximo entre avarias depois de ser consertado")]
    public float cooldownMaximoProximo = 150f;

    [Tooltip("Ativa para o primeiro curto-circuito acontecer logo aos 5 segundos (para testar)")]
    public bool modoTesteRapido = true;

    [Header("Configuração da Avaria")]
    [Tooltip("Multiplicador de velocidade do tempo enquanto avariado (2 = 2x mais rápido)")]
    public float multiplicadorTempoAvariado = 2f;

    [Header("Visuais Opcionais do Relógio")]
    [Tooltip("Objetos/luzes/ecrãs do relógio que devem desligar durante o apagão")]
    public GameObject[] objetosParaApagarNoCurto;

    [Header("Áudio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip somCurtoCircuito;
    public AudioClip somLadoReparado;
    public AudioClip somTimerRestaurado;

    private float cronometroProximoEvento;
    private bool lado1Reparado = false;
    private bool lado2Reparado = false;

    private RepararTimerLado[] paineisReparacao;

    void Start()
    {
        paineisReparacao = FindObjectsByType<RepararTimerLado>(FindObjectsSortMode.None);
        GameTimer.DefinirCurtoCircuito(false, 1f);

        if (modoTesteRapido)
            cronometroProximoEvento = 5f;
        else
            cronometroProximoEvento = Random.Range(tempoMinimoPrimeiro, tempoMaximoPrimeiro);
    }

    void Update()
    {
        // Só conta para a próxima avaria enquanto o relógio estiver a funcionar normalmente
        if (!GameTimer.EmCurtoCircuito)
        {
            cronometroProximoEvento -= Time.deltaTime;
            if (cronometroProximoEvento <= 0f)
            {
                IniciarCurtoCircuito();
            }
        }
    }

    public void IniciarCurtoCircuito()
    {
        if (GameTimer.EmCurtoCircuito) return;

        lado1Reparado = false;
        lado2Reparado = false;

        GameTimer.DefinirCurtoCircuito(true, multiplicadorTempoAvariado);

        if (objetosParaApagarNoCurto != null)
        {
            foreach (GameObject obj in objetosParaApagarNoCurto)
                if (obj != null) obj.SetActive(false);
        }

        foreach (var painel in paineisReparacao)
            painel.AtualizarVisualEstado(true, false);

        if (audioSource != null && somCurtoCircuito != null)
            audioSource.PlayOneShot(somCurtoCircuito);

        Debug.Log("<color=red>[TIMER] Curto-circuito! Ecrã apagado e tempo a correr 2x mais rápido!</color>");
    }

    public void RegistarReparacaoLado(int jogador)
    {
        if (!GameTimer.EmCurtoCircuito) return;

        if (jogador == 1 && !lado1Reparado)
        {
            lado1Reparado = true;
            if (audioSource != null && somLadoReparado != null) audioSource.PlayOneShot(somLadoReparado);
            AtualizarPainelJogador(1, true);
            Debug.Log("<color=yellow>[TIMER] Lado do Player 1 reparado! A aguardar Player 2...</color>");
        }
        else if (jogador == 2 && !lado2Reparado)
        {
            lado2Reparado = true;
            if (audioSource != null && somLadoReparado != null) audioSource.PlayOneShot(somLadoReparado);
            AtualizarPainelJogador(2, true);
            Debug.Log("<color=yellow>[TIMER] Lado do Player 2 reparado! A aguardar Player 1...</color>");
        }

        // Só restaura o relógio quando AMBOS os lados forem consertados
        if (lado1Reparado && lado2Reparado)
        {
            RestaurarTimer();
        }
    }

    private void RestaurarTimer()
    {
        GameTimer.DefinirCurtoCircuito(false, 1f);

        if (objetosParaApagarNoCurto != null)
        {
            foreach (GameObject obj in objetosParaApagarNoCurto)
                if (obj != null) obj.SetActive(true);
        }

        foreach (var painel in paineisReparacao)
            painel.AtualizarVisualEstado(false, false);

        if (audioSource != null && somTimerRestaurado != null)
            audioSource.PlayOneShot(somTimerRestaurado);

        // Agenda o próximo curto-circuito aleatório na partida
        cronometroProximoEvento = Random.Range(cooldownMinimoProximo, cooldownMaximoProximo);
        Debug.Log($"<color=green>[TIMER] Timer restaurado dos dois lados! Próxima avaria possível em {cronometroProximoEvento:F0}s.</color>");
    }

    private void AtualizarPainelJogador(int jogador, bool reparado)
    {
        foreach (var painel in paineisReparacao)
        {
            if (painel.ladoJogador == jogador)
                painel.AtualizarVisualEstado(true, reparado);
        }
    }
}
