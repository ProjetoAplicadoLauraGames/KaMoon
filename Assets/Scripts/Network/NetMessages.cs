using Mirror;
using UnityEngine;

/// <summary>
/// Ações que um cliente pode pedir ao servidor. O servidor decide sempre o
/// resultado; o cliente nunca altera o estado da partida por si.
/// </summary>
public enum NetAcao : byte
{
    Lanterna = 1,
    Keypad = 2,
    Cabo = 3,
    CaboFisico = 4,
    Reabastecer = 5,
    ApanharItem = 6,
    AjustarTempo = 7, // tempo = delta em segundos (negativo = penalidade)
}

/// <summary>Pose do avatar do próprio cliente (cliente -> servidor), 20 Hz.</summary>
public struct PoseJogadorMessage : NetworkMessage
{
    public Vector3 posicao;
    public float yaw;
    public float pitch;
    public float velocidade;
    public bool aFalar;
}

/// <summary>Pose reencaminhada pelo servidor para os restantes clientes.</summary>
public struct PoseRemotaMessage : NetworkMessage
{
    public byte sala;
    public Vector3 posicao;
    public float yaw;
    public float pitch;
    public float velocidade;
}

/// <summary>Pedido de ação de gameplay (cliente -> servidor).</summary>
public struct AcaoJogadorMessage : NetworkMessage
{
    public NetAcao acao;
    public int indice;
    public string texto;
    public float tempo;
}

/// <summary>Objeto consumido/removido na partida (servidor -> clientes).</summary>
public struct ObjetoRemovidoMessage : NetworkMessage
{
    public string nome;
}

/// <summary>Estado autoritativo da partida (servidor -> clientes).</summary>
public struct EstadoPartidaMessage : NetworkMessage
{
    public float tempo;
    public float bateriaUm;
    public float bateriaDois;
    public byte lanternaDono;
    public bool keypadResolvido;
    public bool caboResolvido;
    public bool derrota;
}
