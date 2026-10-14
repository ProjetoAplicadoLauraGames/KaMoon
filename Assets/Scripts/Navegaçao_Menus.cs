using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NavegadorMenuComando : MonoBehaviour
{
    [Header("Elementos de Navegacao")]
    [Tooltip("Lista ordenada de botoes por onde o jogador pode navegar")]
    public Button[] botoes;

    [Tooltip("A moldura ou seta visual que acompanha a selecao")]
    public RectTransform indicadorSelecao;

    [Header("Configuracao")]
    public bool orientacaoHorizontal = true; // True = Esquerda/Direita | False = Cima/Baixo
    public bool fecharComBotaoB = true;

    [Header("Evento de Saida (Opcional)")]
    public Button botaoSair;

    private int indiceAtual = 0;
    private bool menuAtivo = false;

    void OnEnable()
    {
        menuAtivo = true;
        indiceAtual = 0;
        AtualizarPosicaoIndicador();
    }

    void OnDisable()
    {
        menuAtivo = false;
        if (indicadorSelecao != null)
            indicadorSelecao.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!menuAtivo || botoes == null || botoes.Length == 0) return;

        bool retroceder = false;
        bool avancar = false;
        bool confirmar = false;
        bool cancelar = false;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            if (orientacaoHorizontal)
            {
                retroceder |= Gamepad.current.dpad.left.wasPressedThisFrame || (Gamepad.current.leftStick.left.wasPressedThisFrame);
                avancar    |= Gamepad.current.dpad.right.wasPressedThisFrame || (Gamepad.current.leftStick.right.wasPressedThisFrame);
            }
            else
            {
                retroceder |= Gamepad.current.dpad.up.wasPressedThisFrame || (Gamepad.current.leftStick.up.wasPressedThisFrame);
                avancar    |= Gamepad.current.dpad.down.wasPressedThisFrame || (Gamepad.current.leftStick.down.wasPressedThisFrame);
            }

            confirmar |= Gamepad.current.buttonSouth.wasPressedThisFrame; // Botao A
            cancelar  |= Gamepad.current.buttonEast.wasPressedThisFrame;  // Botao B
        }

        if (Keyboard.current != null)
        {
            if (orientacaoHorizontal)
            {
                retroceder |= Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
                avancar    |= Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
            }
            else
            {
                retroceder |= Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame;
                avancar    |= Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame;
            }

            confirmar |= Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;
            cancelar  |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif

        if (fecharComBotaoB && cancelar)
        {
            if (botaoSair != null) botaoSair.onClick.Invoke();
            else gameObject.SetActive(false);
            return;
        }

        if (retroceder)
        {
            MudarIndice(-1);
        }
        else if (avancar)
        {
            MudarIndice(1);
        }

        if (confirmar)
        {
            if (botoes[indiceAtual] != null && botoes[indiceAtual].interactable)
            {
                botoes[indiceAtual].onClick.Invoke();
            }
        }
    }

    private void MudarIndice(int direcao)
    {
        indiceAtual = (indiceAtual + direcao + botoes.Length) % botoes.Length;
        AtualizarPosicaoIndicador();
    }

    public void AtualizarPosicaoIndicador()
    {
        if (indicadorSelecao != null && botoes.Length > 0 && botoes[indiceAtual] != null)
        {
            indicadorSelecao.gameObject.SetActive(true);
            indicadorSelecao.position = botoes[indiceAtual].transform.position;
        }
    }
}
