using System.Collections.Generic;
using UnityEngine;

/// <summary>采集管理器：消耗游戏时间 + 随机发草药（无次数限制）。</summary>
public class GatherManager : MonoSingleton<GatherManager>
{
    [Header("耗时")]
    [Tooltip("每次采集消耗的游戏小时数（随机范围），想固定就设成两个相等")]
    [SerializeField] private Vector2 _gatherHoursRange = new Vector2(3f, 4f);

    private string _currentZoneId;
    private bool _isGathering;

    /// <summary>开始采集（动画播放前调用）。</summary>
    public void StartGather(string zoneId)
    {
        _currentZoneId = zoneId;
        _isGathering = true;
        GameManager.Instance.EnterBusy();   // 动画期间置 busy
    }

    /// <summary>采集完成：发草药 + 推进时间。返回获得的物品（供奖励弹窗）。此函数里面已经进入仓库</summary>
    public List<ItemStackData> CompleteGather()
    {
        if (!_isGathering) return null;
        _isGathering = false;

        var gained = CompleteGather(_currentZoneId);   // ★ 复用发草药逻辑
        TimeManager.Instance.AddTime(Random.Range(_gatherHoursRange.x, _gatherHoursRange.y) * 3600f);
        return gained;
    }
    /// <summary>养子专用的采集完成：只是发草药。返回获得的物品（供奖励弹窗）,实际已经进入仓库</summary>
    public List<ItemStackData> CompleteGather(string zoneId)
    {
        var gained = new List<ItemStackData>();
        if (string.IsNullOrEmpty(zoneId)) return gained;

        foreach (var herb in DatabaseManager.Instance.GetHerbsByZone(zoneId))
        {
            int qty = Random.Range(herb.minQuantity, herb.maxQuantity + 1);
            if (qty <= 0) continue;
            InventoryManager.Instance.Add(herb.id, qty);
            gained.Add(new ItemStackData { itemId = herb.id, count = qty });
        }
        return gained;
    }
}