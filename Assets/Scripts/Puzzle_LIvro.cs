using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PuzzleEstante3D : MonoBehaviour
{
    [Header("Estrutura da UI / Câmara")]
    public GameObject painelEstanteUI;
    public Camera cameraEstante;

    [Header("Objetos 3D dos Livros")]
    public Transform[] livrosPrateleiraBaixo;
    public int indiceLivroCorreto = 4;

    [Header("Movimento da Alavanca")]
    public Vector3 deslocamentoPuxar = new Vector3(0f, -0.04f, 0.10f);
    public float rotacaoPuxarX = 12f;
    public float duracaoMovimento = 0.35f;

    [Header("Cursor de Seleção (Substituto da Luz)")]
    [Tooltip("Arrasta aqui o objeto Moldura_Selecao")]
    public GameObject cursorSelecao;
    public Vector3 offsetCursor = new Vector3(0f, -0.15f, 0.05f);

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip somClique;
    public AudioClip somErro;

    private int indiceAtual = 0;
    private bool puzzleAtivo = false;
    private bool resolvido = false;
    private bool aAnimar = false;

    private Vector3[] posicoesIniciais;
    private Quaternion[] rotacoesIniciais;

    void Awake()
    {
        if (livrosPrateleiraBaixo != null && livrosPrateleiraBaixo.Length > 0)
        {
            posicoesIniciais = new Vector3[livrosPrateleiraBaixo.Length];
            rotacoesIniciais = new Quaternion[livrosPrateleiraBaixo.Length];

            for (int i = 0; i < livrosPrateleiraBaixo.Length; i++)
            {
                if (livrosPrateleiraBaixo[i] != null)
                {
                    posicoesIniciais[i] = livrosPrateleiraBaixo[i].localPosition;
                    rotacoesIniciais[i] = livrosPrateleiraBaixo[i].localRotation;
                }
            }
        }

        if (cursorSelecao != null) cursorSelecao.SetActive(false);
        if (cameraEstante != null) cameraEstante.enabled = false;
        if (painelEstanteUI != null) painelEstanteUI.SetActive(false);
    }

    void Update()
    {
        // Se o puzzle não estiver ativo no ecrã, ignora COMPLETAMENTE os inputs
        if (!puzzleAtivo || aAnimar) return;

        bool esquerda = false;
        bool direita = false;
        bool confirmar = false;
        bool cancelar = false;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            // D-Pad ou analógico esquerdo do Player 2
            esquerda |= Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame;
            direita  |= Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame;
            confirmar |= Gamepad.current.buttonSouth.wasPressedThisFrame; // Botão A
            cancelar  |= Gamepad.current.buttonEast.wasPressedThisFrame;  // Botão B
        }

        // Teclado apenas como fallback secundário (Setas do teclado, NÃO usa A/D para não colidir com P1)
        if (Keyboard.current != null)
        {
            esquerda |= Keyboard.current.leftArrowKey.wasPressedThisFrame;
            direita  |= Keyboard.current.rightArrowKey.wasPressedThisFrame;
            confirmar |= Keyboard.current.enterKey.wasPressedThisFrame;
            cancelar  |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif

        if (cancelar)
        {
            FecharPuzzle();
            return;
        }

        if (esquerda) MudarLivro(-1);
        if (direita) MudarLivro(1);

        if (confirmar)
        {
            StartCoroutine(AcionarLivroAnimacao(indiceAtual));
        }
    }

    private void MudarLivro(int direcao)
    {
        int novo = indiceAtual + direcao;
        if (novo >= 0 && novo < livrosPrateleiraBaixo.Length)
        {
            indiceAtual = novo;
            AtualizarPosicaoCursor();
        }
    }

    private void AtualizarPosicaoCursor()
    {
        if (livrosPrateleiraBaixo == null || livrosPrateleiraBaixo.Length == 0) return;
        if (cursorSelecao == null) return;

        Transform livroAlvo = livrosPrateleiraBaixo[indiceAtual];
        if (livroAlvo == null) return;

        cursorSelecao.transform.SetParent(livroAlvo);

        // Desce a altura em Y e puxa ligeiramente para trás em Z para alinhar com o topo do livro
        cursorSelecao.transform.localPosition = new Vector3(0f, 0.19f, 0.01f);
        cursorSelecao.transform.localRotation = Quaternion.identity;

        // X reduzido (largura exata da lombada), Y reduzido (topo do livro), Z alinhado
        cursorSelecao.transform.localScale = new Vector3(0.155f, 0.44f, 0.39f);

        cursorSelecao.SetActive(true);
    }

    private IEnumerator AcionarLivroAnimacao(int indice)
    {
        aAnimar = true;
        Transform livro = livrosPrateleiraBaixo[indice];

        Vector3 posOrig = posicoesIniciais[indice];
        Quaternion rotOrig = rotacoesIniciais[indice];

        Vector3 posDest = posOrig + deslocamentoPuxar;
        Quaternion rotDest = rotOrig * Quaternion.Euler(rotacaoPuxarX, 0f, 0f);

        // Animação a puxar para fora
        float t = 0f;
        while (t < duracaoMovimento)
        {
            t += Time.deltaTime;
            float p = t / duracaoMovimento;
            livro.localPosition = Vector3.Lerp(posOrig, posDest, p);
            livro.localRotation = Quaternion.Lerp(rotOrig, rotDest, p);
            yield return null;
        }
        livro.localPosition = posDest;
        livro.localRotation = rotDest;

        if (audioSource != null && somClique != null)
            audioSource.PlayOneShot(somClique);

        yield return new WaitForSeconds(0.4f);

        if (indice == indiceLivroCorreto)
        {
            resolvido = true;
            Debug.Log("<color=green>LIVRO AZUL ACIONADO!</color>");

            yield return new WaitForSeconds(1.0f);

            // Retorna suavemente o livro para não ficar permanentemente torto
            t = 0f;
            while (t < duracaoMovimento)
            {
                t += Time.deltaTime;
                float p = t / duracaoMovimento;
                livro.localPosition = Vector3.Lerp(posDest, posOrig, p);
                livro.localRotation = Quaternion.Lerp(rotDest, rotOrig, p);
                yield return null;
            }
            livro.localPosition = posOrig;
            livro.localRotation = rotOrig;

            FecharPuzzle();
        }
        else
        {
            if (audioSource != null && somErro != null)
                audioSource.PlayOneShot(somErro);

            // Devolve o livro errado ao sítio
            t = 0f;
            while (t < duracaoMovimento)
            {
                t += Time.deltaTime;
                float p = t / duracaoMovimento;
                livro.localPosition = Vector3.Lerp(posDest, posOrig, p);
                livro.localRotation = Quaternion.Lerp(rotDest, rotOrig, p);
                yield return null;
            }
            livro.localPosition = posOrig;
            livro.localRotation = rotOrig;
        }

        aAnimar = false;
    }

    public void AbrirPuzzle()
    {
        puzzleAtivo = true;
        indiceAtual = 0;

        if (painelEstanteUI != null)
            painelEstanteUI.SetActive(true);

        if (cameraEstante != null)
            cameraEstante.enabled = true;

        AtualizarPosicaoCursor();
    }

    public void FecharPuzzle()
    {
        puzzleAtivo = false;

        if (cursorSelecao != null)
            cursorSelecao.SetActive(false);

        if (painelEstanteUI != null)
            painelEstanteUI.SetActive(false);

        if (cameraEstante != null)
            cameraEstante.enabled = false;

        InteracaoEstante interacao = FindFirstObjectByType<InteracaoEstante>();
        if (interacao != null)
            interacao.DevolverControloPlayer2();
    }
}