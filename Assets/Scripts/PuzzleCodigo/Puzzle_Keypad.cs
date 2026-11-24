using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Numerics;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Puzzle_Keypad : MonoBehaviour, IInteragivel
{
    [Header("Interface UI")]
    public GameObject painelKeypad;
    public TextMeshProUGUI textoDisplay;
    public TextMeshProUGUI textoTampa;

    [Header("Grelha de 12 Botões (Ordem exata: 1,2,3, 4,5,6, 7,8,9, CLR,0,OK)")]
    public Button[] botoesGrelha = new Button[12];

    [Header("Configuração do Código")]
    public string codigoCorreto = "4281";
    public int limiteDigitos = 4;

    [Header("Jogador (Player 2 - Comando)")]
    public Transform jogadorInterativo;

    [Header("Tampa do Keypad")]
    public GameObject tampaKeypad;
    private bool tampaFora = true;
    public Hotbar hotbar;
    public Transform destinoTampa;

    [Header("Audio Placa")]
    [SerializeField] private AudioSource audioSource;

    private string codigoAtual = "";
    private bool puzzleResolvido = false;
    private int indiceBotaoAtual = 0;
    private float tempoAbertura = 0f;
    private float typingSpeed = 0.04f;

    private CapsuleMovement playerMovement;
    private PlayerInteraction playerInteraction;

    // Valores correspondentes aos 12 botões da grelha 3x4
    private readonly string[] valoresGrelha = new string[]
    {
        "1", "2", "3",
        "4", "5", "6",
        "7", "8", "9",
        "CLR", "0", "OK"
    };

    void Start()
    {
        if (jogadorInterativo == null)
        {
            GameObject p2 = GameObject.Find("Player2");
            if (p2 != null) jogadorInterativo = p2.transform;
        }

        if (jogadorInterativo != null)
        {
            playerMovement = jogadorInterativo.GetComponent<CapsuleMovement>();
            playerInteraction = jogadorInterativo.GetComponentInChildren<PlayerInteraction>();
        }

        // Desliga a navegação automática do Unity para não descalibrar a grelha
        for (int i = 0; i < botoesGrelha.Length; i++)
        {
            if (botoesGrelha[i] != null)
            {
                Navigation nav = botoesGrelha[i].navigation;
                nav.mode = Navigation.Mode.None;
                botoesGrelha[i].navigation = nav;
            }
        }

        if (painelKeypad != null)
            painelKeypad.SetActive(false);

        AtualizarDisplay();
    }

    public void Interagir()
    {
        if (puzzleResolvido || painelKeypad == null || painelKeypad.activeSelf) return;

        if (!tampaFora) //&& !hotbar.HasItem("screwdriver"))
        {
            StartCoroutine(TypeTextRoutine("Might need a screwdriver to open this."));
            return;
        }
        else if (!tampaFora && hotbar.HasItem("screwdriver"))
        {
            tampaFora = true;
            if (tampaKeypad != null) tampaKeypad.transform.position= destinoTampa.position;
            StartCoroutine(TypeTextRoutine("The keypad is now accessible."));
            if (audioSource != null) audioSource.Play();
            return;
        }


        codigoAtual = "";
        indiceBotaoAtual = 0;
        tempoAbertura = Time.time;
        AtualizarDisplay();

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerInteraction != null) playerInteraction.emPuzzle = true;

        painelKeypad.SetActive(true);
        PauseMenu.NotificarModal(true);
        AtualizarDestaqueBotao();
    }

    void Update()
    {
        if (NetMatch.EmPartida && NetMatch.KeypadResolvido)
        {
            puzzleResolvido = true;
            if (textoDisplay != null) textoDisplay.text = "OK!";
            FecharKeypad();
            return;
        }

        if (painelKeypad == null || !painelKeypad.activeSelf || puzzleResolvido) return;

        // Ignora inputs durante os primeiros 0.15s para o botão A que abriu o painel não clicar logo no "1"
        if (Time.time - tempoAbertura < 0.15f) return;

#if ENABLE_INPUT_SYSTEM
        bool esquerda = false, direita = false, cima = false, baixo = false;
        bool confirmar = false, sair = false;

        if (Gamepad.current != null)
        {
            esquerda  |= Gamepad.current.dpad.left.wasPressedThisFrame  || Gamepad.current.leftStick.left.wasPressedThisFrame;
            direita   |= Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame;
            cima      |= Gamepad.current.dpad.up.wasPressedThisFrame    || Gamepad.current.leftStick.up.wasPressedThisFrame;
            baixo     |= Gamepad.current.dpad.down.wasPressedThisFrame  || Gamepad.current.leftStick.down.wasPressedThisFrame;
            confirmar |= Gamepad.current.buttonSouth.wasPressedThisFrame; // Botão A
            sair      |= Gamepad.current.buttonEast.wasPressedThisFrame;  // Botão B
        }

        if (Keyboard.current != null)
        {
            esquerda  |= Keyboard.current.leftArrowKey.wasPressedThisFrame;
            direita   |= Keyboard.current.rightArrowKey.wasPressedThisFrame;
            cima      |= Keyboard.current.upArrowKey.wasPressedThisFrame;
            baixo     |= Keyboard.current.downArrowKey.wasPressedThisFrame;
            confirmar |= Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
            sair      |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        if (sair)
        {
            FecharKeypad();
            return;
        }

        // Navegação matemática exata na grelha 3 colunas x 4 linhas
        int coluna = indiceBotaoAtual % 3;
        int linha = indiceBotaoAtual / 3;

        if (esquerda && coluna > 0)
        {
            indiceBotaoAtual -= 1;
            AtualizarDestaqueBotao();
        }
        else if (direita && coluna < 2)
        {
            indiceBotaoAtual += 1;
            AtualizarDestaqueBotao();
        }
        else if (cima && linha > 0)
        {
            indiceBotaoAtual -= 3;
            AtualizarDestaqueBotao();
        }
        else if (baixo && linha < 3)
        {
            indiceBotaoAtual += 3;
            AtualizarDestaqueBotao();
        }

        if (confirmar)
        {
            ExecutarBotaoAtual();
        }
#endif
    }

    private void AtualizarDestaqueBotao()
    {
        if (EventSystem.current == null || botoesGrelha.Length == 0) return;

        if (indiceBotaoAtual >= 0 && indiceBotaoAtual < botoesGrelha.Length && botoesGrelha[indiceBotaoAtual] != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(botoesGrelha[indiceBotaoAtual].gameObject);
        }
    }

    private void ExecutarBotaoAtual()
    {
        if (indiceBotaoAtual < 0 || indiceBotaoAtual >= valoresGrelha.Length) return;

        string acao = valoresGrelha[indiceBotaoAtual];

        if (acao == "CLR")
        {
            ApagarUltimoDigito();
        }
        else if (acao == "OK")
        {
            VerificarCodigo();
        }
        else
        {
            AdicionarDigito(acao);
        }
    }

    public void AdicionarDigito(string digito)
    {
        if (puzzleResolvido || codigoAtual.Length >= limiteDigitos) return;

        codigoAtual += digito;
        AtualizarDisplay();
    }

    public void InserirDigito(string digito) => AdicionarDigito(digito);

    public void ApagarUltimoDigito()
    {
        if (puzzleResolvido) return;

        codigoAtual = "";
        AtualizarDisplay();
    }

    public void Limpar() => ApagarUltimoDigito();

    public void VerificarCodigo()
    {
        if (puzzleResolvido) return;

        if (NetMatch.EmPartida)
        {
            string enviado = codigoAtual;
            if (enviado == codigoCorreto) GameTimer.RegistarAcerto();
            else GameTimer.RegistarErro();
            NetMatch.PedirKeypad(enviado);
            codigoAtual = "";
            AtualizarDisplay();
            return;
        }

        if (codigoAtual == codigoCorreto)
        {
            GameTimer.RegistarAcerto();
            puzzleResolvido = true;
            if (textoDisplay != null) textoDisplay.text = "OK!";
            Debug.Log("<color=green>Código do Keypad correto!</color>");
            Invoke(nameof(FecharKeypad), 1f);
        }
        else
        {
            GameTimer.RegistarErro();
            Debug.Log("<color=red>Código errado!</color>");
            codigoAtual = "";
            AtualizarDisplay();
        }
    }

    public void Submeter() => VerificarCodigo();

    public void AbrirKeypad() => Interagir();

    private void AtualizarDisplay()
    {
        if (textoDisplay != null)
            textoDisplay.text = codigoAtual;
    }

    public void FecharKeypad()
    {
        if (painelKeypad != null) painelKeypad.SetActive(false);
        PauseMenu.NotificarModal(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerInteraction != null) playerInteraction.emPuzzle = false;
    }

    private IEnumerator TypeTextRoutine(string textToType)
    {
        textoTampa.text = textToType;
        textoTampa.maxVisibleCharacters = 0;

        // Força o TMP a calcular quantos caracteres visíveis existem (ignorando tags)
        textoTampa.ForceMeshUpdate();
        int totalVisibleCharacters = textoTampa.textInfo.characterCount;

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            textoTampa.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
        yield return new WaitForSeconds(2f);
        StartCoroutine(FadeOutTextRoutine());
    }

    private IEnumerator FadeOutTextRoutine()
    {
        // Força o TMP a calcular quantos caracteres visíveis existem (ignorando tags)
        textoTampa.ForceMeshUpdate();
        int totalVisibleCharacters = textoTampa.textInfo.characterCount;

        for (int i = totalVisibleCharacters - 1; i >= 0; i--)
        {
            textoTampa.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
        textoTampa.text = "";
    }



}

// Compatibility for scenes and prefabs created before the keypad was renamed.
public class PuzzleKeypad : Puzzle_Keypad
{
}