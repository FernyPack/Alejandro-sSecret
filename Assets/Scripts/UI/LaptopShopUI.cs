using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// LAPTOP SHOP (put on any object in the scene, e.g. an empty "LaptopShopUI")
// When you use the laptop, this shows the shop as a web browser window instead of the old shop screen.
// It still uses the owner's OnlineShop for buying, selling, gold and the delivery package.
//
// CONTROLS
//   Mouse = click Buy / Sell / Sell all
//   X button or Esc = close the browser
//
// WHERE TO CHANGE THINGS (search for "EDIT:")
//   EDIT: WEBSITE     -> shop name, web address, slogan
//   EDIT: COLORS      -> browser and page colors
//   EDIT: MESSAGES    -> texts shown after buying / selling
//   EDIT: LAYOUT      -> size of the browser window and position of the cards
public class LaptopShopUI : MonoBehaviour
{
    // EDIT: WEBSITE
    [Header("Website")]
    public string siteName   = "SeedNet";
    public string webAddress = "https://www.seednet.shop";
    public string slogan     = "Fresh seeds delivered to your door";

    // EDIT: COLORS (r, g, b, alpha from 0 to 1). You can also change these in the Inspector.
    [Header("Colors")]
    public Color windowFrame = new Color(0.80f, 0.82f, 0.86f, 1f);
    public Color tabColor    = new Color(0.97f, 0.97f, 0.98f, 1f);
    public Color pageColor   = new Color(0.97f, 0.97f, 0.98f, 1f);
    public Color bannerColor = new Color(0.26f, 0.58f, 0.34f, 1f);
    public Color cardColor   = Color.white;
    public Color buyColor    = new Color(0.26f, 0.58f, 0.34f, 1f);
    public Color sellColor   = new Color(0.90f, 0.58f, 0.20f, 1f);
    public Color closeColor  = new Color(0.86f, 0.25f, 0.25f, 1f);
    public Color textColor   = new Color(0.15f, 0.15f, 0.18f, 1f);

    private OnlineShop _shop;
    private ShopUI     _oldShopScreen;
    private IInventory _inventory;

    private GameObject      _window;
    private bool            _open;
    private TextMeshProUGUI _goldText, _productName, _productInfo, _sellInfo, _statusText;
    private Image           _productIcon;

    void Start()
    {
        _shop          = FindFirstObjectByType<OnlineShop>();
        _oldShopScreen = FindFirstObjectByType<ShopUI>();
        _inventory     = ServiceLocator.Get<IInventory>();

        MakeSureEventSystemExists();
        BuildUI();
        _window.SetActive(false);
    }

