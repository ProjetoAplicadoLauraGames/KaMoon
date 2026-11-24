using UnityEngine;

public class CloseLocker : MonoBehaviour, IInteragivel
{
    private GameObject portaArmario;
    private LeitorPistaArmario pistaArmario;

    void Awake()
    {
        portaArmario = transform.gameObject;
        pistaArmario = GetComponentInParent<LeitorPistaArmario>();
    }

    public void Interagir()
    {
        
        if (pistaArmario.isOpen)
        {
            pistaArmario.isOpen = false;
            portaArmario.transform.Rotate(0f, 100f, 0f);
            pistaArmario.audioSource.clip = pistaArmario.abrirSom[1];
            pistaArmario.audioSource.Play();
        }
        else
        {
            pistaArmario.isOpen = true;
            portaArmario.transform.Rotate(0f, -100f, 0f);
            pistaArmario.audioSource.clip = pistaArmario.abrirSom[0];
            pistaArmario.audioSource.Play();
        }
    }
}
