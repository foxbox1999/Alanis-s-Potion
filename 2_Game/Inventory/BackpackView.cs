using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>背包界面 View：按大类分页展示物品，点选看详情，支持 查看/选择/出售 三种上下文。</summary>
public class BackpackView : BaseUIView
{
    [Header("ToggleGroup")]
    [SerializeField] private Toggle HerbToggle;
    [SerializeField] private Toggle PotionToggle;
    [SerializeField] private Toggle OtherToggle;

    [Header("ItemsContent")]
    [SerializeField] private Transform ItemContent;
    [SerializeField] private GameObject ItemPrefab;

    [Header("ItemInfoPanel")]
    [SerializeField] private Image ItemImage;
    [SerializeField] private TextMeshProUGUI ItemName;
    [SerializeField] private TextMeshProUGUI ItemDescrip;
    [SerializeField] private TextMeshProUGUI ItemPrice;

    [Header("通过背包消耗相关物品")]
    [SerializeField] private GameObject ButtonParent;

    private int _currentTab;                         // 0=草药 1=药水 2=其他
    private string _selectedItemId;
    private System.Action _onCanceled;
    private InventoryContext _context = InventoryContext.View;
    private System.Action<Item> _onItemSelected;
    private Button _confirmBtn, _cancelBtn;

    protected override void OnOpen()
    {
        base.OnOpen();
        if (Data is InventoryPanelData panel)
        {
            _context = panel.Context;
            _onItemSelected = panel.OnItemSelected;
            _onCanceled = panel.OnCanceled;   // ★ 补上取消回调
        }
        else { _context = InventoryContext.View; _onItemSelected = null; }

        BindToggles();
        BindButtons();
        SelectTab(0);
        ClearItemInfoPanel();
        ButtonParent.SetActive(_context != InventoryContext.View);   // 选择/出售都显示确认取消，只有查看不显示
    }

    protected override void OnRefresh()
    {
        base.OnRefresh();
        RefreshItems();
    }

    private void BindToggles()
    {
        HerbToggle.onValueChanged.RemoveAllListeners();
        PotionToggle.onValueChanged.RemoveAllListeners();
        OtherToggle.onValueChanged.RemoveAllListeners();
        HerbToggle.onValueChanged.AddListener(isOn => { if (isOn) SelectTab(0); });
        PotionToggle.onValueChanged.AddListener(isOn => { if (isOn) SelectTab(1); });
        OtherToggle.onValueChanged.AddListener(isOn => { if (isOn) SelectTab(2); });
    }

    private void BindButtons()
    {
        if (ButtonParent == null) return;
        _confirmBtn = ButtonParent.transform.Find("Confirm")?.GetComponent<Button>();
        _cancelBtn = ButtonParent.transform.Find("Cancel")?.GetComponent<Button>();
        if (_confirmBtn != null) { _confirmBtn.onClick.RemoveAllListeners(); _confirmBtn.onClick.AddListener(OnConfirm); }
        if (_cancelBtn != null) { _cancelBtn.onClick.RemoveAllListeners(); _cancelBtn.onClick.AddListener(OnCancel); }
    }

    private void SelectTab(int tab)
    {
        _currentTab = tab;
        RefreshItems();
    }

    private bool MatchesTab(Item item) => _currentTab switch
    {
        0 => item.category == ItemCategory.Herb,
        1 => item.category == ItemCategory.Potion,
        _ => item.category == ItemCategory.Dessert || item.category == ItemCategory.Other
    };

    /// <summary>当前页应显示的所有物品（只看拥有的，数量 > 0）。</summary>
    private List<Item> GetFilteredItems()
    {
        var list = new List<Item>();
        foreach (var item in DatabaseManager.Instance.AllItems)
            if (InventoryManager.Instance.GetCount(item.id) > 0 && MatchesTab(item))
                list.Add(item);
        return list;
    }

    private void RefreshItems()
    {
        for (int i = ItemContent.childCount - 1; i >= 0; i--)
            Destroy(ItemContent.GetChild(i).gameObject);

        foreach (var item in GetFilteredItems())
        {
            GameObject obj = Instantiate(ItemPrefab, ItemContent);
            var cell = obj.GetComponent<ItemCell>() ?? obj.AddComponent<ItemCell>();
            cell.Setup(item.id, item.icon, InventoryManager.Instance.GetCount(item.id).ToString(), OnItemClick, item.name);
        }
    }

    private void OnItemClick(string itemId)
    {
        _selectedItemId = itemId;
        var item = DatabaseManager.Instance.GetItem(itemId);
        if (item == null) return;
        UpdateInfoPanel(item);

        // 选择模式：点选即回调并关闭（送礼/交易用）
        if (_context == InventoryContext.Select)
        {
            _onItemSelected?.Invoke(item);
            CloseSelf();
        }
    }

    private void UpdateInfoPanel(Item item)
    {
        ItemImage.sprite = item.icon;
        ItemImage.enabled = item.icon != null;
        ItemName.text = item.name;
        ItemDescrip.text = item.description;
        ItemPrice.text = $"{item.price} 银币";
    }

    private void ClearItemInfoPanel()
    {
        _selectedItemId = null;
        ItemImage.sprite = null;
        ItemImage.enabled = false;
        ItemName.text = "";
        ItemDescrip.text = "";
        ItemPrice.text = "";
    }

    private void OnConfirm()
    {
        if (string.IsNullOrEmpty(_selectedItemId)){
            PopupManager.Instance.ShowSidePrompt("还没选择任意物品呢");
            return;
        }
        var item = DatabaseManager.Instance.GetItem(_selectedItemId);
        if (item == null) return;

        if (_context == InventoryContext.Select)
        {
            _onItemSelected?.Invoke(item);   // 送礼：把选中的物品交给对话
            CloseSelf();
        }
        else if (_context == InventoryContext.Sell)
        {
            if (InventoryManager.Instance.Remove(_selectedItemId, 1))
            {
                PlayerDataManager.Instance.AddSilver(item.price);
                ClearItemInfoPanel();
                RefreshItems();
            }
        }
    }

    private void OnCancel()
    {
        if (_context == InventoryContext.Select)
        {
            _onCanceled?.Invoke();   // 送礼取消
            CloseSelf();
        }
        else
        {
            ClearItemInfoPanel();   // 出售取消选择
        }
    }
}