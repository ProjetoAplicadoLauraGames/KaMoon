using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Puzzle_Sintonizador : MonoBehaviour, IInteragivel
{
    [Header("Câmara Fixa & UI (Mesmo sistema da Estante/TVs)")]
    public GameObject painelSintonizadorUI;
    public Camera cameraSintonizador;

    [Header("Componentes 3D do Visor")]
    [Tooltip("LineRenderer para a onda alvo (cor ciano/branca)")]
    public LineRenderer linhaAlvo;
    [Tooltip("LineRenderer para a onda do jogador (cor amarela/verde)")]
    public LineRenderer linhaJogador;
    [Tooltip("Opcional: o botão amarelo do rádio para rodar enquanto sintonizas")]
    public Transform botaoRotativo;

    [Header("Dimensões do Visor 3D (Ajustar ao retângulo do rádio)")]
    public float larguraVisor = 0.28f;
    public float alturaMaximaOnda = 0.07f;
    public int resolucaoPontos = 60;

    [Header("Dificuldade & Sintonização")]
    public float toleranciaFrequencia = 0.35f;
    public float toleranciaAmplitude = 0.12f;
    public float tempoParaConfirmar = 2.0f;

    [Header("Áudio (Opcional)")]
    public AudioSource audioEstatica;
    public AudioSource audioFeedback;
    public AudioClip somSintonizado;

    private float freqAlvo;
    private float ampAlvo;
    private float freqJogador = 2.0f;
    private float ampJogador = 0.5f;

    private float tempoAlinhado = 0f;
    private float faseAnimacao = 0f;
    private bool emModoInteracao = false;
    private bool puzzleResolvido = false;

    private CapsuleMovement playerMovement;
    private PlayerInteraction playerInteraction;

    void Awake()
    {
        if (cameraSintonizador != null) cameraSintonizador.enabled = false;
        if (painelSintonizadorUI != null) painelSintonizadorUI.SetActive(false);
    }

    void Start()
    {
        GameObject p2 = GameObject.Find("Player2");
        if (p2 != null)
        {
            playerMovement = p2.GetComponent<CapsuleMovement>();
            playerInteraction = p2.GetComponentInChildren<PlayerInteraction>();
        }

        ConfigurarLineRenderer(linhaAlvo, Color.cyan);
        ConfigurarLineRenderer(linhaJogador, Color.yellow);
        GerarNovoAlvo();
    }

    private void ConfigurarLineRenderer(LineRenderer lr, Color cor)
    {
        if (lr == null) return;
        lr.useWorldSpace = false;
        lr.positionCount = resolucaoPontos;
        lr.startColor = cor;
        lr.endColor = cor;
    }

    public void Interagir()
    {
        if (puzzleResolvido || emModoInteracao) return;

        emModoInteracao = true;
        tempoAlinhado = 0f;

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerInteraction != null) playerInteraction.emPuzzle = true;

        if (painelSintonizadorUI != null) painelSintonizadorUI.SetActive(true);
        if (cameraSintonizador != null) cameraSintonizador.enabled = true;
        if (audioEstatica != null) audioEstatica.Play();
    }

    private void GerarNovoAlvo()
    {
        freqAlvo = Random.Range(2.0f, 7.5f);
        ampAlvo = Random.Range(0.35f, 0.95f);

        // Garante que não começa já resolvido
        freqJogador = (freqAlvo > 4.5f) ? 2.0f : 7.0f;
        ampJogador = 0.5f;
    }

    void Update()
    {
        // Anima suavemente as ondas no visor
        faseAnimacao += Time.deltaTime * 4f;
        DesenharOnda(linhaAlvo, freqAlvo, ampAlvo, faseAnimacao);
        DesenharOnda(linhaJogador, freqJogador, ampJogador, faseAnimacao);

        if (!emModoInteracao || puzzleResolvido) return;

        LerInputsSintonizacao();
        VerificarAlinhamento();
    }

    private void LerInputsSintonizacao()
    {
        float inputFreq = 0f;
        float inputAmp = 0f;
        bool sair = false;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            // Analógico Esquerdo (X) ou D-Pad Esquerda/Direita -> Frequência
            inputFreq += Gamepad.current.leftStick.x.ReadValue();
            if (Gamepad.current.dpad.left.isPressed) inputFreq -= 1f;
            if (Gamepad.current.dpad.right.isPressed) inputFreq += 1f;

            // Analógico Direito (Y) ou D-Pad Cima/Baixo -> Amplitude
            inputAmp += Gamepad.current.rightStick.y.ReadValue();
            if (Gamepad.current.dpad.down.isPressed) inputAmp -= 1f;
            if (Gamepad.current.dpad.up.isPressed) inputAmp += 1f;

            if (Gamepad.current.buttonEast.wasPressedThisFrame) sair = true;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputFreq -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputFreq += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputAmp -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputAmp += 1f;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) sair = true;
        }
