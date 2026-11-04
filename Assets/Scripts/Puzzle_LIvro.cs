using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PuzzleEstante3D : MonoBehaviour
{
    [Header("Estrutura da UI / Câmara")]
    [Tooltip("Painel do Canvas com a Raw Image")]
    public GameObject painelEstanteUI;
    [Tooltip("Câmara que filma a estante (pode ser ativada só durante o puzzle)")]
    public Camera cameraEstante;

    [Header("Objetos 3D dos Livros")]
    [Tooltip("Arrasta os livros da prateleira de baixo por ordem (da esquerda para a direita)")]
    public Transform[] livrosPrateleiraBaixo;
    [Tooltip("Índice do livro alavanca (o azul, ex.: 4 se for o 5º livro)")]
    public int indiceLivroCorreto = 4;

    [Header("Movimento da Alavanca")]
    [Tooltip("Inclinação no eixo X ou avanço local para puxar o livro")]
    public Vector3 deslocamentoPuxar = new Vector3(0f, -0.05f, 0.12f);
    public float rotacaoPuxarX = 15f;
    public float duracaoMovimento = 0.35f;

    [Header("Indicador Visual de Seleção")]
    [Tooltip("Luz pontual (Spotlight/Point Light) ou objeto que ilumina o livro selecionado")]
    public Light luzSelecao;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip somClique;
    public AudioClip somErro;

    private int indiceAtual = 0;
    private bool resolvido = false;
    private bool aAnimar = false;

    // Guarda as poses originais dos livros
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
    }

    void OnEnable()
    {
        indiceAtual = 0;
        AtualizarDestaque();
    }

    void Update()
    {
        if (resolvido || aAnimar) return;

        bool esquerda = false;
        bool direita = false;
        bool confirmar = false;
        bool cancelar = false;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            esquerda |= Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame;
            direita  |= Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame;
            confirmar |= Gamepad.current.buttonSouth.wasPressedThisFrame; // Botão A
            cancelar  |= Gamepad.current.buttonEast.wasPressedThisFrame;  // Botão B
        }

        if (Keyboard.current != null)
        {
            esquerda |= Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
            direita  |= Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
            confirmar |= Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;
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
            AtualizarDestaque();
        }
    }

    private void AtualizarDestaque()
    {
        if (luzSelecao != null && livrosPrateleiraBaixo.Length > 0 && livrosPrateleiraBaixo[indiceAtual] != null)
        {
            luzSelecao.gameObject.SetActive(true);
            // Posiciona a luz logo à frente do livro selecionado
            luzSelecao.transform.position = livrosPrateleiraBaixo[indiceAtual].position + Vector3.up * 0.1f + livrosPrateleiraBaixo[indiceAtual].forward * 0.15f;
        }
    }

    private IEnumerator AcionarLivroAnimacao(int indice)
    {
        aAnimar = true;
        Transform livro = livrosPrateleiraBaixo[indice];

        Vector3 posOrig = posicoesIniciais[indice];
        Quaternion rotOrig = rotacoesIniciais[indice];

        Vector3 posDest = posOrig + deslocamentoPuxar;
        Quaternion rotDest = rotOrig * Quaternion.Euler(rotacaoPuxarX, 0f, 0f);

        // Movimento de puxar para fora / baixo
        float t = 0f;
        while (t < duracaoMovimento)
        {
            t += Time.deltaTime;
            float progresso = t / duracaoMovimento;
            livro.localPosition = Vector3.Lerp(posOrig, posDest, progresso);
            livro.localRotation = Quaternion.Lerp(rotOrig, rotDest, progresso);
            yield return null;
        }
        livro.localPosition = posDest;
        livro.localRotation = rotDest;

        if (audioSource != null && somClique != null)
            audioSource.PlayOneShot(somClique);

        yield return new WaitForSeconds(0.2f);

        if (indice == indiceLivroCorreto)
        {
            resolvido = true;
            Debug.Log("<color=green>LIVRO AZUL PUXADO! Alavanca ativada com sucesso.</color>");

            // Aguarda 1.2s para ver o livro acionado e fecha
            yield return new WaitForSeconds(1.2f);
            FecharPuzzle();
        }
        else
        {
            if (audioSource != null && somErro != null)
                audioSource.PlayOneShot(somErro);

            // Devolve o livro errado ao sítio original
            t = 0f;
            while (t < duracaoMovimento)
            {
                t += Time.deltaTime;
                float progresso = t / duracaoMovimento;
                livro.localPosition = Vector3.Lerp(posDest, posOrig, progresso);
                livro.localRotation = Quaternion.Lerp(rotDest, rotOrig, progresso);
                yield return null;
            }
            livro.localPosition = posOrig;
            livro.localRotation = rotOrig;
        }

        aAnimar = false;
    }

    public void AbrirPuzzle()
    {
        if (painelEstanteUI != null) painelEstanteUI.SetActive(true);
        if (cameraEstante != null) cameraEstante.enabled = true;
    }

    public void FecharPuzzle()
    {
        if (painelEstanteUI != null) painelEstanteUI.SetActive(false);
        if (cameraEstante != null) cameraEstante.enabled = false;
        if (luzSelecao != null) luzSelecao.gameObject.SetActive(false);

        // Devolve o controlo ao Player 2
        InteracaoEstante interacao = FindFirstObjectByType<InteracaoEstante>();
        if (interacao != null) interacao.DevolverControloPlayer2();
    }
}