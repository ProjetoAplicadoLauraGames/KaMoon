using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ClearCanvas : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    private bool fechar = false;

    void Update()
    {
        #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                FecharCanvas();

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                FecharCanvas();
        #endif
    }

    public void FecharCanvas()
    {
        if (canvas != null)
        {
            for (int i = 4; i < canvas.transform.childCount; i++)
                Destroy(canvas.transform.GetChild(i).gameObject);
        }

        // Reativa o movimento do jogador e a mira central
        CapsuleMovement playerMovement = GetComponentInParent<CapsuleMovement>();
        if (playerMovement != null) playerMovement.enabled = true;

        PlayerInteraction playerInteraction = GetComponentInParent<PlayerInteraction>();
        if (playerInteraction != null) playerInteraction.emPuzzle = false;
    }
}
