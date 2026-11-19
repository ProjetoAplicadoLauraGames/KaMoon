using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configura��es de Mira (Raycast)")]
    [Tooltip("Arrasta a C�mara deste jogador (de onde sai o olhar)")]
    public Transform InteractorSource;
    [Tooltip("Dist�ncia m�xima em metros para alcan�ar objetos")]
    public float InteractRange = 3f;
    [Tooltip("Ponto/mira no centro do ecr� deste jogador")]
    public GameObject dot;

    [Header("Input deste Jogador")]
    public bool usarComando = true;
    public bool usarTeclado = false;

    [HideInInspector] public bool emPuzzle = false;

    void Update()
    {
        // Se n�o tiver c�mara associada ou estiver dentro de um puzzle, desliga o ponto e ignora
        if (InteractorSource == null || emPuzzle)
        {
            if (dot != null && dot.activeSelf) dot.SetActive(false);
            return;
        }

        // Sem lanterna nao ha interacao com a sala (o dot tambem nao acende).
        Battery bateria = GetComponentInParent<Battery>();
        int jogador = NetMatch.EmPartida ? GameSession.room
                    : (bateria != null ? bateria.PlayerNumber : (transform.root.name.EndsWith("2") ? 2 : 1));
        if (!GestorLanternas.LanternaLigada(jogador))
        {
            if (dot != null && dot.activeSelf) dot.SetActive(false);
            return;
        }

        Ray r = new Ray(InteractorSource.position, InteractorSource.forward);

        if (Physics.Raycast(r, out RaycastHit hit, InteractRange))
        {
            // Procura qualquer script no objeto (ou no pai dele) que implemente IInteragivel
            IInteragivel interagivel = hit.collider.GetComponentInParent<IInteragivel>();

            // Mant�m suporte direto para os cabos j� montados na cena
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
                        if (hit.collider.CompareTag("CorrectCable")) GameTimer.RegistarAcerto();
                        else GameTimer.RegistarErro();

                        if (NetMatch.EmPartida)
                            NetMatch.PedirCaboFisico(hit.collider.gameObject.name);
                        else
                            ProcessarCabo(hit.collider.gameObject);
                    }
                }
                return;
            }
        }

        // N�o est� a olhar para nada interag�vel
        if (dot != null && dot.activeSelf) dot.SetActive(false);
    }

    private bool PressionouBotaoInteragir()
    {
        if (usarComando && Gamepad.current != null)
        {
            // Bot�o A (Xbox) / X (PlayStation)
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
        Debug.Log(cabo.CompareTag("CorrectCable") ? "Correct cable cut!" : "Wrong cable cut!");
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