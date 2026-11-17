using System;
using System.Net;
using Mirror;

/// <summary>
/// Resposta de descoberta com o nome de quem criou a sala. O <see cref="ServerResponse"/>
/// do Mirror só traz uri/id, por isso a lista de salas mostrava o IP em vez do anfitrião.
/// </summary>
public struct LobbyServerResponse : NetworkMessage
{
    // Preenchido pelo cliente ao receber o pacote (não serializado pelo servidor).
    public IPEndPoint EndPoint { get; set; }

    public Uri uri;
    public long serverId;
    public string anfitrao;
}
