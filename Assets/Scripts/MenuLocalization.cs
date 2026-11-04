using System;
using UnityEngine;

public enum MenuTextKey
{
    Play,
    Options,
    Quit,
    Language,
    MasterVolume,
    MuteSound,
    Fullscreen,
    Resolution,
    Quality,
    Back,
    MissionBriefing,
    MissionDescription,
    PlayerOneRole,
    PlayerTwoRole,
    MissionStart,
    ReduceMotion,
    ModeTitle,
    ModeIntro,
    ModeLocal,
    ModeOnline,
    LobbyTitle,
    LobbyUnavailable,
    LobbyCreated,
    LobbyCreate,
    LobbyJoin,
    LobbyStart,
    LobbyRoomTitle,
    LobbyRoomOne,
    LobbyRoomTwo
}

public static class MenuLocalization
{
    private const string KeyLanguage = "MenuLanguage";
    public const int LanguageCount = 2;

    public static event Action LanguageChanged;

    // A ordem corresponde aos valores guardados em PlayerPrefs.
    public static string[] LanguageNames => new[] { "Português", "English" };
    public static int CurrentLanguage => Mathf.Clamp(PlayerPrefs.GetInt(KeyLanguage, 0), 0, LanguageCount - 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimparEventos()
    {
        LanguageChanged = null;
    }

    public static void SetLanguage(int index)
    {
        index = Mathf.Clamp(index, 0, LanguageCount - 1);
        if (CurrentLanguage == index) return;

        PlayerPrefs.SetInt(KeyLanguage, index);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static string GetText(MenuTextKey key)
    {
        bool portugues = CurrentLanguage == 0;
        switch (key)
        {
            case MenuTextKey.Play: return portugues ? "Jogar" : "Play";
            case MenuTextKey.Options: return portugues ? "Opções" : "Options";
            case MenuTextKey.Quit: return portugues ? "Sair" : "Quit";
            case MenuTextKey.Language: return portugues ? "Idioma" : "Language";
            case MenuTextKey.MasterVolume: return portugues ? "Volume geral" : "Master volume";
            case MenuTextKey.MuteSound: return portugues ? "Silenciar som" : "Mute sound";
            case MenuTextKey.Fullscreen: return portugues ? "Ecrã inteiro" : "Fullscreen";
            case MenuTextKey.Resolution: return portugues ? "Resolução" : "Resolution";
            case MenuTextKey.Quality: return portugues ? "Qualidade gráfica" : "Graphics quality";
            case MenuTextKey.Back: return portugues ? "Voltar" : "Back";
            case MenuTextKey.MissionBriefing: return portugues ? "BRIEFING DA MISSÃO" : "MISSION BRIEFING";
            case MenuTextKey.MissionDescription: return portugues ? "Uma estação lunar entrou em autodestruição. Trabalhem em conjunto para desarmar a ameaça antes que o tempo termine." : "A lunar station has entered self-destruct. Work together to disarm the threat before time runs out.";
            case MenuTextKey.PlayerOneRole: return portugues ? "P1  /  SISTEMAS\nInvestiga a estação e comunica as pistas." : "P1  /  SYSTEMS\nInvestigate the station and communicate clues.";
            case MenuTextKey.PlayerTwoRole: return portugues ? "P2  /  DESMINAGEM\nInterage com os módulos e executa as instruções." : "P2  /  DEFUSAL\nInteract with the modules and follow instructions.";
            case MenuTextKey.MissionStart: return portugues ? "INICIAR MISSÃO" : "START MISSION";
            case MenuTextKey.ReduceMotion: return portugues ? "Reduzir movimento" : "Reduce motion";
            case MenuTextKey.ModeTitle: return portugues ? "MODO DE JOGO" : "GAME MODE";
            case MenuTextKey.ModeIntro: return portugues ? "Escolhe como vais jogar: dois astronautas neste PC, ou multiplayer online (a ligação ainda não está disponível)." : "Choose how you play: two astronauts on this PC, or online multiplayer (connection not available yet).";
            case MenuTextKey.ModeLocal: return portugues ? "Local (split-screen)" : "Local (split-screen)";
            case MenuTextKey.ModeOnline: return portugues ? "Multiplayer coop" : "Multiplayer co-op";
            case MenuTextKey.LobbyTitle: return portugues ? "MULTIPLAYER COOP" : "MULTIPLAYER CO-OP";
            case MenuTextKey.LobbyUnavailable: return portugues ? "A ligação online ainda não está disponível. Podes criar uma sala local e treinar sozinho." : "Online connection is not available yet. You can create a local room and practise alone.";
            case MenuTextKey.LobbyCreated: return portugues ? "SALA LOCAL · 1 JOGADOR (TU)" : "LOCAL ROOM · 1 PLAYER (YOU)";
            case MenuTextKey.LobbyCreate: return portugues ? "Criar sala" : "Create room";
            case MenuTextKey.LobbyJoin: return portugues ? "Entrar em sala" : "Join room";
            case MenuTextKey.LobbyStart: return portugues ? "Iniciar" : "Start";
            case MenuTextKey.LobbyRoomTitle: return portugues ? "ESCOLHE A TUA SALA" : "CHOOSE YOUR ROOM";
            case MenuTextKey.LobbyRoomOne: return portugues ? "Sala 1 · Pistas" : "Room 1 · Clues";
            case MenuTextKey.LobbyRoomTwo: return portugues ? "Sala 2 · Desarmar" : "Room 2 · Defuse";
            default: return key.ToString();
        }
    }
}
