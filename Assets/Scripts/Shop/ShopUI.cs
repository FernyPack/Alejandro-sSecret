using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Input (Optional)")]
    [Tooltip("Drag your InputReader asset here if you want Esc / Right Click to close the shop.")]
    public InputReader inputReader;

    [Header("UI Panel Reference")]
    public GameObject shopCanvas;

    [Header("Button References")]
    public Button buySeedButton;
    public Button sellCropButton;
    public Button closeButton;

    private OnlineShop onlineShop;

    void Start()
    {
        onlineShop = GetComponent<OnlineShop>();

        if (shopCanvas != null) shopCanvas.SetActive(false);

        if (buySeedButton  != null) buySeedButton.onClick.AddListener(onlineShop.BuySeed);
        if (sellCropButton != null) sellCropButton.onClick.AddListener(onlineShop.SellCrop);
        if (closeButton    != null) closeButton.onClick.AddListener(CloseShop);

        if (inputReader != null)
            inputReader.OnCancelEvent += HandleCancel;
    }

    void OnDestroy()
    {
        if (inputReader != null)
            inputReader.OnCancelEvent -= HandleCancel;
    }

    private void HandleCancel()
    {
        if (PlayerStateManager.IsState(PlayerState.Shop))
            CloseShop();
    }

    public void OpenShop()
    {
        if (shopCanvas == null) return;

        shopCanvas.SetActive(true);
        PlayerStateManager.SetState(PlayerState.Shop);
    }

    public void CloseShop()
    {
        if (shopCanvas == null) return;

        shopCanvas.SetActive(false);
        PlayerStateManager.SetState(PlayerState.Free);
    }
}
