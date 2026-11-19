using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Puzzle_SequenciaTVs : MonoBehaviour, IInteragivel
{
    [Header("Câmara Fixa & UI (Mesmo sistema da Estante)")]
    [Tooltip("Arrasta o painel do Canvas com a RawImage das TVs")]
    public GameObject painelTVsUI;
    [Tooltip("Arrasta a Camera_TVs apontada para as 6 televisões")]
    public Camera cameraTVs;

    [Header("Ecrãs das 6 TVs (Ordem: Cima Esq->Dir [0,1,2], Baixo Esq->Dir [3,4,5])")]
    public Renderer[] ecrasTV = new Renderer[6];

    [Header("Cores dos Ecrãs")]
    public Color corApagada = new Color(0.1f, 0.1f, 0.12f, 1f);
    public Color corSequencia = Color.cyan;
    public Color corCursor = Color.yellow;
    public Color corAcerto = Color.green;
    public Color corErro = Color.red;

    [Header("Configuração da Sequência")]
    public int tamanhoSequencia = 5;
    public float tempoLuzAcesa = 0.55f;
    public float intervaloEntreLuzes = 0.25f;

    [Header("Áudio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip somBeep;
    public AudioClip somAcerto;
    public AudioClip somErro;

    private List<int> sequenciaGerada = new List<int>();
    private int passoAtualJogador = 0;
    private int indiceCursor = 0;

    private bool emModoInteracao = false;
    private bool aMostrarSequencia = false;
    private bool puzzleResolvido = false;

    private CapsuleMovement playerMovement;
    private PlayerInteraction playerInteraction;

    void Awake()
    {
        if (cameraTVs != null) cameraTVs.enabled = false;
        if (painelTVsUI != null) painelTVsUI.SetActive(false);
    }

    void Start()
    {
        GameObject p2 = GameObject.Find("Player2");
        if (p2 != null)
        {
            playerMovement = p2.GetComponent<CapsuleMovement>();
            playerInteraction = p2.GetComponentInChildren<PlayerInteraction>();
        }

        ApagarTodosEcras();
    }

    // Chamado automaticamente pelo PlayerInteraction (Raycast) quando olhas e primes A
    public void Interagir()
    {
        if (puzzleResolvido || emModoInteracao) return;

        emModoInteracao = true;
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerInteraction != null) playerInteraction.emPuzzle = true;

        if (painelTVsUI != null) painelTVsUI.SetActive(true);
        if (cameraTVs != null) cameraTVs.enabled = true;

        IniciarNovoJogo();
    }

    private void IniciarNovoJogo()
    {
        sequenciaGerada.Clear();
        for (int i = 0; i < tamanhoSequencia; i++)
        {
            sequenciaGerada.Add(Random.Range(0, ecrasTV.Length));
        }

        passoAtualJogador = 0;
        indiceCursor = 0;
        StartCoroutine(ReproduzirSequencia());
    }

    private IEnumerator ReproduzirSequencia()
    {
        aMostrarSequencia = true;
        ApagarTodosEcras();
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < sequenciaGerada.Count; i++)
        {
            int tvIndex = sequenciaGerada[i];
            DefinirCorTV(tvIndex, corSequencia);
            if (audioSource != null && somBeep != null) audioSource.PlayOneShot(somBeep);

            yield return new WaitForSeconds(tempoLuzAcesa);

            DefinirCorTV(tvIndex, corApagada);
            yield return new WaitForSeconds(intervaloEntreLuzes);
        }

        aMostrarSequencia = false;
        indiceCursor = 0;
        AtualizarVisualCursor();
    }

    void Update()
    {
        if (!emModoInteracao || aMostrarSequencia || puzzleResolvido) return;

#if ENABLE_INPUT_SYSTEM
        bool esq = false, dir = false, cima = false, baixo = false;
        bool confirmar = false, sair = false;

        if (Gamepad.current != null)
        {
            esq       |= Gamepad.current.dpad.left.wasPressedThisFrame  || Gamepad.current.leftStick.left.wasPressedThisFrame;
            dir       |= Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame;
            cima      |= Gamepad.current.dpad.up.wasPressedThisFrame    || Gamepad.current.leftStick.up.wasPressedThisFrame;
            baixo     |= Gamepad.current.dpad.down.wasPressedThisFrame  || Gamepad.current.leftStick.down.wasPressedThisFrame;
            confirmar |= Gamepad.current.buttonSouth.wasPressedThisFrame; // Botão A
            sair      |= Gamepad.current.buttonEast.wasPressedThisFrame;  // Botão B
        }

        if (Keyboard.current != null)
        {
            esq       |= Keyboard.current.leftArrowKey.wasPressedThisFrame;
            dir       |= Keyboard.current.rightArrowKey.wasPressedThisFrame;
            cima      |= Keyboard.current.upArrowKey.wasPressedThisFrame;
            baixo     |= Keyboard.current.downArrowKey.wasPressedThisFrame;
            confirmar |= Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
            sair      |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        if (sair)
        {
            FecharPuzzle();
            return;
        }

        // Navegação na grelha 3 colunas x 2 linhas
        int coluna = indiceCursor % 3;
        int linha = indiceCursor / 3;

        if (esq && coluna > 0) { indiceCursor -= 1; AtualizarVisualCursor(); }
        else if (dir && coluna < 2) { indiceCursor += 1; AtualizarVisualCursor(); }
        else if (cima && linha > 0) { indiceCursor -= 3; AtualizarVisualCursor(); }
        else if (baixo && linha < 1) { indiceCursor += 3; AtualizarVisualCursor(); }

        if (confirmar)
        {
            VerificarEscolha(indiceCursor);
        }
#endif
    }

    private void VerificarEscolha(int tvEscolhida)
    {
        if (tvEscolhida == sequenciaGerada[passoAtualJogador])
        {
            if (audioSource != null && somBeep != null) audioSource.PlayOneShot(somBeep);
            passoAtualJogador++;

            if (passoAtualJogador >= sequenciaGerada.Count)
            {
                StartCoroutine(FinalizarComSucesso());
            }
            else
            {
                StartCoroutine(PiscarConfirmacao(tvEscolhida, corAcerto));
            }
        }
        else
        {
            StartCoroutine(FalharEReiniciar());
        }
    }

    private IEnumerator PiscarConfirmacao(int tv, Color cor)
    {
        DefinirCorTV(tv, cor);
        yield return new WaitForSeconds(0.2f);
        AtualizarVisualCursor();
    }

    private IEnumerator FalharEReiniciar()
    {
        GameTimer.RegistarErro();
        aMostrarSequencia = true;
        if (audioSource != null && somErro != null) audioSource.PlayOneShot(somErro);

        for (int i = 0; i < ecrasTV.Length; i++) DefinirCorTV(i, corErro);
        yield return new WaitForSeconds(0.6f);

        IniciarNovoJogo();
    }

    private IEnumerator FinalizarComSucesso()
    {
        GameTimer.RegistarAcerto();
        puzzleResolvido = true;
        if (audioSource != null && somAcerto != null) audioSource.PlayOneShot(somAcerto);

        for (int i = 0; i < ecrasTV.Length; i++) DefinirCorTV(i, corAcerto);
        Debug.Log("<color=green>PUZZLE DAS TVS RESOLVIDO!</color>");

        yield return new WaitForSeconds(1.2f);
        FecharPuzzle();
    }

    private void AtualizarVisualCursor()
    {
        ApagarTodosEcras();
        DefinirCorTV(indiceCursor, corCursor);
    }

    private void ApagarTodosEcras()
    {
        for (int i = 0; i < ecrasTV.Length; i++)
        {
            DefinirCorTV(i, corApagada);
        }
    }

    private void DefinirCorTV(int indice, Color cor)
    {
        if (indice >= 0 && indice < ecrasTV.Length && ecrasTV[indice] != null)
        {
            Material mat = ecrasTV[indice].material;
            mat.color = cor;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", cor);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", cor * 1.5f);
            }
        }
    }

    public void FecharPuzzle()
    {
        emModoInteracao = false;
        if (!puzzleResolvido) ApagarTodosEcras();

        if (painelTVsUI != null) painelTVsUI.SetActive(false);
        if (cameraTVs != null) cameraTVs.enabled = false;

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerInteraction != null) playerInteraction.emPuzzle = false;
    }
}