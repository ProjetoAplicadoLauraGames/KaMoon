#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Constroi o menu de modo e o lobby na cena StartMenu, sem guardar a cena automaticamente.</summary>
public static class ModeMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/StartMenu.unity";
    private const string FontPath = "Assets/Fonts/KaMoon/Rajdhani-Menu.asset";
    private const int UILayer = 5;

    private static readonly Color PanelColor = new Color(0.015f, 0.04f, 0.055f, 0.98f);
    private static readonly Color TextColor = new Color(0.87f, 0.94f, 0.91f);
    private static readonly Color MutedColor = new Color(0.62f, 0.72f, 0.7f);
    private static readonly Color AccentColor = new Color(0.9f, 0.6f, 0.25f);

    [MenuItem("Tools/KaMoon/Build Mode Menu")]
    public static void Build()
    {
        var cena = SceneManager.GetActiveScene();
        if (Application.isPlaying || cena.path != ScenePath)
        {
            Debug.LogError("Abre Assets/Scenes/StartMenu.unity e sai do Play Mode antes de construir o menu de modo.");
            return;
        }

        Canvas canvas = null;
        MainMenu mainMenu = null;
        foreach (var root in cena.GetRootGameObjects())
        {
            foreach (var candidato in root.GetComponentsInChildren<Canvas>(true))
            {
                var menu = candidato.GetComponentInChildren<MainMenu>(true);
                if (menu == null) continue;
                canvas = candidato;
                mainMenu = menu;
                break;
            }
            if (mainMenu != null) break;
        }

        if (mainMenu == null)
        {
            Debug.LogError("StartMenu precisa de um Canvas com o componente MainMenu.");
            return;
        }

        var playButton = EncontrarBotao(mainMenu, "OpenMode");
        if (playButton == null) playButton = EncontrarBotao(mainMenu, "OpenBriefing");
        if (playButton == null)
        {
            Debug.LogError("Nao foi encontrado o botao Jogar ligado a MainMenu.OpenMode/OpenBriefing.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int grupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build KaMoon Mode Menu");

        RemoverGerados(canvas.transform, "ModePanel");
        RemoverGerados(canvas.transform, "LobbyPanel");

        var mode = canvas.GetComponent<ModeMenu>();
        if (mode == null) mode = Undo.AddComponent<ModeMenu>(canvas.gameObject);
        Undo.RecordObject(mode, "Link mode menu");

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) font = TMP_Settings.defaultFontAsset;

        // ---- Painel de modo ----
        var modePanel = CriarPainel(canvas.transform, "ModePanel");
        CriarLabel(modePanel.transform, "Title", MenuTextKey.ModeTitle,
            new Vector2(0f, 250f), new Vector2(800f, 70f), 40, FontStyles.Bold, TextColor, font);
        CriarLabel(modePanel.transform, "Intro", MenuTextKey.ModeIntro,
            new Vector2(0f, 150f), new Vector2(820f, 100f), 22, FontStyles.Normal, MutedColor, font);

        var modeLocal = CriarBotao(modePanel.transform, "Local", MenuTextKey.ModeLocal,
            new Vector2(-160f, 10f), font);
        var modeOnline = CriarBotao(modePanel.transform, "Multiplayer", MenuTextKey.ModeOnline,
            new Vector2(160f, 10f), font);
        var modeVoltar = CriarBotao(modePanel.transform, "Back", MenuTextKey.Back,
            new Vector2(0f, -220f), font);
        DefinirCamadaUI(modePanel);

        UnityEventTools.AddVoidPersistentListener(modeLocal.onClick, mode.Local);
        UnityEventTools.AddVoidPersistentListener(modeOnline.onClick, mode.AbrirLobby);
        UnityEventTools.AddVoidPersistentListener(modeVoltar.onClick, mode.Voltar);
        var modeCancel = Undo.AddComponent<MenuCancel>(modePanel);
        modeCancel.backButton = modeVoltar;

        // ---- Painel de lobby ----
        var lobbyPanel = CriarPainel(canvas.transform, "LobbyPanel");
        CriarLabel(lobbyPanel.transform, "Title", MenuTextKey.LobbyTitle,
            new Vector2(0f, 450f), new Vector2(620f, 70f), 40, FontStyles.Bold, TextColor, font);
        var status = CriarLabel(lobbyPanel.transform, "HostBadge", MenuTextKey.LobbyHost,
            new Vector2(350f, 455f), new Vector2(180f, 36f), 18, FontStyles.Bold, AccentColor, font);
        status.alignment = TextAlignmentOptions.Right;
        CriarLabel(lobbyPanel.transform, "Note", MenuTextKey.LobbyHelp,
            new Vector2(0f, 380f), new Vector2(860f, 60f), 19, FontStyles.Normal, MutedColor, font);

        // Sem campos de nome/IP: a lista de salas (descoberta em LAN) é preenchida em runtime.
        var salasGrupo = new GameObject("RoomsList", typeof(RectTransform));
        var salasRt = salasGrupo.GetComponent<RectTransform>();
        salasRt.SetParent(lobbyPanel.transform, false);
        salasRt.anchorMin = salasRt.anchorMax = salasRt.pivot = new Vector2(0.5f, 0.5f);
        salasRt.anchoredPosition = new Vector2(0f, 140f);
        salasRt.sizeDelta = new Vector2(620f, 160f);
        Undo.RegisterCreatedObjectUndo(salasGrupo, "Create RoomsList");

        var lobbyCriar = CriarBotao(lobbyPanel.transform, "Create", MenuTextKey.LobbyCreate,
            new Vector2(-150f, 240f), font);
        var lobbyEntrar = CriarBotao(lobbyPanel.transform, "Join", MenuTextKey.LobbyJoin,
            new Vector2(150f, 240f), font);

        var netStatus = CriarTexto(lobbyPanel.transform, "NetStatus", string.Empty,
            new Vector2(0f, 92f), new Vector2(860f, 40f), 19, FontStyles.Normal, MutedColor, font);

        var playersLabel = CriarTexto(lobbyPanel.transform, "Players", string.Empty,
            new Vector2(0f, 8f), new Vector2(820f, 100f), 22, FontStyles.Normal, TextColor, font);
        playersLabel.alignment = TextAlignmentOptions.Left;

        var roomChoice = CriarGrupo(lobbyPanel.transform, "RoomChoice");
        CriarLabel(roomChoice.transform, "RoomTitle", MenuTextKey.LobbyRoomTitle,
            new Vector2(0f, -64f), new Vector2(760f, 36f), 20, FontStyles.Bold, MutedColor, font);
        var lobbyRoomOne = CriarBotao(roomChoice.transform, "RoomOne", MenuTextKey.LobbyRoomOne,
            new Vector2(-170f, -124f), font);
        var lobbyRoomTwo = CriarBotao(roomChoice.transform, "RoomTwo", MenuTextKey.LobbyRoomTwo,
            new Vector2(170f, -124f), font);

        var lobbyPronto = CriarBotao(lobbyPanel.transform, "Ready", MenuTextKey.LobbyReady,
            new Vector2(0f, -210f), font);
        var lobbyIniciar = CriarBotao(lobbyPanel.transform, "Start", MenuTextKey.LobbyStart,
            new Vector2(0f, -278f), font);
        var lobbyVoltar = CriarBotao(lobbyPanel.transform, "Back", MenuTextKey.Back,
            new Vector2(0f, -350f), font);
        DefinirCamadaUI(lobbyPanel);

        UnityEventTools.AddVoidPersistentListener(lobbyCriar.onClick, mode.CriarSala);
        UnityEventTools.AddVoidPersistentListener(lobbyEntrar.onClick, mode.Entrar);
        UnityEventTools.AddVoidPersistentListener(lobbyRoomOne.onClick, mode.EscolherSala1);
        UnityEventTools.AddVoidPersistentListener(lobbyRoomTwo.onClick, mode.EscolherSala2);
        UnityEventTools.AddVoidPersistentListener(lobbyPronto.onClick, mode.AlternarPronto);
        UnityEventTools.AddVoidPersistentListener(lobbyIniciar.onClick, mode.Iniciar);
        UnityEventTools.AddVoidPersistentListener(lobbyVoltar.onClick, mode.VoltarLobby);
        var lobbyCancel = Undo.AddComponent<MenuCancel>(lobbyPanel);
        lobbyCancel.backButton = lobbyVoltar;

        mode.modePanel = modePanel;
        mode.lobbyPanel = lobbyPanel;
        mode.createButton = lobbyCriar;
        mode.joinButton = lobbyEntrar;
        mode.startButton = lobbyIniciar;
        mode.readyButton = lobbyPronto;
        mode.nameField = null;
        mode.ipField = null;
        mode.roomsContainer = salasRt;
        mode.lobbyStatus = status;
        mode.playersLabel = playersLabel;
        mode.netStatus = netStatus;
        mode.roomChoice = roomChoice;
        mode.roomOneButton = lobbyRoomOne;
        mode.roomTwoButton = lobbyRoomTwo;
        mode.roomOneLabel = lobbyRoomOne.GetComponentInChildren<TMP_Text>();
        mode.roomTwoLabel = lobbyRoomTwo.GetComponentInChildren<TMP_Text>();
        mode.roomNormalColor = TextColor;
        mode.roomSelectedColor = AccentColor;
        mode.mainMenu = mainMenu;
        mode.modeFirstButton = modeLocal;
        EditorUtility.SetDirty(mode);

        Undo.RecordObject(mainMenu, "Link mode panel");
        mainMenu.modePanel = modePanel;
        mainMenu.modeFirstButton = modeLocal;
        EditorUtility.SetDirty(mainMenu);

        modePanel.SetActive(false);
        lobbyPanel.SetActive(false);
        lobbyIniciar.gameObject.SetActive(false);
        lobbyPronto.gameObject.SetActive(false);
        status.gameObject.SetActive(false);
        roomChoice.SetActive(false);
        lobbyCriar.gameObject.SetActive(true);

        SubstituirListener(playButton, mainMenu, mainMenu.OpenMode, "OpenBriefing", "OpenMode");

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(cena);
        Debug.Log("Menu de modo atualizado em StartMenu. Guarda a cena para manter as alteracoes.");
    }

    private static GameObject CriarPainel(Transform pai, string nome)
    {
        var painel = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        var rt = painel.GetComponent<RectTransform>();
        rt.SetParent(pai, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        painel.GetComponent<Image>().color = PanelColor;
        Undo.AddComponent<MenuPanelTransition>(painel);
        Undo.RegisterCreatedObjectUndo(painel, "Create " + nome);
        return painel;
    }

    private static GameObject CriarGrupo(Transform pai, string nome)
    {
        var grupo = new GameObject(nome, typeof(RectTransform));
        var rt = grupo.GetComponent<RectTransform>();
        rt.SetParent(pai, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Undo.RegisterCreatedObjectUndo(grupo, "Create " + nome);
        return grupo;
    }

    private static TextMeshProUGUI CriarLabel(Transform pai, string nome, MenuTextKey key, Vector2 posicao,
        Vector2 tamanho, int fonte, FontStyles estilo, Color cor, TMP_FontAsset font)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        Colocar(go.transform, pai, posicao, tamanho);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = MenuLocalization.GetText(key);
        tmp.fontSize = fonte;
        tmp.fontStyle = estilo;
        tmp.color = cor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        go.AddComponent<MenuLocalizedText>().SetKey(key);
        return tmp;
    }

    private static TextMeshProUGUI CriarTexto(Transform pai, string nome, string conteudo, Vector2 posicao,
        Vector2 tamanho, int fonte, FontStyles estilo, Color cor, TMP_FontAsset font)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        Colocar(go.transform, pai, posicao, tamanho);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = conteudo;
        tmp.fontSize = fonte;
        tmp.fontStyle = estilo;
        tmp.color = cor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>Campo de texto simples para o nome e o IP (o valor e preenchido em runtime).</summary>
    private static TMP_InputField CriarInput(Transform pai, string nome, string dica, Vector2 posicao,
        Vector2 tamanho, TMP_FontAsset font)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        Colocar(go.transform, pai, posicao, tamanho);
        var fundo = go.GetComponent<Image>();
        fundo.color = new Color(0.06f, 0.12f, 0.13f, 1f);

        var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var areaRt = area.GetComponent<RectTransform>();
        areaRt.SetParent(go.transform, false);
        areaRt.anchorMin = Vector2.zero;
        areaRt.anchorMax = Vector2.one;
        areaRt.offsetMin = new Vector2(14f, 6f);
        areaRt.offsetMax = new Vector2(-14f, -6f);

        var texto = CriarTextoInterno(area.transform, "Text", string.Empty, font, TextColor);
        var placeholder = CriarTextoInterno(area.transform, "Placeholder", dica, font, MutedColor);

        var campo = go.GetComponent<TMP_InputField>();
        campo.targetGraphic = fundo;
        campo.textViewport = areaRt;
        campo.textComponent = texto;
        campo.placeholder = placeholder;
        campo.lineType = TMP_InputField.LineType.SingleLine;
        campo.pointSize = 22;
        campo.caretColor = AccentColor;
        campo.selectionColor = new Color(0.3f, 0.48f, 0.39f, 0.6f);
        return campo;
    }

    private static TextMeshProUGUI CriarTextoInterno(Transform pai, string nome, string conteudo,
        TMP_FontAsset font, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(pai, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = conteudo;
        tmp.fontSize = 22;
        tmp.color = cor;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CriarBotao(Transform pai, string nome, MenuTextKey key, Vector2 posicao,
        TMP_FontAsset font)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        Colocar(go.transform, pai, posicao, new Vector2(270f, 60f));
        var imagem = go.GetComponent<Image>();
        imagem.color = Color.white;
        var botao = go.GetComponent<Button>();
        botao.targetGraphic = imagem;
        botao.colors = new ColorBlock
        {
            normalColor = new Color(0.08f, 0.17f, 0.18f, 1f),
            highlightedColor = new Color(0.3f, 0.48f, 0.39f, 1f),
            pressedColor = new Color(0.9f, 0.6f, 0.25f, 1f),
            selectedColor = new Color(0.65f, 0.4f, 0.16f, 1f),
            disabledColor = new Color(0.15f, 0.18f, 0.18f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };

        var texto = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = texto.GetComponent<RectTransform>();
        rt.SetParent(go.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = texto.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = MenuLocalization.GetText(key);
        tmp.fontSize = 22;
        tmp.color = TextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        texto.AddComponent<MenuLocalizedText>().SetKey(key);
        return botao;
    }

    private static Button EncontrarBotao(MainMenu menu, string metodo)
    {
        foreach (var botao in menu.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < botao.onClick.GetPersistentEventCount(); i++)
            {
                if (botao.onClick.GetPersistentTarget(i) == menu && botao.onClick.GetPersistentMethodName(i) == metodo)
                    return botao;
            }
        }
        return null;
    }

    private static void SubstituirListener(Button botao, MainMenu alvo, UnityAction novo,
        params string[] metodosAntigos)
    {
        Undo.RecordObject(botao, "Link mode menu");
        for (int i = botao.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (botao.onClick.GetPersistentTarget(i) != alvo) continue;
            string metodo = botao.onClick.GetPersistentMethodName(i);
            foreach (var antigo in metodosAntigos)
            {
                if (metodo == antigo)
                {
                    UnityEventTools.RemovePersistentListener(botao.onClick, i);
                    break;
                }
            }
        }
        UnityEventTools.AddVoidPersistentListener(botao.onClick, novo);
    }

    private static void RemoverGerados(Transform pai, string nome)
    {
        for (int i = pai.childCount - 1; i >= 0; i--)
        {
            if (pai.GetChild(i).name == nome)
                Undo.DestroyObjectImmediate(pai.GetChild(i).gameObject);
        }
    }

    private static void Colocar(Transform filho, Transform pai, Vector2 posicao, Vector2 tamanho)
    {
        filho.SetParent(pai, false);
        var rt = filho.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    private static void DefinirCamadaUI(GameObject root)
    {
        foreach (var filho in root.GetComponentsInChildren<Transform>(true))
            filho.gameObject.layer = UILayer;
    }
}
#endif
