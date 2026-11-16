using System.Collections.Generic;
using UnityEngine;

/// <summary>背包管理器：物品增删查、堆叠。实现 ISaveable。</summary>
public class InventoryManager : MonoSingleton<InventoryManager>, ISaveable
{
    public string SaveKey => "inventory";
    public int SaveVersion => 1;

    private readonly Dictionary<string, int> _counts = new();   // itemId → 数量

    protected override void Start() { base.Start(); SaveManager.Instance?.RegisterModule(this); }

    /// <summary>
    /// 向仓库中添加物品
    /// </summary>
    /// <param name="itemId">物品id</param>
    /// <param name="count">数量</param>
    public void Add(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;
        _counts[itemId] = GetCount(itemId) + count;
    }

    /// <summary>扣除；数量不足返回 false（不扣）。</summary>
    public bool Remove(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
        int cur = GetCount(itemId);
        if (cur < count) return false;
        int left = cur - count;
        if (left <= 0) _counts.Remove(itemId); else _counts[itemId] = left;
        return true;
    }

    public int GetCount(string itemId) => _counts.TryGetValue(itemId, out var c) ? c : 0;
    public bool Has(string itemId, int count = 1) => GetCount(itemId) >= count;

    #region ISaveable
    public string SerializeState()
    {
        var data = new InventorySaveData();
        foreach (var kv in _counts)
            data.items.Add(new ItemStackData { itemId = kv.Key, count = kv.Value, quality = 0 });
        return JsonUtility.ToJson(data);
    }

    public void DeserializeState(string json, int version)
    {
        if (string.IsNullOrEmpty(json)) { ResetState(); return; }
        var dto = JsonUtility.FromJson<InventorySaveData>(json);
        _counts.Clear();
        if (dto?.items != null)
            foreach (var s in dto.items)
                if (!string.IsNullOrEmpty(s.itemId)) _counts[s.itemId] = s.count;
    }

    public void ResetState() => _counts.Clear();
    #endregion
}