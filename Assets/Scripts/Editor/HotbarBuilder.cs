#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cria o Canvas da hotbar na cena e liga as referências do componente Hotbar.
/// Substitui apenas os objetos gerados (HotbarCanvasP1/P2), nunca UI escrita à mão.
/// </summary>
public static class HotbarBuilder
{
    private const string UILayer = "UI";

    [MenuItem("Tools/KaMoon/Build Hotbar Canvas")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Sai do Play Mode antes de construir o Canvas da hotbar.");
            return;
        }

        var cena = SceneManager.GetActiveScene();
        Hotbar[] hots = Object.FindObjectsByType<Hotbar>(FindObjectsInactive.Include);
        if (hots.Length == 0)
        {
            Debug.LogError("A cena ativa não tem componentes Hotbar.");
            return;
        }

        HotbarArt.CriarEAssociar();

        Undo.IncrementCurrentGroup();
        int grupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build KaMoon Hotbar Canvas");

        foreach (Hotbar hotbar in hots)
            Construir(hotbar);

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(cena);
        Debug.Log($"Canvas da hotbar criado para {hots.Length} jogador(es) em {cena.name}. Guarda a cena.");
    }

    private static void Construir(Hotbar hotbar)
    {
        string nome = $"HotbarCanvasP{hotbar.PlayerNumber}";
        foreach (GameObject antigo in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (antigo == null || antigo.transform.parent != null) continue;
            if (antigo.name == nome) Undo.DestroyObjectImmediate(antigo);
        }

        Undo.RecordObject(hotbar, "Ligar hotbar canvas");
        hotbar.AplicarMedidas();

        var canvasGo = new GameObject(nome, typeof(RectTransform), typeof(Canvas));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create " + nome);
        canvasGo.layer = LayerMask.NameToLayer(UILayer);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        float minX = hotbar.PlayerNumber == 2 ? 0.5f : 0f;
        float maxX = hotbar.PlayerNumber == 2 ? 1f : 0.5f;
        // Fundo estrelado do menu, muito ténue, para ligar a hotbar ao resto do jogo.
        Sprite estrelasSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Materials/menu.png");
        if (estrelasSprite != null)
        {
            RectTransform estrelas = CriarRect(canvasGo.transform, "Starfield",
                new Vector2(minX, 0f), new Vector2(maxX, 0f), new Vector2(0.5f, 0f));
            estrelas.anchoredPosition = new Vector2(0f, 10f);
            estrelas.sizeDelta = new Vector2(hotbar.SlotCount * (hotbar.SlotSize + hotbar.SlotSpacing) + 20f, hotbar.SlotSize + 24f);
            Image estrelasImagem = estrelas.gameObject.AddComponent<Image>();
            estrelasImagem.sprite = estrelasSprite;
            estrelasImagem.type = Image.Type.Simple;
            estrelasImagem.color = new Color(1f, 1f, 1f, 0.1f);
            estrelasImagem.raycastTarget = false;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Font rajdhani = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/KaMoon/Rajdhani-Regular.ttf");
        if (rajdhani != null) font = rajdhani;

        RectTransform bar = CriarRect(canvasGo.transform, "Bar",
            new Vector2(minX, 0f), new Vector2(maxX, 0f), new Vector2(0.5f, 0f));
        bar.anchoredPosition = new Vector2(0f, 16f);
        bar.sizeDelta = new Vector2(hotbar.SlotCount * (hotbar.SlotSize + hotbar.SlotSpacing), hotbar.SlotSize + 16f);

        HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = hotbar.SlotSpacing;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fundos = new Image[hotbar.SlotCount];
        var rotulos = new Text[0];
        var icones = new Image[hotbar.SlotCount];

        for (int i = 0; i < hotbar.SlotCount; i++)
        {
            RectTransform slot = CriarRect(bar, $"Slot{i + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            slot.sizeDelta = new Vector2(hotbar.SlotSize, hotbar.SlotSize);

            fundos[i] = slot.gameObject.AddComponent<Image>();
            fundos[i].sprite = HotbarArt.SlotVazio();
            fundos[i].color = Color.white;

            RectTransform icone = CriarRect(slot, "Icon", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            icone.offsetMin = new Vector2(4f, 4f);
            icone.offsetMax = new Vector2(-4f, -4f);
            icones[i] = icone.gameObject.AddComponent<Image>();
            icones[i].enabled = false;
            icones[i].preserveAspect = true;
            icones[i].raycastTarget = false;
        }

        RectTransform mira = CriarRect(canvasGo.transform, "Crosshair",
            new Vector2(minX, 0f), new Vector2(maxX, 1f), new Vector2(0.5f, 0.5f));
        mira.offsetMin = mira.offsetMax = Vector2.zero;

        Image contorno = CriarPonto(mira, "Outline", 8f, new Color(0f, 0f, 0f, 0.55f));
        Image ponto = CriarPonto(mira, "Dot", 4f, new Color(1f, 1f, 1f, 0.9f));

        Undo.RecordObject(hotbar, "Ligar hotbar canvas");
        hotbar.AplicarPaleta();
        hotbar.hotbarCanvas = canvas;
        hotbar.bar = bar;
        hotbar.slotBackgrounds = fundos;
        hotbar.slotLabels = rotulos;
        hotbar.slotIcons = icones;
        hotbar.crosshair = mira;
        hotbar.crosshairOutline = contorno;
        hotbar.crosshairDot = ponto;
        hotbar.spriteVazio = HotbarArt.SlotVazio();
        hotbar.spriteItem = HotbarArt.SlotItem();
        hotbar.spriteSelecionado = HotbarArt.SlotSelecionado();
    }

    private static RectTransform CriarRect(Transform pai, string nome, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        go.layer = LayerMask.NameToLayer(UILayer);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static Image CriarPonto(Transform pai, string nome, float tamanho, Color cor)
    {
        RectTransform rt = CriarRect(pai, nome, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(tamanho, tamanho);
        Image imagem = rt.gameObject.AddComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem;
    }
}
#endif
