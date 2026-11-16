using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>商店界面（沿用 Exchange 预制件）：商品格子显示随机库存数量，点选看价格、确认购买。</summary>
public class ShopView : BaseUIView
{
    [Header("展示用")]
    [SerializeField] private Image ItemImage;
    [SerializeField] private TextMeshProUGUI ItemName;
    [SerializeField] private TextMeshProUGUI ItemPrice;
    [SerializeField] private GameObject ItemPrefab;   // 挂 ItemCell 的格子
    [SerializeField] private Transform ItemsPanel;

    private ShopType _type;
    private float _priceMultiplier = 1.1f;
    private string _selectedItemId;

    private readonly Dictionary<string, int> _stock = new();       // itemId → 剩余库存
    private readonly Dictionary<string, ItemCell> _cells = new();  // itemId → 格子

    protected override void OnOpen()
    {
        base.OnOpen();
        if (Data is ShopPanelData panel)
        {
            _type = panel.Type;
            _priceMultiplier = panel.PriceMultiplier;
        }
        GenerateStock();
        UpdatePanel();
    }

    // 打开时随机生成库存：每个商品 0~3 个
    private void GenerateStock()
    {
        _stock.Clear();
        foreach (var item in GetShopItems())
            _stock[item.id] = Random.Range(0, 4);
    }

    private void UpdatePanel()
    {
        ClearPanel();
        _cells.Clear();

        string firstId = null;
        foreach (var item in GetShopItems())
        {
            int qty = _stock.TryGetValue(item.id, out var q) ? q : 0;
            if (qty <= 0) continue;   // 0 就不卖，不显示

            GameObject obj = Instantiate(ItemPrefab, ItemsPanel);
            obj.name = item.id;
            var cell = obj.GetComponent<ItemCell>() ?? obj.AddComponent<ItemCell>();
            cell.Setup(item.id, item.icon, qty.ToString(), OnClickItem, item.name);  // 格子显示数量
            _cells[item.id] = cell;
            if (firstId == null) firstId = item.id;
        }

        if (firstId != null) OnClickItem(firstId);
        else { ItemImage.enabled = false; ItemName.text = ""; ItemPrice.text = ""; }
    }

    private void OnClickItem(string itemId)
    {
        _selectedItemId = itemId;
        var item = DatabaseManager.Instance.GetItem(itemId);
        if (item == null) return;

        UpdateItemVisual(itemId);

        ItemImage.sprite = item.icon;
        ItemImage.enabled = item.icon != null;
        ItemName.text = item.name;
        ItemPrice.text = GetPrice(item).ToString();   // 价格放详情面板
    }

    private void UpdateItemVisual(string selectedId)
    {
        foreach (var kv in _cells)
            kv.Value.SetVisual(kv.Key == selectedId ? ItemVisual.Selected : ItemVisual.Default);
    }

    public void OnBuyButton()
    {
        if (string.IsNullOrEmpty(_selectedItemId)) return;
        var item = DatabaseManager.Instance.GetItem(_selectedItemId);
        if (item == null) return;

        int price = GetPrice(item);
        int qty = _stock.TryGetValue(item.id, out var q) ? q : 0;
        PopupManager.Instance.ShowConfirm("确认购买",
            $"确认要购买 {item.name}*{qty} 吗？需要花费 {price* qty} 枚银币",
            () => OnConfirmBuy(item, price));
    }

    private void OnConfirmBuy(Item item, int price)
    {
        //1.获取数量
        int qty = _stock.TryGetValue(item.id, out var q) ? q : 0;
        if (qty <= 0) { PopupManager.Instance.ShowSidePrompt("已售罄"); return; }
        //2.消耗金币
        if (PlayerDataManager.Instance.TrySpendSilver(price*qty))
        {
            InventoryManager.Instance.Add(item.id, qty);
            _stock[item.id] = 0;
            RefreshStock(item.id);
        }
        else
        {
            PopupManager.Instance.ShowSidePrompt("银币不足");
        }
    }

    // 买完刷新格子：还有就更新数量，卖完就移除
    private void RefreshStock(string itemId)
    {
        int qty = _stock[itemId];
        if (_cells.TryGetValue(itemId, out var cell))
        {
            if (qty > 0)
            {
                cell.SetCount(qty.ToString());
            }
            else
            {
                Destroy(cell.gameObject);
                _cells.Remove(itemId);
                _selectedItemId = null;
                ItemImage.enabled = false;
                ItemName.text = "";
                ItemPrice.text = "";
            }
        }
    }

    private List<Item> GetShopItems()
    {
        var list = new List<Item>();
        if (_type == ShopType.Dessert)
        {
            foreach (var item in DatabaseManager.Instance.AllItems)
                if (item.category == ItemCategory.Dessert) list.Add(item);
        }
        else
        {
            foreach (var herb in DatabaseManager.Instance.GetHerbsByZone("Zone_Witcher"))
                list.Add(herb);
        }
        return list;
    }

    private int GetPrice(Item item) => Mathf.RoundToInt(item.price * _priceMultiplier);

    private void ClearPanel()
    {
        foreach (Transform child in ItemsPanel)
            Destroy(child.gameObject);
    }
}