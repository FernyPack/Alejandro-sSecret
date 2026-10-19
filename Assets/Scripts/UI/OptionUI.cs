using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour
{
    [Header("Input (Optional)")]
    [Tooltip("Drag your InputReader asset here if you want Esc / Right Click to back out of a menu.")]
    public InputReader inputReader;

    [Tooltip("ON: Esc or Right Click closes the open menu. OFF: players must click a button.")]
    public bool allowCancel = true;

    [Header("YesNo Panel")]
    public GameObject yesNoPanel;
    public Button     yesButton;
    public Button     noButton;

    [Header("Inventory List Panel")]
    public GameObject inventoryListPanel;
    public Transform  inventoryListContainer;
    public GameObject seedListItemPrefab;

    private Action _onYes;
    private Action _onNo;
    private bool   _yesNoActive = false;

    private List<SeedListItem> _listItems = new List<SeedListItem>();
    private bool _listActive = false;

    public bool IsMenuOpen => _yesNoActive || _listActive;

    void Start()
    {
        if (yesNoPanel         != null) yesNoPanel.SetActive(false);
        if (inventoryListPanel != null) inventoryListPanel.SetActive(false);

        if (yesButton != null) yesButton.onClick.AddListener(ConfirmYes);
        if (noButton  != null) noButton.onClick.AddListener(ConfirmNo);

        if (inputReader != null)
            inputReader.OnCancelEvent += HandleCancel;
    }

    void OnDestroy()
    {
        if (inputReader != null)
            inputReader.OnCancelEvent -= HandleCancel;
    }

    public void ShowYesNo(Action onYes, Action onNo = null)
    {
        _onYes       = onYes;
        _onNo        = onNo;
        _yesNoActive = true;
        _listActive  = false;

        if (yesNoPanel         != null) yesNoPanel.SetActive(true);
        if (inventoryListPanel != null) inventoryListPanel.SetActive(false);
    }

    public void ShowInventoryList(List<SeedInventoryEntry> seeds, Action<SeedData> onSelected)
    {
        _yesNoActive = false;
        _listActive  = true;

        if (yesNoPanel         != null) yesNoPanel.SetActive(false);
        if (inventoryListPanel != null) inventoryListPanel.SetActive(true);

        foreach (var item in _listItems)
            if (item != null) Destroy(item.gameObject);
        _listItems.Clear();

        if (seeds == null || seeds.Count == 0)
        {
            HidePanels();
            return;
        }

        foreach (var entry in seeds)
        {
            if (seedListItemPrefab == null || inventoryListContainer == null) continue;

            GameObject   obj  = Instantiate(seedListItemPrefab, inventoryListContainer);
            SeedListItem item = obj.GetComponent<SeedListItem>();

            if (item != null)
            {
                item.Setup(entry, () =>
                {
                    HidePanels();
                    onSelected?.Invoke(entry.seedData);
                });
                _listItems.Add(item);
            }
        }
    }

    public void HidePanels()
    {
        _yesNoActive = false;
        _listActive  = false;

        if (yesNoPanel         != null) yesNoPanel.SetActive(false);
        if (inventoryListPanel != null) inventoryListPanel.SetActive(false);
    }

    private void HandleCancel()
    {
        if (!allowCancel) return;

        if (_yesNoActive)     ConfirmNo();
        else if (_listActive) HidePanels();
    }

    private void ConfirmYes()
    {
        _yesNoActive = false;
        if (yesNoPanel != null) yesNoPanel.SetActive(false);
        _onYes?.Invoke();
    }

    private void ConfirmNo()
    {
        HidePanels();
        _onNo?.Invoke();
    }
}
