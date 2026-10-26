using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// RICO HUD
// Hides the old text on screen (gold, seeds, water, crops, date, time) and replaces it with:
//   I = Inventory bag  -> gold, water can, and each seed type
//   P = Phone          -> time, date, and crops
// Everything is built from plain Unity UI objects when the game starts.
public class RicoHUD : MonoBehaviour
{
    [Header("Controls")]
    public Key inventoryKey = Key.I;
    public Key phoneKey     = Key.P;

    [Header("Options")]
    public bool hideOldHUD = true;   // hide the owner's text HUD
    public bool showHints  = true;   // small "[I] Bag  [P] Phone" text in the corner

    [Header("Colors")]
    public Color panelColor  = new Color(0.10f, 0.10f, 0.13f, 0.88f);
    public Color phoneBody   = new Color(0.08f, 0.08f, 0.10f, 1f);
    public Color phoneScreen = new Color(0.17f, 0.24f, 0.36f, 1f);
    public Color accentColor = new Color(1f, 0.84f, 0.31f, 1f);

    private IInventory    _inventory;
    private ITimeProvider _time;

    private GameObject      _inventoryPanel;
    private TextMeshProUGUI _inventoryText;

    private RectTransform   _phone;
    private TextMeshProUGUI _phoneTime, _phoneDate, _phoneCrops;
    private Vector2         _phoneShown  = new Vector2(-40f, 40f);
    private Vector2         _phoneHidden = new Vector2(-40f, -600f);

    private bool _inventoryOpen;
    private bool _phoneOpen;

    void Start()
    {
        _inventory = ServiceLocator.Get<IInventory>();
        _time      = ServiceLocator.Get<ITimeProvider>();

        if (hideOldHUD) HideOldHUD();
        BuildUI();
        _inventoryPanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current[inventoryKey].wasPressedThisFrame)
            {
                _inventoryOpen = !_inventoryOpen;
                _inventoryPanel.SetActive(_inventoryOpen);
            }
            if (Keyboard.current[phoneKey].wasPressedThisFrame)
                _phoneOpen = !_phoneOpen;
        }

        // Slide the phone up / down
        Vector2 target = _phoneOpen ? _phoneShown : _phoneHidden;
        _phone.anchoredPosition = Vector2.Lerp(_phone.anchoredPosition, target,
                                               1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));

        if (_inventoryOpen) RefreshInventory();
        if (_phoneOpen || Vector2.Distance(_phone.anchoredPosition, _phoneHidden) > 1f) RefreshPhone();
    }

    // ── Old HUD ─────────────────────────────────────────────────────────────

    private void HideOldHUD()
    {
        PlayerHUD hud = FindFirstObjectByType<PlayerHUD>();
        if (hud == null) return;

        foreach (TextMeshProUGUI t in new[] { hud.goldText, hud.seedText, hud.waterText,
                                              hud.cropText, hud.dateText, hud.timeText })
        {
            if (t != null) t.gameObject.SetActive(false);
        }
    }

    // ── Refreshing the text ─────────────────────────────────────────────────

    private void RefreshInventory()
    {
        if (_inventory == null) { _inventoryText.text = "No inventory found."; return; }

        string accent = ColorUtility.ToHtmlStringRGB(accentColor);
        var sb = new StringBuilder();
        sb.AppendLine("<b><size=34>INVENTORY</size></b>");
        sb.AppendLine();
        sb.AppendLine($"Gold:  <b><color=#{accent}>{_inventory.GoldCount}</color></b>");
        sb.AppendLine($"Water Can:  <b>{(_inventory.HasWater ? "Full" : "Empty")}</b>");
        sb.AppendLine();
        sb.AppendLine("<b>Seeds</b>");

        bool anySeeds = false;
        foreach (SeedInventoryEntry entry in _inventory.SeedInventory)
        {
            if (entry == null || entry.seedData == null) continue;
            sb.AppendLine($"  • {entry.seedData.seedName}   <b>x{entry.amount}</b>");
            anySeeds = true;
        }
        if (!anySeeds) sb.AppendLine("  (none)");

        sb.AppendLine();
        sb.Append($"<size=18><alpha=#99>Press {inventoryKey} to close</size>");
        _inventoryText.text = sb.ToString();
    }

    private void RefreshPhone()
    {
        _phoneTime.text  = _time != null ? _time.GetFormattedTime() : "--:--";
        _phoneDate.text  = _time != null ? _time.GetFormattedDate() : "";
        _phoneCrops.text = _inventory != null ? $"Crops\n<size=44><b>{_inventory.CropCount}</b></size>" : "";
    }

    // ── Building the UI ─────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("RicoHUD Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        Transform root = canvasGO.transform;

        // Inventory panel (left side)
        RectTransform inv = Box(root, "Inventory", panelColor,
                                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(400f, 480f));
        _inventoryPanel = inv.gameObject;
        _inventoryText  = Text(inv, "Text", 24, TextAlignmentOptions.TopLeft, 28f);

        // Phone (bottom right, slides up)
        _phone = Box(root, "Phone", phoneBody,
                     new Vector2(1f, 0f), new Vector2(1f, 0f), _phoneHidden, new Vector2(270f, 500f));

        RectTransform screen = Box(_phone, "Screen", phoneScreen,
                                   Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = new Vector2(14f, 40f);
        screen.offsetMax = new Vector2(-14f, -40f);

        Box(_phone, "Speaker", new Color(0.25f, 0.25f, 0.28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -18f), new Vector2(70f, 8f));
        Box(_phone, "HomeButton", new Color(0.25f, 0.25f, 0.28f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(40f, 14f));

        _phoneTime = Text(screen, "Time", 64, TextAlignmentOptions.Top, 16f);
        _phoneTime.rectTransform.offsetMax = new Vector2(-16f, -40f);
        _phoneTime.fontStyle = FontStyles.Bold;

        _phoneDate = Text(screen, "Date", 22, TextAlignmentOptions.Top, 16f);
        _phoneDate.rectTransform.offsetMax = new Vector2(-16f, -20f);

        _phoneCrops = Text(screen, "Crops", 26, TextAlignmentOptions.Bottom, 16f);
        _phoneCrops.rectTransform.offsetMin = new Vector2(16f, 40f);
        _phoneCrops.color = accentColor;

        // Hint text (bottom left)
        if (showHints)
        {
            TextMeshProUGUI hint = Text(root, "Hints", 18, TextAlignmentOptions.BottomLeft, 0f);
            hint.rectTransform.offsetMin = new Vector2(24f, 18f);
            hint.text  = $"[{inventoryKey}] Bag    [{phoneKey}] Phone";
            hint.color = new Color(1f, 1f, 1f, 0.6f);
        }
    }

    // A colored rectangle
    private static RectTransform Box(Transform parent, string name, Color color,
                                     Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta        = size;
        return rt;
    }

    // A text label that fills its parent (with padding)
    private static TextMeshProUGUI Text(Transform parent, string name, float size,
                                        TextAlignmentOptions align, float padding)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize      = size;
        t.alignment     = align;
        t.color         = Color.white;
        t.raycastTarget = false;
        return t;
    }
}