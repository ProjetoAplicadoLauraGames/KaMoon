using UnityEngine;

/// <summary>Modo e sala escolhidos no menu, lidos pela partida depois do carregamento da cena.</summary>
public static class GameSession
{
    public static bool multiplayer;
    public static int room; // 0 = local; 1 ou 2 = sala escolhida no multiplayer

    // Com "Reload Domain" desativado, os estaticos persistem entre sessoes de Play Mode.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Repor()
    {
        multiplayer = false;
        room = 0;
    }
}
