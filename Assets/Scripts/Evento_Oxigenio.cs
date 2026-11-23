using TMPro;
using UnityEngine;

public class EventoOxigenio : MonoBehaviour
{
    [Header("Disparo do Evento (segundos)")]
    [Tooltip("180 segundos = 3 minutos após o início da partida")]
    public float tempoParaDisparar = 180f;
    [Tooltip("Ativa para disparar logo aos 5 segundos (para testar rapidamente)")]
    public bool modoTesteRapido = true;
    [Tooltip("Se marcado, pode voltar a acontecer passado este tempo")]
    public bool repetirEvento = false;
    public float intervaloRepeticao = 180f;

    [Header("Alarme e Sincronização (segundos)")]
    [Tooltip("Tempo limite para os dois jogadores carregarem no botão")]
    public float duracaoAlarme = 15f;
    [Tooltip("Diferença máxima entre o clique do P1 e do P2 para contar como 'ao mesmo tempo'")]
    public float toleranciaSincronizacao = 1.5f;

    [Header("UI Opcional (Aviso no Ecrã)")]
    [Tooltip("Painel ou objeto de aviso que aparece durante o alarme (opcional)")]
    public GameObject painelAlertaUI;
    [Tooltip("Texto TMP para mostrar a contagem decrescente dos 15s (opcional)")]
    public TMP_Text textoContagemAlarme;

    [Header("Luzes e Áudio Opcionais")]
    public Light[] luzesEmergencia;
    public AudioSource audioAlarme;
    public AudioClip somBotaoPressionado;
    public AudioClip somOxigenioEstabilizado;

    public bool AlarmeAtivo { get; private set; } = false;

    private float cronometroDisparo;
    private float tempoRestanteAlarme;
    private bool jaOcorreu = false;

    private float ultimoCliqueP1 = -999f;
    private float ultimoCliqueP2 = -999f;

    private BotaoTerminalOxigenio[] terminais;

    void Start()
    {
        terminais = FindObjectsByType<BotaoTerminalOxigenio>(FindObjectsSortMode.None);

        cronometroDisparo = modoTesteRapido ? 5f : tempoParaDisparar;

        DefinirEmergenciaVisual(false);
    }

    void Update()
    {
        if (!AlarmeAtivo)
        {
            if (jaOcorreu && !repetirEvento) return;

            cronometroDisparo -= Time.deltaTime;
            if (cronometroDisparo <= 0f)
            {
                IniciarAlarmeOxigenio();
            }
            return;
        }

        // Alarme a decorrer (15 segundos)
        tempoRestanteAlarme -= Time.deltaTime;

        if (textoContagemAlarme != null)
        {
            textoContagemAlarme.text = $"OXIGÉNIO CRÍTICO! SINCRONIZEM OS TERMINAIS: {Mathf.CeilToInt(Mathf.Max(0f, tempoRestanteAlarme))}s";
        }

        // Pisca luzes de emergência se existirem
        if (luzesEmergencia != null && luzesEmergencia.Length > 0)
        {
            bool ligada = Mathf.PingPong(Time.time * 4f, 1f) > 0.5f;
            foreach (Light luz in luzesEmergencia)
            {
                if (luz != null) luz.enabled = ligada;
            }
        }

        // Acabou o tempo -> Oxigénio esgotado -> Game Over
        if (tempoRestanteAlarme <= 0f)
        {
            AlarmeAtivo = false;
            DefinirEmergenciaVisual(false);
            Debug.Log("<color=red>[OXIGÉNIO] O oxigénio da base acabou! Game Over!</color>");

            GameOverManager gm = FindAnyObjectByType<GameOverManager>();
            if (gm != null) gm.GameOver();
        }
    }

    public void IniciarAlarmeOxigenio()
    {
        if (AlarmeAtivo) return;

        AlarmeAtivo = true;
        jaOcorreu = true;
        tempoRestanteAlarme = duracaoAlarme;
        ultimoCliqueP1 = -999f;
        ultimoCliqueP2 = -999f;

        DefinirEmergenciaVisual(true);

        if (audioAlarme != null)
        {
            audioAlarme.loop = true;
            audioAlarme.Play();
        }

        foreach (var t in terminais)
            if (t != null) t.AtualizarEstadoTerminal(true, false);

        Debug.Log("<color=red>[OXIGÉNIO] ALARME! Têm 15 segundos para carregar nos dois terminais ao mesmo tempo!</color>");
    }

    public void RegistarCliqueJogador(int jogador)
    {
        if (!AlarmeAtivo) return;

        float agora = Time.time;

        if (jogador == 1)
        {
            ultimoCliqueP1 = agora;
            Debug.Log("<color=yellow>[OXIGÉNIO] Player 1 carregou no terminal!</color>");
        }
        else if (jogador == 2)
        {
            ultimoCliqueP2 = agora;
            Debug.Log("<color=yellow>[OXIGÉNIO] Player 2 carregou no terminal!</color>");
        }

        if (audioAlarme != null && somBotaoPressionado != null)
            audioAlarme.PlayOneShot(somBotaoPressionado);

        // Verifica se ambos carregaram dentro da janela de tolerância ("ao mesmo tempo")
        if (Mathf.Abs(ultimoCliqueP1 - ultimoCliqueP2) <= toleranciaSincronizacao)
        {
            EstabilizarOxigenio();
        }
    }

    private void EstabilizarOxigenio()
    {
        AlarmeAtivo = false;
        DefinirEmergenciaVisual(false);

        if (audioAlarme != null)
        {
            audioAlarme.Stop();
            if (somOxigenioEstabilizado != null)
                audioAlarme.PlayOneShot(somOxigenioEstabilizado);
        }

        foreach (var t in terminais)
            if (t != null) t.AtualizarEstadoTerminal(false, true);

        if (repetirEvento)
            cronometroDisparo = intervaloRepeticao;

        Debug.Log("<color=green>[OXIGÉNIO] Terminais sincronizados! Oxigénio estabilizado!</color>");
    }

    private void DefinirEmergenciaVisual(bool ativo)
    {
        if (painelAlertaUI != null)
            painelAlertaUI.SetActive(ativo);

        if (!ativo && luzesEmergencia != null)
        {
            foreach (Light luz in luzesEmergencia)
                if (luz != null) luz.enabled = false;
        }
    }
}
