using UnityEngine;

public class InterruptorComutador : MonoBehaviour
{
    [Header("Luzes das Salas")]
    public Light luzSala1; // Sala das Pistas
    public Light luzSala2; // Sala da Bomba

    [Header("Material do Interruptor (Opcional)")]
    public Renderer rendererInterruptor;

    private bool jogadorPorPerto = false;

    void Start()
    {
        // Garante o estado inicial: apenas uma luz pode estar ligada
        if (luzSala1 != null && luzSala2 != null)
        {
            luzSala1.enabled = true;
            luzSala2.enabled = false;
        }
    }

    void Update()
    {
        // Se o jogador estiver ao pe do interruptor e carregar na tecla E
        if (jogadorPorPerto && Input.GetKeyDown(KeyCode.E))
        {
            AlternarLuz();
        }
    }

    public void AlternarLuz()
    {
        if (luzSala1 == null || luzSala2 == null) return;

        // Inverte o estado: se a 1 esta ligada, passa a falsa; a 2 fica com o oposto da 1
        bool estadoLuz1 = !luzSala1.enabled;

        luzSala1.enabled = estadoLuz1;
        luzSala2.enabled = !estadoLuz1;

        Debug.Log("Luz comutada! Sala 1: " + luzSala1.enabled + " | Sala 2: " + luzSala2.enabled);
    }

    // Deteta se o astronauta entrou na area do interruptor
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorPorPerto = true;
        }
    }

    // Deteta se o astronauta se afastou
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorPorPerto = false;
        }
    }
}