#endif

        if (sair)
        {
            FecharPuzzle();
            return;
        }

        inputFreq = Mathf.Clamp(inputFreq, -1f, 1f);
        inputAmp = Mathf.Clamp(inputAmp, -1f, 1f);

        freqJogador = Mathf.Clamp(freqJogador + inputFreq * 3.0f * Time.deltaTime, 1.5f, 8.0f);
        ampJogador = Mathf.Clamp(ampJogador + inputAmp * 0.75f * Time.deltaTime, 0.2f, 1.0f);

        // Roda visualmente o botão amarelo do rádio
        if (botaoRotativo != null && Mathf.Abs(inputFreq) > 0.05f)
        {
            botaoRotativo.Rotate(Vector3.forward, -inputFreq * 180f * Time.deltaTime, Space.Self);
        }
    }

    private void VerificarAlinhamento()
    {
        float diffFreq = Mathf.Abs(freqJogador - freqAlvo);
        float diffAmp = Mathf.Abs(ampJogador - ampAlvo);

        bool estaAlinhado = (diffFreq <= toleranciaFrequencia) && (diffAmp <= toleranciaAmplitude);

        if (estaAlinhado)
        {
            tempoAlinhado += Time.deltaTime;
            MudarCorLinha(linhaJogador, Color.Lerp(Color.yellow, Color.green, tempoAlinhado / tempoParaConfirmar));

            if (tempoAlinhado >= tempoParaConfirmar)
            {
                ConcluirPuzzle();
            }
        }
        else
        {
            tempoAlinhado = 0f;
            MudarCorLinha(linhaJogador, Color.yellow);
        }

        // Reduz a estática conforme se aproxima da frequência certa
        if (audioEstatica != null)
        {
            float proximidade = Mathf.Clamp01(1f - (diffFreq / 5f));
            audioEstatica.volume = Mathf.Lerp(0.4f, 0.02f, proximidade);
        }
    }

    private void DesenharOnda(LineRenderer lr, float frequencia, float amplitude, float fase)
    {
        if (lr == null || resolucaoPontos < 2) return;

        lr.positionCount = resolucaoPontos;
        for (int i = 0; i < resolucaoPontos; i++)
        {
            float t = (float)i / (resolucaoPontos - 1);
            float x = Mathf.Lerp(-larguraVisor * 0.5f, larguraVisor * 0.5f, t);
            float y = Mathf.Sin(t * frequencia * Mathf.PI * 2f + fase) * (amplitude * alturaMaximaOnda);
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void MudarCorLinha(LineRenderer lr, Color cor)
    {
        if (lr == null) return;
        lr.startColor = cor;
        lr.endColor = cor;
    }

    private void ConcluirPuzzle()
    {
        GameTimer.RegistarAcerto();
        puzzleResolvido = true;
        MudarCorLinha(linhaAlvo, Color.green);
        MudarCorLinha(linhaJogador, Color.green);

        if (audioEstatica != null) audioEstatica.Stop();
        if (audioFeedback != null && somSintonizado != null) audioFeedback.PlayOneShot(somSintonizado);

        Debug.Log("<color=green>SINAL SINTONIZADO COM SUCESSO!</color>");
        Invoke(nameof(FecharPuzzle), 1.2f);
    }

    public void FecharPuzzle()
    {
        emModoInteracao = false;
        if (audioEstatica != null) audioEstatica.Stop();

        if (painelSintonizadorUI != null) painelSintonizadorUI.SetActive(false);
        if (cameraSintonizador != null) cameraSintonizador.enabled = false;

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerInteraction != null) playerInteraction.emPuzzle = false;
    }
}