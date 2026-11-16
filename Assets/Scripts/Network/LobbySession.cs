using UnityEngine;

/// <summary>Escolhas do jogador no menu, ainda antes de existir ligação de rede.</summary>
public static class LobbySession
{
    public static string Nome;
    public static int Sala;
    public static bool Pronto;

    // Com "Reload Domain" desativado, os estáticos persistem entre sessões de Play Mode.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Repor()
    {
        Nome = null;
        Sala = 0;
        Pronto = false;
    }
}
