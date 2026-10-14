using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NavegadorKeypad : MonoBehaviour
{
    [Header("Matriz de Botões (12 botões)")]
    [Tooltip("Arrasta os botões nesta ordem exata: 1, 2, 3, 4, 5, 6, 7, 8, 9, CLR, 0, Ok")]
    public Button[] botoes;

    [Header("Destaque Visual")]
    public RectTransform indicadorSelecao;

    [Header("Saída")]
    public Button botaoSair;

    private int indiceAtual = 0;
    private const int COLUNAS = 3;

    void OnEnable()
    {
        indiceAtual = 0;
        AtualizarPosicaoIndicador();
    }

    void OnDisable()
    {
        if (indicadorSelecao != null)
            indicadorSelecao.gameObject.SetActive(false);
    }

    void Update()
    {
        if (botoes == null || botoes.Length == 0) return;

        bool cima = false;
        bool baixo = false;
        bool esquerda = false;
        bool direita = false;
        bool confirmar = false;
        bool cancelar = false;

#if ENABLE_INPUT_SYSTEM
        // Input do Comando Xbox (Player 2)
        if (Gamepad.current != null)
        {
            esquerda |= Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame;
            direita  |= Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame;
            cima     |= Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame;
            baixo    |= Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame;

            confirmar |= Gamepad.current.buttonSouth.wasPressedThisFrame; // Botão A (confirma número)
            cancelar  |= Gamepad.current.buttonEast.wasPressedThisFrame;  // Botão B (fecha o keypad)
        }

        // Input de Teclado (para testes no editor)
        if (Keyboard.current != null)
        {
            esquerda |= Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
            direita  |= Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
            cima     |= Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame;
            baixo    |= Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame;

            confirmar |= Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame;
            cancelar  |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif

        // Fechar painel
        if (cancelar)
        {
            if (botaoSair != null) botaoSair.onClick.Invoke();
            else gameObject.SetActive(false);
            return;
        }

        // Movimentação na Grelha (3 colunas x 4 linhas)
        if (direita) MoverIndice(1);
        else if (esquerda) MoverIndice(-1);
        else if (baixo) MoverIndice(COLUNAS);
        else if (cima) MoverIndice(-COLUNAS);

        // Pressionar o botão selecionado
        if (confirmar)
        {
            if (botoes[indiceAtual] != null && botoes[indiceAtual].interactable)
            {
                botoes[indiceAtual].onClick.Invoke();
            }
        }
    }

    private void MoverIndice(int deslocamento)
    {
        int destino = indiceAtual + deslocamento;

        // Limita a navegação para não sair fora dos 12 botões
        if (destino >= 0 && destino < botoes.Length)
        {
            indiceAtual = destino;
            AtualizarPosicaoIndicador();
        }
    }

    private void AtualizarPosicaoIndicador()
    {
        if (indicadorSelecao != null && botoes.Length > 0 && botoes[indiceAtual] != null)
        {
            indicadorSelecao.gameObject.SetActive(true);
            indicadorSelecao.position = botoes[indiceAtual].transform.position;
        }
    }
}