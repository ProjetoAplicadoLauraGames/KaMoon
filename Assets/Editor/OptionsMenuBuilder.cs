#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Constroi o painel de Opcoes e o botao Options no StartMenu.
/// Menu: Tools > KaMoon > Build Options Menu
/// Pode ser corrido de novo para reconstruir o painel (apaga o anterior).
/// </summary>
public static class OptionsMenuBuilder
{
    private const int UILayer = 5;

    [MenuItem("Tools/KaMoon/Build Options Menu")]
    public static void Build()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("OptionsMenuBuilder: cena sem Canvas. Abre a cena StartMenu.");
            return;
        }

        var mainMenuGO = GameObject.Find("MainMenu");
        if (mainMenuGO == null)
        {
            Debug.LogError("OptionsMenuBuilder: GameObject 'MainMenu' nao encontrado na cena.");
            return;
        }

        var mainMenu = mainMenuGO.GetComponent<MainMenu>();
        if (mainMenu == null)
        {
            Debug.LogError("OptionsMenuBuilder: 'MainMenu' nao tem o componente MainMenu.");
            return;
        }

        var res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };

        Undo.SetCurrentGroupName("Build Options Menu");
        int grupo = Undo.GetCurrentGroup();

        // ---- Painel de opcoes -------------------------------------------------
        var painelAntigo = canvas.transform.Find("OptionsMenuPanel");
        if (painelAntigo != null)
            Undo.DestroyObjectImmediate(painelAntigo.gameObject);

        var rootGO = new GameObject("OptionsMenuPanel", typeof(RectTransform));
        var rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.SetParent(canvas.transform, false);
        rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = new Vector2(500f, 540f);

        var bg = rootGO.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        bg.raycastTarget = true;

        CriarLabel(rootRT, "Title", "OPCOES", new Vector2(0f, -30f), new Vector2(240f, 40f), 30, FontStyle.Bold);

        // Volume
        CriarLabel(rootRT, "VolumeLabel", "Volume", new Vector2(-100f, -80f), new Vector2(120f, 24f), 20, FontStyle.Normal);
        var volumeGO = DefaultControls.CreateSlider(res);
        volumeGO.name = "VolumeSlider";
        Colocar(volumeGO.transform, rootRT, new Rect(new Vector2(60f, -80f), new Vector2(200f, 18f)));

        // Fullscreen
        CriarLabel(rootRT, "FullscreenLabel", "Fullscreen", new Vector2(-100f, -125f), new Vector2(120f, 24f), 20, FontStyle.Normal);
        var fullscreenGO = DefaultControls.CreateToggle(res);
        fullscreenGO.name = "FullscreenToggle";
        Colocar(fullscreenGO.transform, rootRT, new Rect(new Vector2(60f, -125f), new Vector2(22f, 22f)));
        var labelToggle = fullscreenGO.transform.Find("Label");
        if (labelToggle != null) labelToggle.gameObject.SetActive(false);

        // Resolucao
        CriarLabel(rootRT, "ResolutionLabel", "Resolucao", new Vector2(-100f, -170f), new Vector2(120f, 24f), 20, FontStyle.Normal);
        var resolucaoGO = DefaultControls.CreateDropdown(res);
        resolucaoGO.name = "ResolutionDropdown";
        Colocar(resolucaoGO.transform, rootRT, new Rect(new Vector2(60f, -170f), new Vector2(200f, 30f)));

        // Qualidade
        CriarLabel(rootRT, "QualityLabel", "Qualidade", new Vector2(-100f, -213f), new Vector2(120f, 24f), 20, FontStyle.Normal);
        var qualidadeGO = DefaultControls.CreateDropdown(res);
        qualidadeGO.name = "QualityDropdown";
        Colocar(qualidadeGO.transform, rootRT, new Rect(new Vector2(60f, -213f), new Vector2(200f, 28f)));

        // Back
        var backGO = CriarBotaoEstiloMain(rootRT, "BackButton", "Back", new Vector2(0f, -252f));

        // Componente OptionsMenu + ligacoes
        var options = rootGO.AddComponent<OptionsMenu>();
        var so = new SerializedObject(options);
        so.FindProperty("volumeSlider").objectReferenceValue = volumeGO.GetComponent<Slider>();
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenGO.GetComponent<Toggle>();
        so.FindProperty("resolutionDropdown").objectReferenceValue = resolucaoGO.GetComponent<Dropdown>();
        so.FindProperty("qualityDropdown").objectReferenceValue = qualidadeGO.GetComponent<Dropdown>();
        so.FindProperty("backButton").objectReferenceValue = backGO.GetComponent<Button>();
        so.FindProperty("menuPrincipal").objectReferenceValue = mainMenuGO;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(options);

        // ---- Botao Options no menu principal ----------------------------------
        var optionsButtonGO = CriarBotaoEstiloMain(mainMenuGO.transform, "OptionsButton", "Options", new Vector2(49f, 67f));
        var optionsRT = optionsButtonGO.GetComponent<RectTransform>();
        optionsRT.localScale = new Vector3(4f, 4f, 4f);

        var botaoOptions = optionsButtonGO.GetComponent<Button>();
        var nav = botaoOptions.navigation;
        nav.mode = Navigation.Mode.Automatic;
        botaoOptions.navigation = nav;
        UnityEventTools.AddVoidPersistentListener(botaoOptions.onClick, mainMenu.OpenOptions);

        // MainMenu -> referenceia o painel
        var soMenu = new SerializedObject(mainMenu);
        soMenu.FindProperty("optionsMenuPanel").objectReferenceValue = rootGO;
        soMenu.ApplyModifiedProperties();

        rootGO.SetActive(false);
        Undo.RegisterCreatedObjectUndo(rootGO, "Build Options Menu");
        Undo.RegisterCreatedObjectUndo(optionsButtonGO, "Build Options Button");
        Undo.CollapseUndoOperations(grupo);

        EditorSceneManager_MarkSceneDirty();
        Debug.Log("OptionsMenuBuilder: painel de opcoes criado. Painel guarda-se ativo=false; usa o botao Options.");
    }

    private static void Colocar(Transform filho, Transform pai, Rect rect)
    {
        filho.SetParent(pai, false);
        var rt = filho.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = rect.position;
        rt.sizeDelta = rect.size;
    }

    private static void CriarLabel(Transform pai, string nome, string texto, Vector2 pos, Vector2 size, int fontSize, FontStyle style)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        Colocar(go.transform, pai, new Rect(pos, size));

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    /// <summary>Recria o estilo dos botoes do MainMenu: sprite branco + texto TMP escuro.</summary>
    private static GameObject CriarBotaoEstiloMain(Transform pai, string nome, string texto, Vector2 pos)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var imagem = go.GetComponent<Image>();
        imagem.sprite = sprite;
        imagem.type = Image.Type.Sliced;
        imagem.color = Color.white;

        var botao = go.GetComponent<Button>();
        botao.targetGraphic = imagem;
        botao.transition = Selectable.Transition.ColorTint;

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160f, 23.5f);

        var textoGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer));
        var textoRT = textoGO.GetComponent<RectTransform>();
        textoRT.SetParent(go.transform, false);
        textoRT.anchorMin = Vector2.zero;
        textoRT.anchorMax = Vector2.one;
        textoRT.offsetMin = Vector2.zero;
        textoRT.offsetMax = Vector2.zero;

        var tmp = textoGO.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = texto;
        tmp.fontSize = 24;
        tmp.color = new Color(0.196f, 0.196f, 0.196f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go;
    }

    private static void EditorSceneManager_MarkSceneDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
#endif
