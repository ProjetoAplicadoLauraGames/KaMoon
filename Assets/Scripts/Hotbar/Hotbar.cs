using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hotbar por jogador (task #21). Não precisa de setup na cena:
/// constrói a sua barra (UGUI) em runtime, metade esquerda (P1)
/// ou direita (P2) para funcionar em splitscreen.
/// P1: teclas 1-4 escolhem slot, G usa. P2: d-pad esq/dir muda,
/// R3 usa. Slot 0 é a lanterna por defeito.
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
    [SerializeField] private Key useKeyP1 = Key.G;

    [Header("Input P2 (comando)")]
    [SerializeField] private bool useGamepad = false;

    [Header("Lanterna (item \"flashlight\")")]
    [Tooltip("Spotlight própria. Se houver GestorLanternas na cena, ele tem prioridade.")]
    [SerializeField] private Light flashlight;

    [Header("Papel (item \"paper\")")]
    [SerializeField] private GameObject paperPrefab;
    [SerializeField] private Canvas canvas;


    [Header("Visual dos slots")]
    [SerializeField] private float slotSize = 64f;
    [SerializeField] private float slotSpacing = 8f;
    [SerializeField] private Color emptyColor = new Color(0.06f, 0.12f, 0.13f, 0.95f);
    [SerializeField] private Color itemColor = new Color(0.08f, 0.17f, 0.18f, 0.98f);
    [SerializeField] private Color selectedColor = new Color(0.65f, 0.4f, 0.16f, 0.98f);

    [Header("UI guardada na cena")]
    public Canvas hotbarCanvas;
    public RectTransform bar;
    public Image[] slotBackgrounds;
    public Text[] slotLabels;
    public Image[] slotIcons;
    public RectTransform crosshair;
    public Image crosshairOutline;
    public Image crosshairDot;
    public Sprite spriteVazio;
    public Sprite spriteItem;
    public Sprite spriteSelecionado;

    public int SelectedIndex { get; private set; }

    /// <summary>Número do jogador dono desta hotbar (1 = esquerda/teclado, 2 = direita/comando).</summary>
    public int PlayerNumber => playerNumber;
    private GestorLanternas lanternManager;
    private bool ativa = true;

    private void Awake()
    {
        // Em multiplayer so existe um jogador nesta sessao: esconde e desativa a hotbar alheia.
        ativa = !GameSession.multiplayer || playerNumber == GameSession.room;
        if (!ativa)
        {
            useKeyboard = false;
            useGamepad = false;
            if (hotbarCanvas != null) hotbarCanvas.gameObject.SetActive(false);
            return;
        }

        if (GameSession.multiplayer) useKeyboard = true;
        if (hotbarCanvas != null && !hotbarCanvas.gameObject.activeSelf) hotbarCanvas.gameObject.SetActive(true);
        if (useKeyP1 == Key.F) useKeyP1 = Key.G;
        NormalizeItems();
        lanternManager = FindFirstObjectByType<GestorLanternas>();

        if (!LigarUI())
        {
            Debug.LogError($"[Hotbar P{playerNumber}] Canvas da hotbar em falta na cena. Cria a hierarquia no Editor.", this);
            return;
        }

        AjustarAncoras();
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

    /// <summary>Usa a hierarquia guardada na cena; não cria nada em runtime.</summary>
    private bool LigarUI()
    {
        return hotbarCanvas != null && bar != null && slotBackgrounds != null &&
               slotIcons != null && crosshair != null &&
               crosshairOutline != null && crosshairDot != null &&
               slotBackgrounds.Length == slotCount && slotIcons.Length == slotCount;
    }

    private void AjustarAncoras()
    {
        float minX = 0f;
        float maxX = 1f;
        if (!GameSession.multiplayer)
        {
            if (playerNumber == 1) maxX = 0.5f;
            else minX = 0.5f;
        }

        bar.anchorMin = new Vector2(minX, 0f);
        bar.anchorMax = new Vector2(maxX, 0f);
        crosshair.anchorMin = new Vector2(minX, 0f);
        crosshair.anchorMax = new Vector2(maxX, 1f);

        Sprite circulo = CrosshairCircle();
        crosshairOutline.sprite = circulo;
        crosshairDot.sprite = circulo;
    }

    private void Update()
    {
        if (!ativa) return;
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

        if (kb.eKey.wasPressedThisFrame)
            TryPickup();
    }

    private void HandleGamepad()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null) return;

        if (pad.dpad.left.wasPressedThisFrame)
            SelectSlot((SelectedIndex - 1 + slotCount) % slotCount);
        if (pad.dpad.right.wasPressedThisFrame)
            SelectSlot((SelectedIndex + 1) % slotCount);
        if (pad.rightStickButton.wasPressedThisFrame)
            UseSelected();
        if (pad.buttonSouth.wasPressedThisFrame)
            TryPickup();
    }

    private void TryPickup()
    {
        Camera camera = GetComponentInChildren<Camera>();
        if (camera == null || !camera.enabled) return;

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            HotbarPickup pickup = hit.collider.GetComponentInParent<HotbarPickup>();
            if (pickup != null)
            {
                // Em rede o objeto tem de desaparecer em todos os clientes.
                string nomeItem = pickup.gameObject.name;
                if (pickup.TryPickup(this) && NetMatch.EmPartida)
                    NetMatch.PedirItem(nomeItem);
            }
            if (pickup != null || !hit.collider.isTrigger) break;
        }
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
        else if (item.itemId == "battery")
            UseBattery(item);
        else if (item.itemId == "codegiver")
            OpenPaper(item);
        else
            Debug.Log($"[Hotbar P{playerNumber}] Usar '{item.displayName}' (sem comportamento).");
    }

    /// <summary>Usar bateria: recarrega e consome o item (task #17).</summary>
    private void UseBattery(HotbarItem item)
    {
        // Em rede a carga e do servidor: a recarga tem de passar por la.
        if (NetMatch.EmPartida)
        {
            if (FindPlayerBattery() == null)
            {
                Debug.LogWarning($"[Hotbar P{playerNumber}] Sem componente Battery no player.");
                return;
            }
            NetMatch.PedirReabastecimento();
            RemoveItem(item.itemId);
            return;
        }

        Battery battery = FindPlayerBattery();
        if (battery == null)
        {
            Debug.LogWarning($"[Hotbar P{playerNumber}] Sem componente Battery no player.");
            return;
        }
        if (battery.Charge >= 0.999f)
        {
            Debug.Log($"[Hotbar P{playerNumber}] Bateria já cheia.");
            return;
        }
        battery.Recharge();
        RemoveItem(item.itemId);
    }

    private Battery FindPlayerBattery()
    {
        foreach (Battery battery in FindObjectsByType<Battery>(FindObjectsSortMode.None))
        {
            if (battery.PlayerNumber == playerNumber)
                return battery;
        }
        return null;
    }

    private void UseFlashlight(HotbarItem item)
    {
        // Em rede o pedido de lanterna vai para o servidor.
        if (NetMatch.EmPartida)
        {
            NetMatch.PedirLanterna();
            return;
        }

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

    private void OpenPaper(HotbarItem item)
    {
        Debug.Log($"[Hotbar P{playerNumber}] Abrir papel (item '{item.displayName}').");
        GameObject rawImageObj = Instantiate(paperPrefab);
        Canvas parente = canvas != null ? canvas : hotbarCanvas;
        if (parente != null) rawImageObj.transform.SetParent(parente.transform, false);
    }

    private static Sprite crosshairCircle;

    /// <summary>Círculo branco anti-aliased; sem sprite o Image desenhava um quadrado.</summary>
    private static Sprite CrosshairCircle()
    {
        if (crosshairCircle != null) return crosshairCircle;

        const int size = 32;
        float radius = (size - 1) * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius - d) * 255f));
            }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear
        };
        texture.SetPixels32(pixels);
        texture.Apply();

        crosshairCircle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        crosshairCircle.hideFlags = HideFlags.HideAndDontSave;
        return crosshairCircle;
    }

    private void RefreshUI()
    {
        if (slotBackgrounds == null) return;

        for (int i = 0; i < slotCount; i++)
        {
            HotbarItem item = items[i];
            if (spriteSelecionado != null)
            {
                slotBackgrounds[i].sprite = i == SelectedIndex ? spriteSelecionado : (item == null ? spriteVazio : spriteItem);
                slotBackgrounds[i].color = Color.white;
            }
            else
            {
                slotBackgrounds[i].color = i == SelectedIndex ? selectedColor : (item == null ? emptyColor : itemColor);
            }

            if (item != null && item.icon != null)
            {
                slotIcons[i].enabled = true;
                slotIcons[i].sprite = item.icon;
            }
            else
            {
                slotIcons[i].enabled = false;
            }
        }
    }
}
