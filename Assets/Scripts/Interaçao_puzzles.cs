using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InteracaoKeypad : MonoBehaviour, IInteragivel
{
    [Header("Painel do Keypad (Canvas)")]
    [Tooltip("Arrasta o objeto Painel_puzzleKeypad do Canvas")]
    public GameObject painelKeypadUI;

    [Header("Jogador que Interage")]
    [Tooltip("Arrasta o Player1 ou Player2 conforme quem usa este Keypad")]
    public Transform playerTransform;

    [Tooltip("Ativa se este jogador usar Rato para clicar nos números (ex: Player 1)")]
    public bool libertarRatoAoAbrir = true;

    private CapsuleMovement playerMovement;
    private PlayerInteraction playerInteraction;

    void Start()
    {
        if (playerTransform != null)
        {
            playerMovement = playerTransform.GetComponent<CapsuleMovement>();
            playerInteraction = playerTransform.GetComponentInChildren<PlayerInteraction>();
        }
    }

    void Update()
    {
        // Permite fechar o Keypad rapidamente carregando em ESC ou Botão B (Comando)
        if (painelKeypadUI != null && painelKeypadUI.activeSelf)
        {
            bool fechar = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                fechar = true;

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                fechar = true;
#endif

            if (fechar)
            {
                FecharKeypad();
            }
        }
    }

    public void Interagir()
    {
        if (painelKeypadUI == null || painelKeypadUI.activeSelf) return;

        // Bloqueia o movimento do jogador e desliga a mira central
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerInteraction != null) playerInteraction.emPuzzle = true;

        // Se for Player 1, mostra e liberta o cursor do rato para clicar nos números
        if (libertarRatoAoAbrir)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        painelKeypadUI.SetActive(true);
    }

    // Liga esta função ao botão "Sair/X" do Keypad ou chama-a no script do código quando acertar a senha
    public void FecharKeypad()
    {
        if (painelKeypadUI != null) painelKeypadUI.SetActive(false);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerInteraction != null) playerInteraction.emPuzzle = false;

        if (libertarRatoAoAbrir)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}