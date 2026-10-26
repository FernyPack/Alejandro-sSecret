using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// PHONE UI (put on any object in the scene, e.g. an empty "InventoryPhoneUI")
// Hides the old text on screen and replaces it with a phone that has apps:
//   Home screen -> time, date, and app icons
//   Bag app     -> gold, water can, and each seed type
//   Farm app    -> crops and total seeds
// While the phone is open the player stops and the mouse appears, so you can tap the apps.
//
// CONTROLS
//   P         = open / close phone
//   I         = open phone straight to the Bag app
//   1 / 2     = open Bag / Farm app
//   Backspace = back to home screen
//   Esc       = close phone
public class InventoryPhoneUI : MonoBehaviour
{
    [Header("Controls")]
    public Key phoneKey    = Key.P;
    public Key bagShortcut = Key.I;

    [Header("Options")]
    public bool hideOldHUD = true;   // hide the owner's text HUD
    public bool showHints  = true;   // small "[P] Phone  [I] Bag" text in the corner

    [Header("Colors")]
    public Color phoneBody   = new Color(0.08f, 0.08f, 0.10f, 1f);
    public Color phoneScreen = new Color(0.17f, 0.24f, 0.36f, 1f);
    public Color bagAppColor = new Color(0.85f, 0.55f, 0.25f, 1f);
    public Color farmAppColor = new Color(0.35f, 0.70f, 0.35f, 1f);
    public Color buttonColor = new Color(1f, 1f, 1f, 0.15f);
    public Color accentColor = new Color(1f, 0.84f, 0.31f, 1f);

    private enum PhoneApp { Home, Bag, Farm }

    private IInventory    _inventory;
    private ITimeProvider _time;

    private RectTransform _phone;
    private Vector2       _phoneShown  = new Vector2(-40f, 40f);
    private Vector2       _phoneHidden = new Vector2(-40f, -650f);

    private GameObject      _homeScreen, _bagScreen, _farmScreen;
    private TextMeshProUGUI _homeTime, _homeDate, _bagText, _farmText, _statusBar;

    private bool     _open;
    private PhoneApp _currentApp = PhoneApp.Home;

