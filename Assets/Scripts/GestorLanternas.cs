using UnityEngine;
using UnityEngine.InputSystem;

public class GestorLanternas : MonoBehaviour
{
    [Header("Luzes das Lanternas (Spotlights)")]
    public Light lanternaP1; // Lanterna do Player 1 (Teclado)
    public Light lanternaP2; // Lanterna do Player 2 (Comando Xbox)

    [Header("Configuracao Inicial")]
    public bool p1ComecaLigado = true;

    void Start()
    {
        // Garante que apenas uma lanterna comeca ativa no arranque da cena
        if (lanternaP1 != null && lanternaP2 != null)
        {
            lanternaP1.enabled = p1ComecaLigado;
            lanternaP2.enabled = !p1ComecaLigado;
        }
    }

    void Update()
    {
        VerificarInputTeclado();
        VerificarInputCommando();
    }

    // --- CONTROLO DO PLAYER 1 (TECLADO) ---
    private void VerificarInputTeclado()
    {
        if (Keyboard.current == null) return;

        // Player 1 carrega no F (ou E) para "puxar" a lanterna para si
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            AtivarLanterna(1);
        }
    }

    // --- CONTROLO DO PLAYER 2 (COMANDO XBOX) ---
    private void VerificarInputCommando()
    {
        if (Gamepad.current == null) return;

        // Lanterna no Botao Y da Xbox (buttonNorth)
        if (Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            AtivarLanterna(2);
        }
    }

    // Regra estrita: ao ligar uma, desliga a outra instantaneamente
    public void AtivarLanterna(int numeroJogador)
    {
        if (lanternaP1 == null || lanternaP2 == null) return;

        if (numeroJogador == 1)
        {
            lanternaP1.enabled = true;
            lanternaP2.enabled = false;
            Debug.Log("<color=cyan>[LANTERNA]</color> Player 1 (Teclado) ligou a lanterna.");
        }
        else if (numeroJogador == 2)
        {
            lanternaP1.enabled = false;
            lanternaP2.enabled = true;
            Debug.Log("<color=green>[LANTERNA]</color> Player 2 (Comando) ligou a lanterna.");
        }
    }
}