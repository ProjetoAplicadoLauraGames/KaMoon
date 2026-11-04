using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InteracaoEstante : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("Distância máxima para o Player 2 interagir")]
    public float distanciaInteracao = 5f;

    [Header("Referências")]
    public PuzzleEstante3D puzzleScript;
    public GameObject avisoInteragirTexto;

    [Tooltip("Objeto do Player 2")]
    public Transform player2Transform;

    private CapsuleMovement player2Movement;

    void Start()
    {
        if (player2Transform == null)
        {
            GameObject p2 = GameObject.Find("Player2");
            if (p2 != null) player2Transform = p2.transform;
        }

        if (player2Transform != null)
        {
            player2Movement = player2Transform.GetComponent<CapsuleMovement>();
        }
    }

    void Update()
    {
        if (puzzleScript == null || player2Transform == null) return;

        bool puzzleAberto = puzzleScript.painelEstanteUI != null && puzzleScript.painelEstanteUI.activeSelf;

        // Distância entre o pivô da estante e o Player 2
        float dist = Vector3.Distance(transform.position, player2Transform.position);
        bool estaPerto = dist <= distanciaInteracao;

        if (avisoInteragirTexto != null)
            avisoInteragirTexto.SetActive(estaPerto && !puzzleAberto);

        if (estaPerto && !puzzleAberto)
        {
            bool interagir = false;

#if ENABLE_INPUT_SYSTEM
            // Suporta Comando (A, X ou Gatilho)
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame || // Botão A (Xbox) / X (PS)
                    Gamepad.current.buttonWest.wasPressedThisFrame ||  // Botão X (Xbox) / Quadrado (PS)
                    Gamepad.current.rightTrigger.wasPressedThisFrame)
                {
                    interagir = true;
                }
            }

            // Fallback de Teclado (Tecla E ou Espaço) para poderes testar sem comando ligado
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                    interagir = true;
            }
#endif

            if (interagir)
            {
                Debug.Log("<color=green>Sucesso! A abrir puzzle da estante para o Player 2.</color>");

                if (player2Movement != null)
                    player2Movement.enabled = false;

                puzzleScript.AbrirPuzzle();
            }
        }
    }

    public void DevolverControloPlayer2()
    {
        if (player2Movement != null)
            player2Movement.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}