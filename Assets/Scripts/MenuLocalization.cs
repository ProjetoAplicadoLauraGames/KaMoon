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
    LobbyHelp,
    LobbyCreated,
    LobbyCreate,
    LobbyJoin,
    LobbyStart,
    LobbyRoomTitle,
    LobbyRoomOne,
    LobbyRoomTwo,
    LobbyName,
    LobbyIp,
    LobbyReady,
    LobbyNotReady,
    LobbyConnecting,
    LobbyFailed,
    LobbyWaiting,
    LobbyFree,
    LobbyRoomConflict,
    LobbyHost,
    LobbyHostIp,
    LobbyPeerLeft,
    LobbyRoomsTitle,
    LobbySearching,
    LobbyNoRooms,
    LobbyJoinRoom,
    LobbyHostAdvertised,
    PauseTitle,
    PauseResume,
    PauseSettings,
    PauseControls,
    PauseSensitivity,
    PauseControlsList,
    PauseSound
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
            case MenuTextKey.ModeIntro: return portugues ? "Escolhe como vais jogar: dois astronautas neste PC, ou multiplayer online com outro jogador na mesma rede." : "Choose how you play: two astronauts on this PC, or online multiplayer with another player on the same network.";
            case MenuTextKey.ModeLocal: return portugues ? "Local (split-screen)" : "Local (split-screen)";
            case MenuTextKey.ModeOnline: return portugues ? "Multiplayer coop" : "Multiplayer co-op";
            case MenuTextKey.LobbyTitle: return portugues ? "MULTIPLAYER COOP" : "MULTIPLAYER CO-OP";
            case MenuTextKey.LobbyHelp: return portugues ? "Quem cria a partida fica visível na rede; o outro entra pela lista de salas." : "Whoever creates the match becomes visible on the network; the other joins from the room list.";
            case MenuTextKey.LobbyCreated: return portugues ? "SALA LOCAL · 1 JOGADOR (TU)" : "LOCAL ROOM · 1 PLAYER (YOU)";
            case MenuTextKey.LobbyCreate: return portugues ? "Criar partida" : "Create match";
            case MenuTextKey.LobbyJoin: return portugues ? "Entrar" : "Join";
            case MenuTextKey.LobbyStart: return portugues ? "Iniciar missão" : "Start mission";
            case MenuTextKey.LobbyRoomTitle: return portugues ? "ESCOLHE A TUA SALA" : "CHOOSE YOUR ROOM";
            case MenuTextKey.LobbyRoomOne: return portugues ? "Sala 1 · Pistas" : "Room 1 · Clues";
            case MenuTextKey.LobbyRoomTwo: return portugues ? "Sala 2 · Desarmar" : "Room 2 · Defuse";
            case MenuTextKey.LobbyName: return portugues ? "Nome" : "Name";
            case MenuTextKey.LobbyIp: return portugues ? "IP do anfitrião (só para entrar)" : "Host IP (join only)";
            case MenuTextKey.LobbyReady: return portugues ? "Estou pronto" : "I'm ready";
            case MenuTextKey.LobbyNotReady: return portugues ? "Ainda não estou pronto" : "Not ready yet";
            case MenuTextKey.LobbyConnecting: return portugues ? "A ligar…" : "Connecting…";
            case MenuTextKey.LobbyFailed: return portugues ? "Não foi possível ligar. A sala já pode estar cheia ou ter fechado." : "Could not connect. The room may already be full or closed.";
            case MenuTextKey.LobbyWaiting: return portugues ? "À espera que o outro jogador escolha sala e fique pronto." : "Waiting for the other player to pick a room and get ready.";
            case MenuTextKey.LobbyFree: return portugues ? "livre" : "free";
            case MenuTextKey.LobbyRoomConflict: return portugues ? "As duas salas têm de ser diferentes." : "The two rooms must be different.";
            case MenuTextKey.LobbyHost: return portugues ? "ANFITRIÃO" : "HOST";
            case MenuTextKey.LobbyHostIp: return portugues ? "O teu IP: {0}  ·  porta {1}" : "Your IP: {0}  ·  port {1}";
            case MenuTextKey.LobbyRoomsTitle: return portugues ? "SALAS NA REDE" : "ROOMS ON THE NETWORK";
            case MenuTextKey.LobbySearching: return portugues ? "À procura de salas…" : "Searching for rooms…";
            case MenuTextKey.LobbyNoRooms: return portugues ? "Ainda não há salas. Cria uma partida ou espera pelo anfitrião." : "No rooms yet. Create a match or wait for the host.";
            case MenuTextKey.LobbyJoinRoom: return portugues ? "Entrar" : "Join";
            case MenuTextKey.LobbyHostAdvertised: return portugues ? "A tua sala está visível para os outros jogadores na rede." : "Your room is visible to other players on the network.";
            case MenuTextKey.LobbyPeerLeft: return portugues ? "A partida terminou: um dos jogadores saiu." : "The match ended: one of the players left.";
            case MenuTextKey.PauseTitle: return portugues ? "PAUSA" : "PAUSE";
            case MenuTextKey.PauseResume: return portugues ? "RESUMO" : "RESUME";
            case MenuTextKey.PauseSettings: return portugues ? "DEFINIÇÕES" : "SETTINGS";
            case MenuTextKey.PauseControls: return portugues ? "CONTROLOS" : "CONTROLS";
            case MenuTextKey.PauseSensitivity: return portugues ? "SENSIBILIDADE" : "SENSITIVITY";
            case MenuTextKey.PauseControlsList: return portugues
                ? "Mover · W A S D / setas · left stick\nOlhar · rato / right stick\nInteragir · E / A\nLanterna · F / Y\nRádio (manter) · V / LT\nInventário · 1–4 · G / R3\nCancelar · ESC / B"
                : "Move · W A S D / arrows · left stick\nLook · mouse / right stick\nInteract · E / A\nFlashlight · F / Y\nRadio (hold) · V / LT\nInventory · 1–4 · G / R3\nCancel · ESC / B";
            case MenuTextKey.PauseSound: return portugues ? "SOM" : "SOUND";
            default: return key.ToString();
        }
    }
}
