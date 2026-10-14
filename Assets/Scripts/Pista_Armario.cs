using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LeitorPistaArmario : MonoBehaviour
{
    [Header("Interface da Pista")]
    public GameObject painelNota; // O Painel_NotaArmario

    [Header("Interacao")]
    public float distanciaInteracao = 3.5f;
    [Tooltip("Arrasta aqui o Player 1 (Teclado)")]
    public Transform jogadorP1;

    private bool notaAberta = false;

    void Start()
    {
        if (painelNota != null)
            painelNota.SetActive(false);
    }

    void Update()
    {
        if (jogadorP1 == null) return;

        float dist = Vector3.Distance(transform.position, jogadorP1.position);

        // Tecla E para abrir/fechar e tecla Escape para sair rapido
        bool carregarE = false;
        bool carregarEsc = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            carregarE = Keyboard.current.eKey.wasPressedThisFrame;
            carregarEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#else
        carregarE = Input.GetKeyDown(KeyCode.E);
        carregarEsc = Input.GetKeyDown(KeyCode.Escape);
#endif

        // Se a nota estiver aberta e carregar em Esc ou E -> Fecha
        if (notaAberta && (carregarE || carregarEsc))
        {
            FecharNota();
            return;
        }

        // Se estiver perto do armario e carregar em E -> Abre
        if (dist <= distanciaInteracao && carregarE && !notaAberta)
        {
            AbrirNota();
        }
    }

    public void AbrirNota()
    {
        notaAberta = true;
        if (painelNota != null)
            painelNota.SetActive(true);

        Debug.Log("<color=cyan>[ARMÁRIO]</color> Player 1 abriu a nota com a pista!");
    }

    public void FecharNota()
    {
        notaAberta = false;
        if (painelNota != null)
            painelNota.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaInteracao);
    }
}