    void Start()
    {
        _inventory = ServiceLocator.Get<IInventory>();
        _time      = ServiceLocator.Get<ITimeProvider>();

        if (hideOldHUD) HideOldHUD();
        MakeSureEventSystemExists();
        BuildUI();
        ShowApp(PhoneApp.Home);
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb[phoneKey].wasPressedThisFrame)
            {
                if (_open) ClosePhone(); else OpenPhone(PhoneApp.Home);
            }
            else if (kb[bagShortcut].wasPressedThisFrame)
            {
                if (_open && _currentApp == PhoneApp.Bag) ClosePhone(); else OpenPhone(PhoneApp.Bag);
            }
            else if (_open)
            {
                if (kb.digit1Key.wasPressedThisFrame)    ShowApp(PhoneApp.Bag);
                if (kb.digit2Key.wasPressedThisFrame)    ShowApp(PhoneApp.Farm);
                if (kb.backspaceKey.wasPressedThisFrame) ShowApp(PhoneApp.Home);
                if (kb.escapeKey.wasPressedThisFrame)    ClosePhone();
            }
        }

        // Something else closed it (e.g. Esc) -> put the phone away
        if (_open && !PlayerStateManager.IsState(PlayerState.Shop))
            _open = false;

        // Slide the phone up / down
        Vector2 target = _open ? _phoneShown : _phoneHidden;
        _phone.anchoredPosition = Vector2.Lerp(_phone.anchoredPosition, target,
                                               1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));

        if (_open || Vector2.Distance(_phone.anchoredPosition, _phoneHidden) > 1f)
            Refresh();
    }

    // ── Opening / closing ───────────────────────────────────────────────────

    private void OpenPhone(PhoneApp app)
    {
        if (!_open)
        {
            if (!PlayerStateManager.IsFree) return;           // not during dialogue, shop, etc.
            PlayerStateManager.SetState(PlayerState.Shop);    // stops walking + shows the mouse
            _open = true;
        }
        ShowApp(app);
    }

    private void ClosePhone()
    {
        if (!_open) return;
        _open = false;
        if (PlayerStateManager.IsState(PlayerState.Shop))
            PlayerStateManager.SetState(PlayerState.Free);    // player can walk again
    }

    private void ShowApp(PhoneApp app)
    {
        _currentApp = app;
        _homeScreen.SetActive(app == PhoneApp.Home);
        _bagScreen.SetActive(app == PhoneApp.Bag);
        _farmScreen.SetActive(app == PhoneApp.Farm);
        _statusBar.gameObject.SetActive(app != PhoneApp.Home);
    }

    // ── Updating the text ───────────────────────────────────────────────────

    private void Refresh()
    {
        string clock = _time != null ? _time.GetFormattedTime() : "--:--";
        _homeTime.text  = clock;
        _homeDate.text  = _time != null ? _time.GetFormattedDate() : "";
        _statusBar.text = clock;

        if (_currentApp == PhoneApp.Bag)  _bagText.text  = BagText();
        if (_currentApp == PhoneApp.Farm) _farmText.text = FarmText();
    }

    private string BagText()
    {
        if (_inventory == null) return "No inventory found.";

        string accent = ColorUtility.ToHtmlStringRGB(accentColor);
        var sb = new StringBuilder();
        sb.AppendLine($"Gold:  <b><color=#{accent}>{_inventory.GoldCount}</color></b>");
        sb.AppendLine($"Water:  <b>{(_inventory.HasWater ? "Full" : "Empty")}</b>");
        sb.AppendLine();
        sb.AppendLine("<b>Seeds</b>");

        bool any = false;
        foreach (SeedInventoryEntry entry in _inventory.SeedInventory)
        {
            if (entry == null || entry.seedData == null) continue;
            sb.AppendLine($"• {entry.seedData.seedName}  <b>x{entry.amount}</b>");
            any = true;
        }
        if (!any) sb.AppendLine("(none)");
        return sb.ToString();
    }

    private string FarmText()
    {
        if (_inventory == null) return "";
        string accent = ColorUtility.ToHtmlStringRGB(accentColor);
        int seeds = _inventory.SeedInventory.Where(e => e != null).Sum(e => e.amount);
        return $"Crops\n<size=56><b><color=#{accent}>{_inventory.CropCount}</color></b></size>\n\n" +
               $"Seeds in bag: <b>{seeds}</b>";
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

    // Buttons need an EventSystem to be clickable
    private static void MakeSureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ── Building the phone ──────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("Phone Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        Transform root = canvasGO.transform;

        // Phone body + screen
        _phone = Box(root, "Phone", phoneBody, new Vector2(1f, 0f), new Vector2(1f, 0f), _phoneHidden, new Vector2(300f, 560f));
        RectTransform screen = Stretch(Box(_phone, "Screen", phoneScreen, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero),
                                       new Vector2(14f, 44f), new Vector2(-14f, -44f));
        Box(_phone, "Speaker", new Color(0.25f, 0.25f, 0.28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(70f, 8f));
        MakeButton(_phone, "", new Color(0.25f, 0.25f, 0.28f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                   new Vector2(0f, 12f), new Vector2(44f, 20f), () => ShowApp(PhoneApp.Home));   // home button

        // Small clock at the top of app screens
        _statusBar = Label(screen, "StatusBar", 18, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(240f, 26f));

        // HOME screen
        _homeScreen = Stretch(Empty(screen, "Home"), Vector2.zero, Vector2.zero).gameObject;
        _homeTime = Label(_homeScreen.transform, "Time", 60, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(260f, 80f));
        _homeTime.fontStyle = FontStyles.Bold;
        _homeDate = Label(_homeScreen.transform, "Date", 20, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(260f, 30f));

        MakeButton(_homeScreen.transform, "Bag\n<size=14>[1]</size>", bagAppColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                   new Vector2(-62f, -60f), new Vector2(100f, 100f), () => ShowApp(PhoneApp.Bag));
        MakeButton(_homeScreen.transform, "Farm\n<size=14>[2]</size>", farmAppColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                   new Vector2(62f, -60f), new Vector2(100f, 100f), () => ShowApp(PhoneApp.Farm));

        // BAG app
        _bagScreen = Stretch(Empty(screen, "BagApp"), Vector2.zero, Vector2.zero).gameObject;
        Label(_bagScreen.transform, "Title", 30, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(240f, 40f)).text = "<b>Bag</b>";
        _bagText = Stretch(Label(_bagScreen.transform, "Text", 20, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform,
                           new Vector2(18f, 60f), new Vector2(-18f, -80f)).GetComponent<TextMeshProUGUI>();
        BackButton(_bagScreen.transform);

        // FARM app
        _farmScreen = Stretch(Empty(screen, "FarmApp"), Vector2.zero, Vector2.zero).gameObject;
        Label(_farmScreen.transform, "Title", 30, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(240f, 40f)).text = "<b>Farm</b>";
        _farmText = Stretch(Label(_farmScreen.transform, "Text", 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, Vector2.zero).rectTransform,
                            new Vector2(18f, 60f), new Vector2(-18f, -80f)).GetComponent<TextMeshProUGUI>();
        BackButton(_farmScreen.transform);

        // Hint text (bottom left)
        if (showHints)
        {
            TextMeshProUGUI hint = Label(root, "Hints", 18, TextAlignmentOptions.BottomLeft, Vector2.zero, new Vector2(24f, 18f), new Vector2(400f, 30f));
            hint.rectTransform.pivot = Vector2.zero;
            hint.text  = $"[{phoneKey}] Phone    [{bagShortcut}] Bag";
            hint.color = new Color(1f, 1f, 1f, 0.6f);
        }
    }

    private void BackButton(Transform parent)
    {
        MakeButton(parent, "Back", buttonColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                   new Vector2(0f, 12f), new Vector2(120f, 38f), () => ShowApp(PhoneApp.Home));
    }

    // ── Small helpers for making UI objects ─────────────────────────────────

    // A colored rectangle
    private static RectTransform Box(Transform parent, string name, Color color,
                                     Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta        = size;
        return rt;
    }

    // An invisible container
    private static RectTransform Empty(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // Make a rectangle fill its parent, with margins
    private static RectTransform Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    // A text label at a fixed spot
    private static TextMeshProUGUI Label(Transform parent, string name, float size, TextAlignmentOptions align,
                                         Vector2 anchor, Vector2 position, Vector2 boxSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta        = boxSize;

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize      = size;
        t.alignment     = align;
        t.color         = Color.white;
        t.raycastTarget = false;
        return t;
    }

    // A clickable button with a label
    private static Button MakeButton(Transform parent, string text, Color color, Vector2 anchor, Vector2 pivot,
                                     Vector2 position, Vector2 size, UnityAction onClick)
    {
        RectTransform rt = Box(parent, "Button " + text.Split('\n')[0], color, anchor, pivot, position, size);
        Image img = rt.GetComponent<Image>();
        img.raycastTarget = true;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        if (!string.IsNullOrEmpty(text))
        {
            TextMeshProUGUI label = Label(rt, "Label", 22, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            label.text = $"<b>{text}</b>";
        }
        return button;
    }
}