using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LeitorPistaArmario : MonoBehaviour, IInteragivel
{
    [Header("Interface da Pista")]
    public GameObject painelNota; // O Painel_NotaArmario

    private GameObject portaArmario;
    private bool isOpen = false;

    private bool notaAberta = false;

    void Start()
    {
        if (painelNota != null)
            painelNota.SetActive(false);
        
        portaArmario = transform.GetChild(0).gameObject;
    }

    public void Interagir()
    {
        if (portaArmario != null)
        {
            if (!isOpen) {
                portaArmario.transform.Rotate(0f, -100f, 0f);
                isOpen = true;
            } else {
                portaArmario.transform.Rotate(0f, 100f, 0f);
                isOpen = false;}
        }
    }

    public void AbrirNota()
    {
        PauseMenu.NotificarModal(true);
        notaAberta = true;
        if (painelNota != null)
            painelNota.SetActive(true);

        Debug.Log("<color=cyan>[ARM�RIO]</color> Player 1 abriu a nota com a pista!");
    }

    public void FecharNota()
    {
        if (!notaAberta) return;
        PauseMenu.NotificarModal(false);
        notaAberta = false;
        if (painelNota != null)
            painelNota.SetActive(false);
    }
}