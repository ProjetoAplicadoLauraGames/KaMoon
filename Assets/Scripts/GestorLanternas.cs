using UnityEngine;
using UnityEngine.InputSystem;

public class GestorLanternas : MonoBehaviour
{
    [Header("Luzes das Lanternas (Spotlights)")]
    public Light lanternaP1; // Lanterna do Player 1 (Teclado)
    public Light lanternaP2; // Lanterna do Player 2 (teclado / comando)

    [Header("Configuracao Inicial")]
    public bool p1ComecaLigado = true;

    /// <summary>
    /// Regra da sala: so quem tem a propria lanterna ligada pode interagir.
    /// Em rede o dono e decidido pelo servidor (NetMatch.LanternaDono).
    /// </summary>
    public static bool LanternaLigada(int jogador)
    {
        if (NetMatch.EmPartida) return NetMatch.LanternaDono == jogador;

        GestorLanternas gestor = FindFirstObjectByType<GestorLanternas>();
        if (gestor == null) return true; // sem gestor na cena, mantem comportamento antigo

        Light luz = jogador == 1 ? gestor.lanternaP1 : gestor.lanternaP2;
        return luz != null && luz.enabled;
    }

    void Start()
    {
        // Garante que apenas uma lanterna comeca ativa no arranque da cena
        if (lanternaP1 != null && lanternaP2 != null)
        {
            bool p1Ativo = GameSession.multiplayer ? GameSession.room != 2 : p1ComecaLigado;
            lanternaP1.enabled = p1Ativo;
            lanternaP2.enabled = !p1Ativo;
        }
    }

    void Update()
    {
        // Em rede o servidor decide a lanterna ativa; aqui aplica-se o estado recebido
        // e o input local apenas envia o pedido (antes o F era ignorado em rede).
        if (NetMatch.EmPartida)
        {
            AplicarEstadoRede(NetMatch.LanternaDono);
            VerificarInputRede();
            return;
        }

        VerificarInputTeclado();
        VerificarInputCommando();
    }

    /// <summary>Pede ao servidor para alternar a lanterna deste jogador (rede).</summary>
    private void VerificarInputRede()
    {
        // Em rede os dois jogadores usam o mesmo esquema de teclado: a tecla F
        // alterna a propria lanterna. O servidor decide o dono pela ligacao (sala).
        bool pedido = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;

        // O botao Y do comando mantem-se como alternativa do Player 2.
        if (GameSession.room == 2 && Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
            pedido = true;

        if (pedido) NetMatch.PedirLanterna();
    }

    /// <summary>Aplica a lanterna ativa decidida pelo servidor (1 ou 2; 0 = nenhuma).</summary>
    private void AplicarEstadoRede(int dono)
    {
        if (lanternaP1 != null) lanternaP1.enabled = dono == 1;
        if (lanternaP2 != null) lanternaP2.enabled = dono == 2;
    }

    // --- CONTROLO DO PLAYER 1 (TECLADO) ---
    private void VerificarInputTeclado()
    {
        if (Keyboard.current == null) return;

        // Player 1 carrega no F (ou E) para "puxar" a lanterna para si
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            AtivarLanterna(GameSession.multiplayer && GameSession.room == 2 ? 2 : 1);
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

    // Regra estrita: ao ligar uma, desliga a outra instantaneamente.
    // Sem bateria (task #17), o jogador não pode ligar a lanterna.
    public void AtivarLanterna(int numeroJogador)
    {
        // Em rede o pedido vai para o servidor, que decide e replica.
        if (NetMatch.EmPartida)
        {
            NetMatch.PedirLanterna();
            return;
        }

        if (lanternaP1 == null || lanternaP2 == null) return;

        foreach (Battery battery in FindObjectsByType<Battery>(FindObjectsSortMode.None))
        {
            if (battery.PlayerNumber == numeroJogador && battery.IsDepleted)
            {
                Debug.Log($"<color=red>[LANTERNA]</color> Player {numeroJogador} sem bateria.");
                return;
            }
        }

        Light propria = numeroJogador == 1 ? lanternaP1 : lanternaP2;
        Light outra = numeroJogador == 1 ? lanternaP2 : lanternaP1;

        // Cada jogador liga e desliga a sua: ao desligar ficam as duas apagadas.
        if (propria.enabled)
        {
            propria.enabled = false;
            Debug.Log($"<color=yellow>[LANTERNA]</color> Player {numeroJogador} desligou a lanterna.");
            return;
        }

        propria.enabled = true;
        outra.enabled = false;
        Debug.Log($"<color=cyan>[LANTERNA]</color> Player {numeroJogador} ligou a lanterna.");
    }
}
