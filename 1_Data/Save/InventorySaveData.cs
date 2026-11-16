using System;
using System.Collections.Generic;

/// <summary>背包存档：物品堆叠列表。</summary>
[Serializable]
public class InventorySaveData
{
    /// <summary>所有物品堆叠。</summary>
    public List<ItemStackData> items = new List<ItemStackData>();
}

/// <summary>单个物品堆叠（同 itemId 合并数量）。</summary>
[Serializable]
public class ItemStackData
{
    /// <summary>物品 id。</summary>
    public string itemId;

    /// <summary>数量。</summary>
    public int count;

    /// <summary>物品大类。</summary>
    public ItemCategory category;

    /// <summary>品质（高级药等，0 = 普通）。</summary>
    public int quality;
}