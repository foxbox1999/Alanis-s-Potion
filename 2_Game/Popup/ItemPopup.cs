using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 物品获得提示视图层
/// </summary>
public class ItemPopup : PopupViewBase
{
    #region 配置
    [Header("组件")]
    [SerializeField] private Transform ItemContent;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject ItemPrefab;

    #endregion

    #region 核心方法
    protected override void Bind()
    {
        if (Popup == null) return;
        ShowItem();
        confirmButton?.onClick.RemoveAllListeners();
        confirmButton?.onClick.AddListener(ConfirmAndClose);
    }
    /// <summary>
    /// 将要渲染的物品展示在获得物品框里面
    /// </summary>
    private void ShowItem()
    {
        if (Popup == null) return;
        if (Popup.items.Count < 1) return;
        foreach (Transform child in ItemContent)
        {
            Destroy(child.gameObject);
        }
        foreach (var item in Popup.items)
        {
            var itemData = DatabaseManager.Instance.GetItem(item.itemId);
            GameObject obj = Instantiate(ItemPrefab, ItemContent);
            var itemobj = obj.GetComponent<ItemCell>() ?? obj.AddComponent<ItemCell>();
            //以后做一个接口，itemobj.action里面专门传入用于打开信息展示窗口，现在donothing
            itemobj.Setup(item.itemId, itemData.icon, item.count.ToString(), null);
        }
    }
    #endregion
}
