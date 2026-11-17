using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Termina a partida quando um dos dois jogadores sai: qualquer dos lados volta ao menu inicial.
/// Vive no objeto do NetworkManager (DontDestroyOnLoad), por isso sobrevive à troca de cena.
/// </summary>
public class MatchGuard : MonoBehaviour
{
    private const string CenaMenu = "StartMenu";

    /// <summary>Motivo mostrado no lobby depois de a partida ser interrompida.</summary>
    public static string Aviso;

    private bool jaTeveDois;   // o servidor já teve os dois jogadores
    private bool jaLigado;     // este cliente já esteve ligado ao anfitrião
    private bool aSair;

    // shortcut: criação em runtime para não depender de reeditar a cena; passar o componente para o NetworkSetupBuilder quando o Editor estiver disponível.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Instalar()
    {
        if (NetworkManager.singleton == null) return;
        if (NetworkManager.singleton.GetComponent<MatchGuard>() == null)
            NetworkManager.singleton.gameObject.AddComponent<MatchGuard>();
    }

    private void Update()
    {
        string cena = SceneManager.GetActiveScene().name;
        if (cena == CenaMenu)
        {
            jaTeveDois = false;
            jaLigado = false;
            aSair = false;
            return;
        }

        // Marcado antes de entrar na partida, para apanhar quem cai durante o carregamento.
        if (NetworkClient.isConnected) jaLigado = true;
        if (NetworkServer.active && NetworkServer.connections.Count >= LobbyRules.MaxPlayers) jaTeveDois = true;

        if (aSair || cena != LobbyPlayer.CenaPartida) return;

        // O anfitrião conta como uma das ligações do servidor.
        if (LobbyRules.PartidaPerdida(NetworkServer.active, NetworkServer.connections.Count,
                jaTeveDois, NetworkClient.isConnected, jaLigado))
            Terminar();
    }

    private void Terminar()
    {
        aSair = true;
        Aviso = MenuLocalization.GetText(MenuTextKey.LobbyPeerLeft);
        Debug.Log("[Rede] partida terminada: um dos jogadores saiu.");

        Time.timeScale = 1f;

        // Para a rede, destrói o manager antigo e só depois carrega o menu: evita a
        // segunda cópia do NetworkManager (que o Mirror anunciava com um warning).
        RetornoAoMenu.Iniciar(CenaMenu);
    }
}
