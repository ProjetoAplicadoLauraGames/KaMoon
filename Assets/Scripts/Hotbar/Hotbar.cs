using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hotbar por jogador (task #21). Não precisa de setup na cena:
/// constrói a sua barra (UGUI) em runtime, metade esquerda (P1)
/// ou direita (P2) para funcionar em splitscreen.
/// P1: teclas 1-4 escolhem slot, F usa. P2: d-pad esq/dir muda,
/// Y (buttonNorth) usa. Slot 0 é a lanterna por defeito.
/// </summary>
public class Hotbar : MonoBehaviour
{
    [Header("Jogador")]
    [Tooltip("1 = metade esquerda (teclado). 2 = metade direita (comando).")]
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private int slotCount = 4;
    [Tooltip("Itens por slot. Null = vazio. O índice 0 deve ser a lanterna.")]
    [SerializeField] private HotbarItem[] items;

    [Header("Input P1 (teclado)")]
    [SerializeField] private bool useKeyboard = true;
    [SerializeField] private Key useKeyP1 = Key.F;

    [Header("Input P2 (comando)")]
    [SerializeField] private bool useGamepad = false;

    [Header("Lanterna (item \"flashlight\")")]
    [Tooltip("Spotlight própria. Se houver GestorLanternas na cena, ele tem prioridade.")]
    [SerializeField] private Light flashlight;

    [Header("Visual dos slots")]
    [SerializeField] private float slotSize = 64f;
    [SerializeField] private float slotSpacing = 8f;
    [SerializeField] private Color emptyColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color itemColor = new Color(0.15f, 0.15f, 0.2f, 0.75f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 0.95f);

    public int SelectedIndex { get; private set; }

    private Image[] slotBackgrounds;
    private Text[] slotLabels;
    private Image[] slotIcons;
    private GestorLanternas lanternManager;

    private void Awake()
    {
        NormalizeItems();
        lanternManager = FindFirstObjectByType<GestorLanternas>();
        BuildUI();
        RefreshUI();
    }

    private void NormalizeItems()
    {
        if (slotCount < 1) slotCount = 1;
        if (slotCount > 8) slotCount = 8;
        if (items == null || items.Length != slotCount)
        {
            HotbarItem[] resized = new HotbarItem[slotCount];
            if (items != null)
                for (int i = 0; i < Mathf.Min(items.Length, slotCount); i++)
                    resized[i] = items[i];
            items = resized;
        }
        SelectedIndex = Mathf.Clamp(SelectedIndex, 0, slotCount - 1);
    }

    private void Update()
    {
        if (useKeyboard)
            HandleKeyboard();
        if (useGamepad)
            HandleGamepad();
    }

    private void HandleKeyboard()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
                         Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
        for (int i = 0; i < slotCount; i++)
        {
            if (kb[digits[i]].wasPressedThisFrame)
                SelectSlot(i);
        }

        if (kb[useKeyP1].wasPressedThisFrame)
            UseSelected();
    }

    private void HandleGamepad()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        if (pad.dpad.left.wasPressedThisFrame)
            SelectSlot((SelectedIndex - 1 + slotCount) % slotCount);
        if (pad.dpad.right.wasPressedThisFrame)
            SelectSlot((SelectedIndex + 1) % slotCount);
        if (pad.buttonNorth.wasPressedThisFrame)
            UseSelected();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotCount || index == SelectedIndex) return;
        SelectedIndex = index;
        RefreshUI();
    }

    public void SetItem(int index, HotbarItem item)
    {
        if (index < 0 || index >= slotCount) return;
        items[index] = item;
        RefreshUI();
    }

    /// <summary>
    /// Apanhar item: ocupa o primeiro slot vazio. Devolve false se a hotbar
    /// estiver cheia ou o item já existir (sem duplicados no protótipo).
    /// </summary>
    public bool AddItem(HotbarItem item)
    {
        if (item == null || HasItem(item.itemId)) return false;
        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                RefreshUI();
                Debug.Log($"[Hotbar P{playerNumber}] Apanhou '{item.displayName}' (slot {i + 1}).");
                return true;
            }
        }
        Debug.Log($"[Hotbar P{playerNumber}] Hotbar cheia, não apanhou '{item.displayName}'.");
        return false;
    }

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] != null && items[i].itemId == itemId)
                return true;
        }
        return false;
    }

    public bool RemoveItem(string itemId)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] != null && items[i].itemId == itemId)
            {
                items[i] = null;
                RefreshUI();
                return true;
            }
        }
        return false;
    }

    public void UseSelected()
    {
        HotbarItem item = items[SelectedIndex];
        if (item == null)
        {
            Debug.Log($"[Hotbar P{playerNumber}] Slot {SelectedIndex + 1} vazio.");
            return;
        }

        if (item.itemId == "flashlight")
            UseFlashlight(item);
        else
            Debug.Log($"[Hotbar P{playerNumber}] Usar '{item.displayName}' (sem comportamento; ligar na task da bateria).");
    }

    private void UseFlashlight(HotbarItem item)
    {
        // Integração com o sistema existente (Samuel): a lanterna é exclusiva,
        // por isso delega no gestor quando ele existir na cena.
        if (lanternManager == null)
            lanternManager = FindFirstObjectByType<GestorLanternas>();

        if (lanternManager != null)
        {
            lanternManager.AtivarLanterna(playerNumber);
            return;
        }

        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
            Debug.Log($"[Hotbar P{playerNumber}] Lanterna {(flashlight.enabled ? "ON" : "OFF")}.");
        }
        else
        {
            Debug.LogWarning($"[Hotbar P{playerNumber}] Item '{item.displayName}' sem lanterna atribuída nem GestorLanternas na cena.");
        }
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject($"HotbarCanvasP{playerNumber}");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        GameObject barObj = new GameObject("Bar");
        barObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bar = barObj.AddComponent<RectTransform>();

        // Metade do ecrã correspondente ao jogador (splitscreen vertical).
        if (playerNumber == 1)
        {
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(0.5f, 0f);
        }
        else
        {
            bar.anchorMin = new Vector2(0.5f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
        }
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 16f);
        bar.sizeDelta = new Vector2(slotCount * (slotSize + slotSpacing), slotSize + 16f);

        HorizontalLayoutGroup layout = barObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        slotBackgrounds = new Image[slotCount];
        slotLabels = new Text[slotCount];
        slotIcons = new Image[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = new GameObject($"Slot{i + 1}");
            slot.transform.SetParent(bar, false);
            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);

            Image bg = slot.AddComponent<Image>();
            bg.color = emptyColor;
            slotBackgrounds[i] = bg;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(slot.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObj.AddComponent<Text>();
            label.font = font;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            slotLabels[i] = label;

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(6f, 6f);
            iconRect.offsetMax = new Vector2(-6f, -6f);

            Image icon = iconObj.AddComponent<Image>();
            icon.enabled = false;
            icon.preserveAspect = true;
            slotIcons[i] = icon;
        }
    }

    private void RefreshUI()
    {
        if (slotBackgrounds == null) return;

        for (int i = 0; i < slotCount; i++)
        {
            HotbarItem item = items[i];
            slotBackgrounds[i].color = i == SelectedIndex ? selectedColor : (item == null ? emptyColor : itemColor);

            if (item != null && item.icon != null)
            {
                slotIcons[i].enabled = true;
                slotIcons[i].sprite = item.icon;
                slotLabels[i].text = $"{i + 1}";
            }
            else if (item != null)
            {
                slotIcons[i].enabled = false;
                string initial = string.IsNullOrEmpty(item.displayName) ? "?" : item.displayName.Substring(0, 1).ToUpper();
                slotLabels[i].text = $"{i + 1}\n{initial}";
            }
            else
            {
                slotIcons[i].enabled = false;
                slotLabels[i].text = $"{i + 1}";
            }
        }
    }
}
