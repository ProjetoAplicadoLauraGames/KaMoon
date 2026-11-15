using UnityEngine;

public class InteracaoEstante : MonoBehaviour, IInteragivel
{
    [Header("Referências")]
    public PuzzleEstante3D puzzleScript;
    public Transform player2Transform;

    private CapsuleMovement player2Movement;
    private PlayerInteraction player2Interaction;

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
            player2Interaction = player2Transform.GetComponentInChildren<PlayerInteraction>();
        }
    }

    public void Interagir()
    {
        if (puzzleScript == null) return;

        // Evita reabrir se já estiver aberto
        if (puzzleScript.painelEstanteUI != null && puzzleScript.painelEstanteUI.activeSelf)
            return;

        // Bloqueia movimento e esconde a mira enquanto mexe nos livros
        if (player2Movement != null) player2Movement.enabled = false;
        if (player2Interaction != null) player2Interaction.emPuzzle = true;

        puzzleScript.AbrirPuzzle();
    }

    public void DevolverControloPlayer2()
    {
        if (player2Movement != null) player2Movement.enabled = true;
        if (player2Interaction != null) player2Interaction.emPuzzle = false;
    }
}