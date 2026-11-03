#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Constroi os menus apenas na cena StartMenu, sem guardar a cena automaticamente.</summary>
public static class OptionsMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/StartMenu.unity";
    private const int UILayer = 5;

    [MenuItem("Tools/KaMoon/Build Options Menu")]
    public static void Build()
    {
        var cena = SceneManager.GetActiveScene();
        if (Application.isPlaying || cena.path != ScenePath)
        {
            Debug.LogError("Abre Assets/Scenes/StartMenu.unity e sai do Play Mode antes de construir o menu.");
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

        var playButton = EncontrarBotao(mainMenu, "ChangeScene");
        var quitButton = EncontrarBotao(mainMenu, "QuitApplication");
        if (playButton == null || quitButton == null)
        {
            Debug.LogError("Nao foram encontrados os botoes ligados a ChangeScene e QuitApplication.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int grupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build KaMoon Menu");
        Undo.RecordObject(mainMenu, "Link options panel");

        // Remove apenas os elementos gerados anteriormente, evitando botoes duplicados.
        RemoverGerados(canvas.transform, "OptionsMenuPanel");
        RemoverGerados(mainMenu.transform, "OptionsButton");

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Scale menu");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        var recursos = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };

        var painel = new GameObject("OptionsMenuPanel", typeof(RectTransform), typeof(Image));
        painel.SetActive(false);
        var painelRT = painel.GetComponent<RectTransform>();
        painelRT.SetParent(canvas.transform, false);
        painelRT.anchorMin = Vector2.zero;
        painelRT.anchorMax = Vector2.one;
        painelRT.offsetMin = painelRT.offsetMax = Vector2.zero;
        painel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var conteudo = new GameObject("Content", typeof(RectTransform), typeof(Image));
        Colocar(conteudo.transform, painel.transform, Vector2.zero, new Vector2(760f, 640f));
        conteudo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        var pai = conteudo.transform;

        CriarLabel(pai, "Title", MenuTextKey.Options, new Vector2(0f, 260f), new Vector2(600f, 54f), 36, FontStyles.Bold);

        CriarLabelLinha(pai, "LanguageLabel", MenuTextKey.Language, 168f);
        var idioma = CriarDropdown(recursos, pai, "LanguageDropdown", 168f);
        idioma.AddOptions(new List<string>(MenuLocalization.LanguageNames));
        idioma.SetValueWithoutNotify(MenuLocalization.CurrentLanguage);

        CriarLabelLinha(pai, "VolumeLabel", MenuTextKey.MasterVolume, 108f);
        var volumeGO = DefaultControls.CreateSlider(recursos);
        volumeGO.name = "VolumeSlider";
        Colocar(volumeGO.transform, pai, new Vector2(110f, 108f), new Vector2(240f, 28f));
        var volume = volumeGO.GetComponent<Slider>();
        volume.minValue = 0f;
        volume.maxValue = 1f;
        volume.value = 1f;
        var percentagem = CriarTexto(pai, "VolumeValue", "100%", new Vector2(300f, 108f), new Vector2(80f, 32f), 22);

        CriarLabelLinha(pai, "MuteLabel", MenuTextKey.MuteSound, 48f);
        var silenciar = CriarToggle(recursos, pai, "MuteToggle", 48f);
        silenciar.isOn = false;

        CriarLabelLinha(pai, "FullscreenLabel", MenuTextKey.Fullscreen, -12f);
        var ecraInteiro = CriarToggle(recursos, pai, "FullscreenToggle", -12f);
        ecraInteiro.isOn = Screen.fullScreen;

        CriarLabelLinha(pai, "ResolutionLabel", MenuTextKey.Resolution, -72f);
        var resolucao = CriarDropdown(recursos, pai, "ResolutionDropdown", -72f);

        CriarLabelLinha(pai, "QualityLabel", MenuTextKey.Quality, -132f);
        var qualidade = CriarDropdown(recursos, pai, "QualityDropdown", -132f);
        qualidade.AddOptions(new List<string>(QualitySettings.names));

        var voltar = CriarBotao(pai, "BackButton", MenuTextKey.Back, new Vector2(0f, -250f), recursos.standard);
        voltar.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 52f);

        var options = painel.AddComponent<OptionsMenu>();
        options.languageDropdown = idioma;
        options.volumeSlider = volume;
        options.volumeValueText = percentagem;
        options.muteToggle = silenciar;
        options.fullscreenToggle = ecraInteiro;
        options.resolutionDropdown = resolucao;
        options.qualityDropdown = qualidade;
        options.backButton = voltar;
        options.menuPrincipal = mainMenu.gameObject;
        ConfigurarNavegacao(idioma, volume, silenciar, ecraInteiro, resolucao, qualidade, voltar);

        var optionsButton = CriarBotao(mainMenu.transform, "OptionsButton", MenuTextKey.Options, Vector2.zero, recursos.standard);
        UnityEventTools.AddVoidPersistentListener(optionsButton.onClick, mainMenu.OpenOptions);
        ConfigurarBotaoExistente(playButton, MenuTextKey.Play, new Vector2(0f, 96f));
        ConfigurarBotaoExistente(quitButton, MenuTextKey.Quit, new Vector2(0f, -96f));
        ConfigurarNavegacao(playButton, optionsButton, quitButton);

        mainMenu.optionsMenuPanel = painel;
        mainMenu.firstSelectedButton = playButton;
        EditorUtility.SetDirty(mainMenu);
        Undo.RecordObject(mainMenu.gameObject, "Show main menu");
        mainMenu.gameObject.SetActive(true);

        foreach (var root in cena.GetRootGameObjects())
        {
            var eventSystem = root.GetComponentInChildren<EventSystem>(true);
            if (eventSystem == null) continue;
            Undo.RecordObject(eventSystem, "Select play button");
            eventSystem.firstSelectedGameObject = playButton.gameObject;
        }

        DefinirCamadaUI(painel);
        DefinirCamadaUI(optionsButton.gameObject);
        Undo.RegisterCreatedObjectUndo(painel, "Create options panel");
        Undo.RegisterCreatedObjectUndo(optionsButton.gameObject, "Create options button");
        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(cena);
        Debug.Log("Menu atualizado em StartMenu: idioma, som e ecra. Guarda a cena para manter as alteracoes.");
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

    private static TextMeshProUGUI CriarTexto(Transform pai, string nome, string texto, Vector2 posicao, Vector2 tamanho, int tamanhoFonte)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        Colocar(go.transform, pai, posicao, tamanho);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = texto;
        tmp.fontSize = tamanhoFonte;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void CriarLabel(Transform pai, string nome, MenuTextKey key, Vector2 posicao, Vector2 tamanho, int tamanhoFonte, FontStyles estilo = FontStyles.Normal)
    {
        var tmp = CriarTexto(pai, nome, MenuLocalization.GetText(key), posicao, tamanho, tamanhoFonte);
        tmp.fontStyle = estilo;
        tmp.gameObject.AddComponent<MenuLocalizedText>().SetKey(key);
    }

    private static void CriarLabelLinha(Transform pai, string nome, MenuTextKey key, float y)
    {
        CriarLabel(pai, nome, key, new Vector2(-180f, y), new Vector2(260f, 36f), 24);
    }

    private static Dropdown CriarDropdown(DefaultControls.Resources recursos, Transform pai, string nome, float y)
    {
        var go = DefaultControls.CreateDropdown(recursos);
        go.name = nome;
        Colocar(go.transform, pai, new Vector2(140f, y), new Vector2(280f, 40f));
        foreach (var texto in go.GetComponentsInChildren<Text>(true))
        {
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 20;
            texto.raycastTarget = false;
        }
        var dropdown = go.GetComponent<Dropdown>();
        dropdown.captionText.rectTransform.offsetMin = new Vector2(10f, 2f);
        dropdown.captionText.rectTransform.offsetMax = new Vector2(-25f, -2f);
        var item = dropdown.itemText.transform.parent.GetComponent<RectTransform>();
        item.sizeDelta = new Vector2(0f, 36f);
        item.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
        dropdown.ClearOptions();
        return dropdown;
    }

    private static Toggle CriarToggle(DefaultControls.Resources recursos, Transform pai, string nome, float y)
    {
        var go = DefaultControls.CreateToggle(recursos);
        go.name = nome;
        Colocar(go.transform, pai, new Vector2(140f, y), new Vector2(280f, 28f));
        go.transform.Find("Label").gameObject.SetActive(false);
        return go.GetComponent<Toggle>();
    }

    private static Button CriarBotao(Transform pai, string nome, MenuTextKey key, Vector2 posicao, Sprite sprite)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        Colocar(go.transform, pai, posicao, new Vector2(360f, 64f));
        var imagem = go.GetComponent<Image>();
        imagem.sprite = sprite;
        imagem.type = Image.Type.Sliced;
        var botao = go.GetComponent<Button>();
        botao.targetGraphic = imagem;
        var texto = CriarTexto(go.transform, "Text (TMP)", MenuLocalization.GetText(key), Vector2.zero, Vector2.zero, 28);
        var rt = texto.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        texto.color = new Color(0.196f, 0.196f, 0.196f, 1f);
        texto.gameObject.AddComponent<MenuLocalizedText>().SetKey(key);
        return botao;
    }

    private static void ConfigurarBotaoExistente(Button botao, MenuTextKey key, Vector2 posicao)
    {
        Undo.RecordObject(botao.GetComponent<RectTransform>(), "Layout main menu");
        Colocar(botao.transform, botao.transform.parent, posicao, new Vector2(360f, 64f));
        var texto = botao.GetComponentInChildren<TextMeshProUGUI>(true);
        if (texto == null) return;
        Undo.RecordObject(texto, "Translate main menu");
        texto.fontSize = 28;
        texto.raycastTarget = false;
        var localizacao = texto.GetComponent<MenuLocalizedText>();
        if (localizacao == null) localizacao = Undo.AddComponent<MenuLocalizedText>(texto.gameObject);
        Undo.RecordObject(localizacao, "Set translation key");
        localizacao.SetKey(key);
        EditorUtility.SetDirty(localizacao);
    }

    private static void ConfigurarNavegacao(params Selectable[] controlos)
    {
        for (int i = 0; i < controlos.Length; i++)
        {
            Undo.RecordObject(controlos[i], "Set menu navigation");
            controlos[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = controlos[(i + controlos.Length - 1) % controlos.Length],
                selectOnDown = controlos[(i + 1) % controlos.Length]
            };
        }
    }

    private static void DefinirCamadaUI(GameObject root)
    {
        foreach (var filho in root.GetComponentsInChildren<Transform>(true))
            filho.gameObject.layer = UILayer;
    }
}
#endif
