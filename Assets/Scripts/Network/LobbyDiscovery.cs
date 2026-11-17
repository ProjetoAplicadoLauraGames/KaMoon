using System.Collections.Generic;
using Mirror;
using Mirror.Discovery;
using UnityEngine;

/// <summary>
/// Descoberta de salas na rede local (difusão UDP do Mirror).
///
/// Quem cria a partida chama <see cref="Anunciar"/> e passa a responder aos pedidos
/// de descoberta; quem entra chama <see cref="Procurar"/> e recebe as salas em
/// <see cref="Lista"/>. Vive no objeto do NetworkManager (DontDestroyOnLoad) e é
/// criado em runtime, por isso não exige alterações de cena.
/// </summary>
[DisallowMultipleComponent]
public class LobbyDiscovery : MonoBehaviour
{
    // Só responde a pedidos de instâncias KaMoon (o Mirror compara este valor).
    private const long HandshakeKaMoon = 0x4B614D6F6F6E;   // "KaMoon"

    public static LobbyDiscovery Instance { get; private set; }

    private LobbyNetworkDiscovery descoberta;
    private readonly Dictionary<long, LobbyServerResponse> salas = new Dictionary<long, LobbyServerResponse>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Instalar()
    {
        if (NetworkManager.singleton == null) return;
        if (NetworkManager.singleton.GetComponent<LobbyDiscovery>() == null)
            NetworkManager.singleton.gameObject.AddComponent<LobbyDiscovery>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Começa a responder aos pedidos de descoberta (anfitrião).</summary>
    public static void Anunciar()
    {
        if (Instance == null) return;

        LobbyNetworkDiscovery d = Instance.Descoberta();
        if (d == null) return;

        try
        {
            d.AdvertiseServer();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Lobby] Não foi possível anunciar a sala: " + e.Message);
        }
    }

    /// <summary>Procura salas na rede local (quem entra).</summary>
    public static void Procurar()
    {
        if (Instance == null) return;

        Instance.salas.Clear();

        LobbyNetworkDiscovery d = Instance.Descoberta();
        if (d == null) return;

        try
        {
            d.StartDiscovery();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Lobby] Não foi possível procurar salas: " + e.Message);
        }
    }

    /// <summary>Pára o anúncio e a procura (ao ligar, ao sair do lobby ou ao começar a partida).</summary>
    public static void Parar()
    {
        if (Instance == null) return;

        if (Instance.descoberta != null) Instance.descoberta.StopDiscovery();
        Instance.salas.Clear();
    }

    /// <summary>Salas encontradas, ordenadas por id de servidor.</summary>
    public static List<LobbyServerResponse> Lista()
    {
        if (Instance == null) return new List<LobbyServerResponse>();

        List<LobbyServerResponse> lista = new List<LobbyServerResponse>(Instance.salas.Values);
        lista.Sort((a, b) => a.serverId.CompareTo(b.serverId));
        return lista;
    }

    private LobbyNetworkDiscovery Descoberta()
    {
        NetworkManager manager = NetworkManager.singleton;
        if (manager == null || !LobbyNetworkDiscovery.SupportedOnThisPlatform) return null;

        if (manager.transport == null)
        {
            Debug.LogWarning("[Lobby] NetworkManager sem transporte; a descoberta fica indisponível.");
            return null;
        }

        if (descoberta == null)
        {
            descoberta = manager.GetComponent<LobbyNetworkDiscovery>();
            if (descoberta == null)
                descoberta = manager.gameObject.AddComponent<LobbyNetworkDiscovery>();

            // Num componente criado em runtime os UnityEvent ficam null (só o
            // serializador do Inspector os instancia), e RemoveAllListeners() rebentava.
            if (descoberta.OnServerFound == null)
                descoberta.OnServerFound = new ServerFoundUnityEvent<LobbyServerResponse>();

            descoberta.transport = manager.transport;
            descoberta.secretHandshake = HandshakeKaMoon;
            descoberta.OnServerFound.RemoveAllListeners();
            descoberta.OnServerFound.AddListener(OnSalaEncontrada);
        }

        return descoberta;
    }

    private void OnSalaEncontrada(LobbyServerResponse resposta)
    {
        salas[resposta.serverId] = resposta;
    }
}
