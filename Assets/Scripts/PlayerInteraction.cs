using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Mira (Raycast)")]
    [Tooltip("Arrasta a Câmara deste jogador (de onde sai o olhar)")]
    public Transform InteractorSource;
    [Tooltip("Distância máxima em metros para alcançar objetos")]
    public float InteractRange = 3f;
    [Tooltip("Ponto/mira no centro do ecrã deste jogador")]
    public GameObject dot;

    [Header("Input deste Jogador")]
    public bool usarComando = true;
    public bool usarTeclado = false;

    [Header("Compatibilidade Cabos (Opcional)")]
    public GameOverManager gameOver;

    [HideInInspector] public bool emPuzzle = false;

    void Update()
    {
        // Se não tiver câmara associada ou estiver dentro de um puzzle, desliga o ponto e ignora
        if (InteractorSource == null || emPuzzle)
        {
            if (dot != null && dot.activeSelf) dot.SetActive(false);
            return;
        }

        Ray r = new Ray(InteractorSource.position, InteractorSource.forward);

        if (Physics.Raycast(r, out RaycastHit hit, InteractRange))
        {
            // Procura qualquer script no objeto (ou no pai dele) que implemente IInteragivel
            IInteragivel interagivel = hit.collider.GetComponentInParent<IInteragivel>();

            // Mantém suporte direto para os cabos já montados na cena
            bool eCabo = hit.collider.CompareTag("WrongCable") || hit.collider.CompareTag("CorrectCable");

            if (interagivel != null || eCabo)
            {
                if (dot != null) dot.SetActive(true);

                if (PressionouBotaoInteragir())
                {
                    if (interagivel != null)
                    {
                        interagivel.Interagir();
                    }
                    else if (eCabo)
                    {
                        ProcessarCabo(hit.collider.gameObject);
                    }
                }
                return;
            }
        }

        // Não está a olhar para nada interagível
        if (dot != null && dot.activeSelf) dot.SetActive(false);
    }

    private bool PressionouBotaoInteragir()
    {
        if (usarComando && Gamepad.current != null)
        {
            // Botão A (Xbox) / X (PlayStation)
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
        }

        if (usarTeclado && Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private void ProcessarCabo(GameObject cabo)
    {
        if (cabo.CompareTag("WrongCable"))
        {
            Debug.Log("Wrong cable cut!");
            if (gameOver != null) gameOver.GameOver();
        }
        else if (cabo.CompareTag("CorrectCable"))
        {
            Debug.Log("Correct cable cut!");
        }
        Destroy(cabo);
    }

    // Desenha o raio vermelho na janela Scene para veres exatamente para onde o jogador olha
    private void OnDrawGizmosSelected()
    {
        if (InteractorSource != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(InteractorSource.position, InteractorSource.forward * InteractRange);
        }
    }
}