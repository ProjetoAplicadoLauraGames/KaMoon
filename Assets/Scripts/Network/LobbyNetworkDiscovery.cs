using System;
using System.Net;
using Mirror;
using Mirror.Discovery;
using UnityEngine;

/// <summary>
/// Variante do <see cref="NetworkDiscovery"/> do Mirror que anuncia o IPv4 local da máquina
/// e o nome de quem criou a sala. O <c>KcpTransport.ServerUri()</c> devolve o hostname
/// (<c>Dns.GetHostName()</c>), que pode não resolver noutro computador da rede; o cliente
/// recebia assim um endereço inutilizável.
/// </summary>
public class LobbyNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, LobbyServerResponse>
{
    protected override ServerRequest GetRequest() => new ServerRequest();

    protected override LobbyServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
    {
        return new LobbyServerResponse
        {
            serverId = ServerId,
            uri = EnderecoAnunciado(),
            anfitrao = NomeAtual()
        };
    }

    protected override void ProcessResponse(LobbyServerResponse response, IPEndPoint endpoint)
    {
        // Mesma correção do Mirror: o host anunciado pode não resolver, mas sabemos
        // o IP real de quem respondeu ao pacote.
        response.EndPoint = endpoint;
        UriBuilder realUri = new UriBuilder(response.uri)
        {
            Host = endpoint.Address.ToString()
        };
        response.uri = realUri.Uri;

        OnServerFound.Invoke(response);
    }

    private Uri EnderecoAnunciado()
    {
        Uri baseUri = transport.ServerUri();

        string ip = IpLocal();
        if (string.IsNullOrEmpty(ip)) return baseUri;

        var builder = new UriBuilder(baseUri) { Host = ip };
        return builder.Uri;
    }

    private static string NomeAtual()
        => string.IsNullOrWhiteSpace(LobbySession.Nome)
            ? Environment.MachineName
            : LobbySession.Nome;

    private static string IpLocal()
    {
        try
        {
            foreach (IPAddress endereco in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (endereco.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(endereco))
                    return endereco.ToString();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Lobby] Não foi possível obter o IPv4 local: " + e.Message);
        }

        return null;
    }
}
