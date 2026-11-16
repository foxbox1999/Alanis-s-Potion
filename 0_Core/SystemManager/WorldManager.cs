using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 世界管理器：负责「世界级」状态——交租倒计时、天数、已解锁区域、世界旗标。
/// 只存世界级数据，不碰玩家/背包/对话等其它模块。
/// </summary>
public class WorldManager : MonoSingleton<WorldManager>, ISaveable
{
    #region 存档契约
    public string SaveKey => "world";
    public int SaveVersion => 1;
    #endregion

    #region 配置
    [Header("交租配置")]
    [Tooltip("交租周期（游戏天数）")]
    [SerializeField] private int _rentPeriodDays = 90;
    [SerializeField] private int _rentAmount = 60;   // 每期租金（银币）
    [Tooltip("首次交租在游戏第几天")]
    [SerializeField] private int _firstRentDay = 90;
    #endregion

    #region 运行时数据
    private IDisposable _newDaySub;
    private int _nextRentDay;                              // 下次交租的绝对游戏日
    private readonly List<string> _unlockedRegions = new(); // 已解锁区域（运行时用 List）
    private readonly HashSet<string> _worldFlags = new();   // 世界旗标（运行时用 HashSet，内存更省、查更快）
    #endregion

    #region 公共属性
    /// <summary>当前游戏天数（从 TimeManager 总秒数推算，0 起，不额外存，省内存）。</summary>
    public int CurrentDay => Mathf.FloorToInt(TimeManager.Instance.CurrentTotalSeconds / 86400f);

    /// <summary>距下次交租还剩多少天（最小 0）。</summary>
    public int RentDaysLeft => Mathf.Max(0, _nextRentDay - CurrentDay);

    /// <summary>下次交租的绝对游戏日。</summary>
    public int NextRentDay => _nextRentDay;
    #endregion

    #region 生命周期
    protected override void Awake()
    {
        base.Awake();
        _nextRentDay = _firstRentDay;   // 默认值（读档时会覆盖）
    }

    protected override void Start()
    {
        base.Start();
        SaveManager.Instance?.RegisterModule(this);   // 自注册为存档模块
        _newDaySub = EventBus.Instance.Subscribe<NewDayEvent>(e => CheckRent(), -10);
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _newDaySub?.Dispose();
    }
    /// <summary>交租检查：到租日扣银币并推下次交租日。</summary>
    private void CheckRent()
    {
        if (CurrentDay < _nextRentDay) return;
        if (PlayerDataManager.Instance.TrySpendSilver(_rentAmount))
        {
            PayRent();
            PopupManager.Instance.ShowSidePrompt($"[WorldManager] 已交租 {_rentAmount} 银币，下次交租日: 第{_nextRentDay}天");

        }
        else
        {
            // 余额不足：本次不推进，下个 NewDay 再试（欠债/惩罚机制以后可加在这里）
            PopupManager.Instance.ShowSidePrompt($"[WorldManager] 租金不足，无法交租（需 {_rentAmount} 银币）,维格怀疑值+10");
            PopupManager.Instance.ShowInfo("提示", "请尽快补交租金，每拖一天，维格怀疑值+2");
        }
    }
    #endregion

    #region 交租
    /// <summary>支付本次租金，把下次交租日往后推一个周期。</summary>
    public void PayRent()
    {
        _nextRentDay += _rentPeriodDays;
        Debug.Log($"[WorldManager] 已交租，下次交租日: 第{_nextRentDay}天");
    }
    #endregion

    #region 区域解锁（未来用）
    public bool IsRegionUnlocked(string regionId) => _unlockedRegions.Contains(regionId);

    public void UnlockRegion(string regionId)
    {
        if (!_unlockedRegions.Contains(regionId))
            _unlockedRegions.Add(regionId);
    }
    #endregion

    #region 世界旗标（未来用）
    public bool HasFlag(string flag) => _worldFlags.Contains(flag);
    public void SetFlag(string flag) => _worldFlags.Add(flag);
    public void ClearFlag(string flag) => _worldFlags.Remove(flag);
    #endregion

    #region 存档（ISaveable）
    public string SerializeState() => JsonUtility.ToJson(new WorldSaveData
    {
        nextRentDay = _nextRentDay,
        unlockedRegions = new List<string>(_unlockedRegions),
        worldFlags = new List<string>(_worldFlags)
    });

    public void DeserializeState(string json, int version)
    {
        if (string.IsNullOrEmpty(json)) { ResetState(); return; }
        var dto = JsonUtility.FromJson<WorldSaveData>(json);
        if (dto == null) return;

        _nextRentDay = dto.nextRentDay;
        _unlockedRegions.Clear();
        if (dto.unlockedRegions != null) _unlockedRegions.AddRange(dto.unlockedRegions);
        _worldFlags.Clear();
        if (dto.worldFlags != null)
            foreach (var f in dto.worldFlags) _worldFlags.Add(f);
    }

    public void ResetState()
    {
        _nextRentDay = _firstRentDay;
        _unlockedRegions.Clear();
        _worldFlags.Clear();
    }
    #endregion
}