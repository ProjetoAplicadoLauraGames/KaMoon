#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Constroi o menu de pausa na SampleScene, sem guardar a cena automaticamente.</summary>
public static class PauseMenuBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string FontPath = "Assets/Fonts/KaMoon/Rajdhani-Menu.asset";
    private const int UILayer = 5;

    private static readonly Color PanelColor = new Color(0.015f, 0.04f, 0.055f, 0.98f);
    private static readonly Color TextColor = new Color(0.87f, 0.94f, 0.91f);
    private static readonly Color MutedColor = new Color(0.62f, 0.72f, 0.7f);

    [MenuItem("Tools/KaMoon/Build Pause Menu")]
    public static void Build()
    {
        var cena = SceneManager.GetActiveScene();
        if (Application.isPlaying || cena.path != ScenePath)
        {
            Debug.LogError("Abre Assets/Scenes/SampleScene.unity e sai do Play Mode antes de construir o menu de pausa.");
            return;
        }

        Canvas canvas = null;
        foreach (var raiz in cena.GetRootGameObjects())
        {
            canvas = raiz.GetComponentInChildren<Canvas>(true);
            if (canvas != null) break;
        }

        if (canvas == null)
        {
            Debug.LogError("SampleScene precisa de um Canvas para o menu de pausa.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int grupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build KaMoon Pause Menu");

        RemoverGerados(canvas.transform, "PausePanel");
        RemoverGerados(canvas.transform, "SettingsPanel");

        if (canvas.sortingOrder < 101)
        {
            Undo.RecordObject(canvas, "Raise canvas above Hotbar");
            canvas.sortingOrder = 101;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) font = TMP_Settings.defaultFontAsset;

        var pause = canvas.GetComponent<PauseMenu>();
        if (pause == null) pause = Undo.AddComponent<PauseMenu>(canvas.gameObject);
        Undo.RecordObject(pause, "Link pause menu");

        // ---- Painel principal ----
        var painelPausa = CriarPainel(canvas.transform, "PausePanel");
        var conteudoPausa = CriarGrupo(painelPausa.transform, "Content");
        CriarLabel(conteudoPausa.transform, "Title", MenuTextKey.PauseTitle,
            new Vector2(0f, 230f), new Vector2(620f, 70f), 40, FontStyles.Bold, TextColor, font);
        pause.botaoRetomar = CriarBotao(conteudoPausa.transform, "ResumeButton", MenuTextKey.PauseResume,
            new Vector2(0f, 120f), font);
        pause.botaoSettings = CriarBotao(conteudoPausa.transform, "SettingsButton", MenuTextKey.PauseSettings,
            new Vector2(0f, 45f), font);
        pause.botaoSair = CriarBotao(conteudoPausa.transform, "QuitButton", MenuTextKey.Quit,
            new Vector2(0f, -30f), font);

        // ---- Painel de definições (irmão, para poder substituir o principal) ----
        var painelSettings = CriarPainel(canvas.transform, "SettingsPanel");
        var conteudoSettings = CriarGrupo(painelSettings.transform, "Content");
        CriarLabel(conteudoSettings.transform, "Title", MenuTextKey.PauseSettings,
            new Vector2(0f, 225f), new Vector2(620f, 50f), 34, FontStyles.Bold, TextColor, font);
        CriarLabel(conteudoSettings.transform, "ControlsTitle", MenuTextKey.PauseControls,
            new Vector2(0f, 180f), new Vector2(400f, 32f), 24, FontStyles.Bold, MutedColor, font);
        CriarTexto(conteudoSettings.transform, "ControlsList", MenuLocalization.GetText(MenuTextKey.PauseControlsList),
            new Vector2(0f, 95f), new Vector2(660f, 150f), 16, FontStyles.Normal, TextColor, font);

        CriarLabel(conteudoSettings.transform, "SensitivityLabel", MenuTextKey.PauseSensitivity,
            new Vector2(-210f, -5f), new Vector2(260f, 36f), 20, FontStyles.Normal, TextColor, font);
        pause.textoSensibilidade = CriarTexto(conteudoSettings.transform, "SensitivityValue", "0.80",
            new Vector2(110f, -5f), new Vector2(80f, 36f), 20, FontStyles.Normal, MutedColor, font);
        pause.sliderSensibilidade = CriarSlider(conteudoSettings.transform, "SensitivitySlider",
            new Vector2(-20f, -40f), new Vector2(500f, 26f), 0.1f, 2f, 0.8f);

        CriarLabel(conteudoSettings.transform, "SoundTitle", MenuTextKey.PauseSound,
            new Vector2(0f, -85f), new Vector2(400f, 36f), 24, FontStyles.Bold, MutedColor, font);
        CriarLabel(conteudoSettings.transform, "MasterVolumeLabel", MenuTextKey.MasterVolume,
            new Vector2(-210f, -120f), new Vector2(260f, 36f), 20, FontStyles.Normal, TextColor, font);
        pause.textoVolume = CriarTexto(conteudoSettings.transform, "VolumeValue", "100%",
            new Vector2(110f, -120f), new Vector2(80f, 36f), 20, FontStyles.Normal, MutedColor, font);
        pause.sliderVolume = CriarSlider(conteudoSettings.transform, "VolumeSlider",
            new Vector2(-20f, -155f), new Vector2(500f, 26f), 0f, 1f, 1f);
        pause.toggleSilenciar = CriarToggle(conteudoSettings.transform, "MuteToggle", MenuTextKey.MuteSound,
            new Vector2(0f, -190f), font);
        pause.botaoVoltar = CriarBotao(conteudoSettings.transform, "BackButton", MenuTextKey.Back,
            new Vector2(0f, -230f), font);

        painelPausa.SetActive(false);
        painelSettings.SetActive(false);

        pause.painelPausa = painelPausa;
        pause.painelSettings = painelSettings;

        DefinirCamadaUI(painelPausa);
        DefinirCamadaUI(painelSettings);

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(cena);
        Debug.Log("Menu de pausa atualizado em SampleScene. Guarda a cena para manter as alterações.");
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
        var tmp = CriarTexto(pai, nome, MenuLocalization.GetText(key), posicao, tamanho, fonte, estilo, cor, font);
        tmp.gameObject.AddComponent<MenuLocalizedText>().SetKey(key);
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

    private static Button CriarBotao(Transform pai, string nome, MenuTextKey key, Vector2 posicao, TMP_FontAsset font)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        Colocar(go.transform, pai, posicao, new Vector2(300f, 60f));
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

        var conteudo = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = conteudo.GetComponent<RectTransform>();
        rt.SetParent(go.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = conteudo.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = MenuLocalization.GetText(key);
        tmp.fontSize = 22;
        tmp.color = TextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        conteudo.AddComponent<MenuLocalizedText>().SetKey(key);
        return botao;
    }

    private static Slider CriarSlider(Transform pai, string nome, Vector2 posicao, Vector2 tamanho,
        float minimo, float maximo, float valor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Slider));
        Colocar(go.transform, pai, posicao, tamanho);
        go.GetComponent<Image>().color = new Color(0.06f, 0.12f, 0.13f, 1f);

        var area = new GameObject("Fill Area", typeof(RectTransform));
        var areaRt = area.GetComponent<RectTransform>();
        areaRt.SetParent(go.transform, false);
        areaRt.anchorMin = new Vector2(0f, 0.25f);
        areaRt.anchorMax = new Vector2(1f, 0.75f);
        areaRt.offsetMin = new Vector2(4f, 0f);
        areaRt.offsetMax = new Vector2(-4f, 0f);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.SetParent(areaRt, false);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.3f, 0.48f, 0.39f, 1f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.SetParent(go.transform, false);
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(8f, 0f);
        handleAreaRt.offsetMax = new Vector2(-8f, 0f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.SetParent(handleAreaRt, false);
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.sizeDelta = new Vector2(18f, 0f);
        handle.GetComponent<Image>().color = new Color(0.9f, 0.6f, 0.25f, 1f);

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = minimo;
        slider.maxValue = maximo;
        slider.SetValueWithoutNotify(valor);
        return slider;
    }

    private static Toggle CriarToggle(Transform pai, string nome, MenuTextKey key, Vector2 posicao, TMP_FontAsset font)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Toggle));
        Colocar(go.transform, pai, posicao, new Vector2(26f, 26f));
        var fundo = go.GetComponent<Image>();
        fundo.color = new Color(0.06f, 0.12f, 0.13f, 1f);

        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        var checkRt = check.GetComponent<RectTransform>();
        checkRt.SetParent(go.transform, false);
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.offsetMin = new Vector2(4f, 4f);
        checkRt.offsetMax = new Vector2(-4f, -4f);
        check.GetComponent<Image>().color = new Color(0.9f, 0.6f, 0.25f, 1f);

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = fundo;
        toggle.graphic = check.GetComponent<Image>();
        toggle.SetIsOnWithoutNotify(false);

        var rotulo = CriarLabel(pai, nome + "Label", key,
            new Vector2(posicao.x + 110f, posicao.y), new Vector2(200f, 36f),
            18, FontStyles.Normal, TextColor, font);
        rotulo.alignment = TextAlignmentOptions.Left;
        return toggle;
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
