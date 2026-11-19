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

/// <summary>
/// Frame de voz do próprio cliente (cliente -> servidor).
/// PCM mono 16 kHz, 16 bits com sinal, little-endian. Vai no canal não fiável
/// em blocos de poucos milissegundos: perder um frame é preferível a atrasar a voz.
/// </summary>
public struct VozJogadorMessage : NetworkMessage
{
    public byte[] pcm;
}

/// <summary>
/// Frame de voz reencaminhado pelo servidor para os restantes jogadores.
/// O servidor preenche <see cref="sala"/>; o cliente nunca decide quem fala.
/// </summary>
public struct VozRemotaMessage : NetworkMessage
{
    public byte sala;
    public byte[] pcm;
}
