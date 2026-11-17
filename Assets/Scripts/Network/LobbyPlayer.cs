using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Um jogador no lobby. O Mirror cria uma instância por ligação (autoCreatePlayer),
/// por isso é este objeto que identifica quem é quem antes da partida.
/// Não usar um NetworkBehaviour colocado na StartMenu: o Mirror só trata objetos
/// spawnados, e a cena do menu não é carregada pelo servidor.
/// </summary>
public class LobbyPlayer : NetworkBehaviour
{
    public const string CenaPartida = "SampleScene";

    [SyncVar] public string displayName = "";
    [SyncVar] public int room;   // 0 = ainda não escolheu
    [SyncVar] public bool ready;

    /// <summary>connectionId -> sala, para a partida atribuir autoridade. Estático para sobreviver à mudança de cena.</summary>
    public static readonly Dictionary<int, int> SalaPorConexao = new Dictionary<int, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Repor() => SalaPorConexao.Clear();

    /// <summary>O LobbyPlayer deste cliente, ou null se ainda não existe.</summary>
    public static LobbyPlayer Local => NetworkClient.localPlayer != null
        ? NetworkClient.localPlayer.GetComponent<LobbyPlayer>()
        : null;

    public static List<LobbyPlayer> Todos()
        => new List<LobbyPlayer>(FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None));

    public override void OnStartLocalPlayer()
    {
        CmdConfigurar(LobbySession.Nome, LobbySession.Sala, LobbySession.Pronto);
        Debug.Log($"[Lobby] jogador local criado (sala {LobbySession.Sala}, pronto {LobbySession.Pronto}).");
    }

    /// <summary>Reenvia as escolhas guardadas localmente (usado sempre que mudam).</summary>
    /// <remarks>
    /// No anfitrião o estado tem de ser aplicado localmente: chamar o Command a partir
    /// do próprio servidor não estava a atualizar este LobbyPlayer (ficava em sala 0).
    /// </remarks>
    public void EnviarEscolhas(string nome, int sala, bool pronto)
    {
        if (isServer)
        {
            Aplicar(nome, sala, pronto);
            return;
        }

        CmdConfigurar(nome, sala, pronto);
    }

    /// <summary>O cliente pede ao servidor para guardar as suas escolhas.</summary>
    /// <remarks>
    /// Sem autoridade exigida pelo Mirror: em ligações criadas por descoberta o
    /// cliente pode não ser reconhecido como dono do objeto, e o Command era
    /// descartado em silêncio. A validação passa a ser explícita pelo remetente.
    /// </remarks>
    [Command(requiresAuthority = false)]
    public void CmdConfigurar(string nome, int sala, bool pronto, NetworkConnectionToClient sender = null)
    {
        if (sender != null && sender != connectionToClient)
        {
            Debug.LogWarning($"[Lobby] CmdConfigurar recusado: ligacao {sender.connectionId} nao e dona deste jogador.");
            return;
        }

        Aplicar(nome, sala, pronto);
    }

    /// <summary>Grava as escolhas. Corre no servidor (via Command ou diretamente no anfitrião).</summary>
    private void Aplicar(string nome, int sala, bool pronto)
    {
        displayName = string.IsNullOrWhiteSpace(nome) ? "Player" : nome.Trim();
        if (displayName.Length > 16) displayName = displayName.Substring(0, 16);

        room = LobbyRules.SalaValida(sala) ? sala : 0;
        ready = pronto && room != 0;   // sem sala escolhida não há "pronto"
    }

    /// <summary>Pedido de arranque. Só o anfitrião conta e a validação corre sempre no servidor.</summary>
    [Command(requiresAuthority = false)]
    public void CmdIniciar(NetworkConnectionToClient sender = null)
    {
        if (!isServer || sender != NetworkServer.localConnection)
            return;

        List<LobbyPlayer> jogadores = Todos();
        bool todosProntos = jogadores.Count == LobbyRules.MaxPlayers;
        foreach (LobbyPlayer jogador in jogadores)
            todosProntos &= jogador.ready;

        int salaA = jogadores.Count > 0 ? jogadores[0].room : 0;
        int salaB = jogadores.Count > 1 ? jogadores[1].room : 0;
        if (!LobbyRules.PodeComecar(jogadores.Count, todosProntos, salaA, salaB))
            return;

        SalaPorConexao.Clear();
        foreach (LobbyPlayer jogador in jogadores)
            SalaPorConexao[jogador.connectionToClient.connectionId] = jogador.room;

        Debug.Log($"[Lobby] a iniciar partida: sala {salaA} / sala {salaB}");
        NetworkManager.singleton.ServerChangeScene(CenaPartida);
    }
}
