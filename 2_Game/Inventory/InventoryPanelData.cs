using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPanelData : UIPanelData
{
    /// <summary>
    /// 背包上下文，打开背包的目的
    /// </summary>
    public InventoryContext Context { get; set; } = InventoryContext.View;

    /// <summary>
    /// 选择后的物品回调，可以传入item作为参数
    /// </summary>
    public Action<Item> OnItemSelected { get; set; }

    /// <summary>
    /// 取消选择的回调
    /// </summary>
    public Action OnCanceled { get; set; }   // 新增：送礼取消
}