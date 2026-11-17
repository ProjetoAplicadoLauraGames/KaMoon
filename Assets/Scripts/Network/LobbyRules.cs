/// <summary>
/// Regras do lobby em rede. Sem dependências do Unity para poderem ser testadas fora do Editor.
/// </summary>
public static class LobbyRules
{
    public const int MaxPlayers = 2;
    public const int Porta = 7777;

    public static bool SalaValida(int sala) => sala == 1 || sala == 2;

    /// <summary>Só arranca com os dois jogadores, ambos prontos e em salas diferentes.</summary>
    public static bool PodeComecar(int jogadores, bool todosProntos, int salaA, int salaB)
        => jogadores == MaxPlayers
           && todosProntos
           && SalaValida(salaA)
           && SalaValida(salaB)
           && salaA != salaB;

    /// <summary>Um dos jogadores desapareceu: a partida tem de terminar. O anfitrião conta como ligação do servidor.</summary>
    public static bool PartidaPerdida(bool servidor, int conexoes, bool teveDois, bool clienteLigado, bool esteveLigado)
        => servidor
            ? teveDois && conexoes < MaxPlayers
            : esteveLigado && !clienteLigado;
}