    void Update()
    {
        // The laptop opened the old shop screen -> hide it and show the browser instead
        if (!_open && _oldShopScreen != null && _oldShopScreen.shopCanvas != null && _oldShopScreen.shopCanvas.activeSelf)
        {
            _oldShopScreen.shopCanvas.SetActive(false);
            Open();
        }

        if (_open)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            else if (!PlayerStateManager.IsState(PlayerState.Shop)) HideWindow();   // closed by something else
            else Refresh();
        }
    }

    // ── Opening / closing ───────────────────────────────────────────────────

    private void Open()
    {
        _open = true;
        _window.SetActive(true);
        _statusText.text = "";
        Refresh();
    }

    private void Close()
    {
        HideWindow();
        if (PlayerStateManager.IsState(PlayerState.Shop))
            PlayerStateManager.SetState(PlayerState.Free);   // player can walk again
    }

    private void HideWindow()
    {
        _open = false;
        _window.SetActive(false);
    }

    // ── Buttons ─────────────────────────────────────────────────────────────

    private void Buy()
    {
        if (_shop == null || _inventory == null) return;
        int goldBefore = _inventory.GoldCount;
        _shop.BuySeed();

        // EDIT: MESSAGES
        _statusText.text = _inventory.GoldCount < goldBefore
            ? "Order placed! Your package is on its way to the door."
            : "Not enough gold for this seed.";
    }

    private void SellOne()
    {
        if (_shop == null || _inventory == null) return;
        if (_inventory.CropCount <= 0) { _statusText.text = "You have no crops to sell."; return; }
        _shop.SellCrop();
        _statusText.text = $"Sold 1 crop for {_shop.cropValue} gold.";
    }

    private void SellAll()
    {
        if (_shop == null || _inventory == null) return;
        int count = _inventory.CropCount;
        if (count <= 0) { _statusText.text = "You have no crops to sell."; return; }
        for (int i = 0; i < count; i++) _shop.SellCrop();
        _statusText.text = $"Sold {count} crops for {count * _shop.cropValue} gold.";
    }

    // ── Updating the page ───────────────────────────────────────────────────

    private void Refresh()
    {
        _goldText.text = _inventory != null ? $"Your gold: <b>{_inventory.GoldCount}</b>" : "";

        if (_shop != null && _shop.seedForSale != null)
        {
            SeedData seed = _shop.seedForSale;
            _productName.text = $"<b>{seed.seedName}</b>";
            _productInfo.text = $"{seed.description}\n\nGrows in {seed.daysToGrow:0.#} day(s)\n" +
                                $"<size=30><b>{_shop.seedCost} gold</b></size>";
            _productIcon.sprite  = seed.seedIcon;
            _productIcon.enabled = seed.seedIcon != null;
        }
        else
        {
            _productName.text = "<b>Out of stock</b>";
            _productInfo.text = "";
            _productIcon.enabled = false;
        }

        int crops = _inventory != null ? _inventory.CropCount : 0;
        int price = _shop != null ? _shop.cropValue : 0;
        _sellInfo.text = $"You have <b>{crops}</b> crop(s)\nWe pay <b>{price} gold</b> each";
    }

    // Buttons need an EventSystem to be clickable
    private static void MakeSureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ── Building the browser ────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("Laptop Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        // Dark see-through background behind the window
        RectTransform dim = Stretch(Box(canvasGO.transform, "Dim", new Color(0f, 0f, 0f, 0.45f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), Vector2.zero, Vector2.zero);
        _window = dim.gameObject;

        // EDIT: LAYOUT — browser window size (width x height)
        RectTransform win = Box(dim, "Browser", windowFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1150f, 700f));

        // Tab bar
        RectTransform tab = Box(win, "Tab", tabColor, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(260f, 36f));
        Label(tab, "TabTitle", 18, TextAlignmentOptions.MidlineLeft, new Vector2(0.5f, 0.5f), new Vector2(-10f, 0f), new Vector2(220f, 36f), textColor).text = siteName;
        MakeButton(win, "X", closeColor, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(46f, 32f), Close);
        Label(win, "Minimize", 22, TextAlignmentOptions.Center, new Vector2(1f, 1f), new Vector2(-110f, -24f), new Vector2(40f, 32f), textColor).text = "-";

        // Address bar
        RectTransform bar = Box(win, "AddressRow", tabColor, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -44f), new Vector2(1150f, 46f));
        Label(bar, "Arrows", 22, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(80f, 40f), new Color(0.5f, 0.5f, 0.55f)).text = "<  >";
        RectTransform field = Box(bar, "Address", new Color(0.90f, 0.91f, 0.93f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(1030f, 32f));
        Label(field, "Url", 17, TextAlignmentOptions.MidlineLeft, new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(1000f, 32f), new Color(0.3f, 0.3f, 0.35f)).text = webAddress;

        // Web page
        RectTransform page = Stretch(Box(win, "Page", pageColor, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), new Vector2(0f, 0f), new Vector2(0f, -90f));

        // Banner
        RectTransform banner = Box(page, "Banner", bannerColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1150f, 100f));
        Label(banner, "SiteName", 44, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(240f, 12f), new Vector2(440f, 56f), Color.white).text = $"<b>{siteName}</b>";
        Label(banner, "Slogan", 18, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(240f, -26f), new Vector2(440f, 26f), new Color(1f, 1f, 1f, 0.85f)).text = slogan;
        _goldText = Label(banner, "Gold", 24, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(300f, 40f), Color.white);

        // EDIT: LAYOUT — product card (left)
        RectTransform product = Box(page, "ProductCard", cardColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-280f, -130f), new Vector2(500f, 380f));
        Label(product, "Heading", 16, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(460f, 24f), new Color(0.45f, 0.45f, 0.5f)).text = "TODAY'S SEED";
        _productIcon = Box(product, "Icon", Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -50f), new Vector2(140f, 140f)).GetComponent<Image>();
        _productIcon.preserveAspect = true;
        _productName = Label(product, "Name", 28, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(345f, -70f), new Vector2(310f, 40f), textColor);
        _productInfo = Label(product, "Info", 18, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(345f, -190f), new Vector2(310f, 170f), textColor);
        MakeButton(product, "Buy", buyColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(460f, 56f), Buy);

        // EDIT: LAYOUT — sell card (right)
        RectTransform sell = Box(page, "SellCard", cardColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(280f, -130f), new Vector2(500f, 380f));
        Label(sell, "Heading", 16, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(460f, 24f), new Color(0.45f, 0.45f, 0.5f)).text = "SELL YOUR HARVEST";
        _sellInfo = Label(sell, "Info", 24, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(460f, 120f), textColor);
        MakeButton(sell, "Sell 1", sellColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-117f, 20f), new Vector2(226f, 56f), SellOne);
        MakeButton(sell, "Sell all", sellColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(117f, 20f), new Vector2(226f, 56f), SellAll);

        // Message line at the bottom of the page
        _statusText = Label(page, "Status", 20, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1000f, 40f), textColor);
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
        img.raycastTarget = name == "Dim";   // the dark background blocks clicks behind the window

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta        = size;
        return rt;
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
                                         Vector2 anchor, Vector2 position, Vector2 boxSize, Color color)
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
        t.color         = color;
        t.raycastTarget = false;
        return t;
    }

    // A clickable button with a label
    private static Button MakeButton(Transform parent, string text, Color color, Vector2 anchor, Vector2 pivot,
                                     Vector2 position, Vector2 size, UnityAction onClick)
    {
        RectTransform rt = Box(parent, "Button " + text, color, anchor, pivot, position, size);
        Image img = rt.GetComponent<Image>();
        img.raycastTarget = true;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        Label(rt, "Label", 22, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white).text = $"<b>{text}</b>";
        return button;
    }
}