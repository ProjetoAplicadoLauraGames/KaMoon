using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LeitorPistaArmario : MonoBehaviour, IInteragivel
{
    [Header("Interface da Pista")]
    public GameObject painelNota; // O Painel_NotaArmario

    private GameObject portaArmario;
    [HideInInspector] public bool isOpen = false;

    private bool notaAberta = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] abrirSom;

    private Collider lockerCollider;
    private Collider doorCollider;

    void Start()
    {
        if (painelNota != null)
            painelNota.SetActive(false);
        
        portaArmario = transform.GetChild(0).gameObject;
        lockerCollider = GetComponent<Collider>();
        doorCollider = transform.GetChild(0).GetComponent<Collider>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                FecharNota();

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                FecharNota();
        #endif
    }

    public void Interagir()
    {
        if (portaArmario != null)
        {
            if (isOpen) {
                AbrirNota();
            }
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