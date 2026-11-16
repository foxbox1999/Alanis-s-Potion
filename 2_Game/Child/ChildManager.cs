using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>养子管理器：4 属性、每日任务、每日结算。实现 ISaveable。</summary>
public class ChildManager : MonoSingleton<ChildManager>, ISaveable
{
    #region 配置

    public string SaveKey => "child";
    public int SaveVersion => 1;

    private int _magic, _stamina, _commonSense, _alchemy;
    #endregion

    #region 运行时数据
    private readonly List<string> _assignedTaskIds = new();
    private int _taskDayIndex = -1;
    private IDisposable _newDaySub;
    public bool isDayTaskPlan => _taskDayIndex == WorldManager.Instance.CurrentDay;
    #endregion

    #region 生命周期
    protected override void Start()
    {
        base.Start();
        SaveManager.Instance?.RegisterModule(this);
        _newDaySub = EventBus.Instance.Subscribe<NewDayEvent>(e => SettleDailyTasks(), -10);  // 结算优先
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _newDaySub?.Dispose();
    }
    #endregion

    #region 接口
    /// <summary>当天已安排的任务 id（未安排/跨天返回 null）。只读，供 UI 回显。</summary>
    public IReadOnlyList<string> TodayPlan => isDayTaskPlan ? _assignedTaskIds : (IReadOnlyList<string>)null;

    /// <summary>
    /// 获得每个属性的数值
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public int GetStat(ChildStatType t) => t switch
    {
        ChildStatType.Magic => _magic,
        ChildStatType.Stamina => _stamina,
        ChildStatType.CommonSense => _commonSense,
        ChildStatType.Alchemy => _alchemy,
        _ => 0
    };
    /// <summary>
    /// 为某个属性增加数值
    /// </summary>
    /// <param name="t">目标</param>
    /// <param name="delta">具体值，减少用负数</param>
    public void AddStat(ChildStatType t, int delta)
    {
        switch (t)
        {
            case ChildStatType.Magic: _magic += delta; break;
            case ChildStatType.Stamina: _stamina += delta; break;
            case ChildStatType.CommonSense: _commonSense += delta; break;
            case ChildStatType.Alchemy: _alchemy += delta; break;
        }
    }

    /// <summary>安排当天任务。</summary>
    public void AssignDailyTasks(List<string> taskIds)
    {
        _assignedTaskIds.Clear();
        _assignedTaskIds.AddRange(taskIds);
        _taskDayIndex = WorldManager.Instance.CurrentDay;
    }

    /// <summary>每日结算：把已安排任务奖励加到属性（醒来时调用）。</summary>
    public void SettleDailyTasks()
    {
        List<ItemStackData> gained = new List<ItemStackData>();
        string msg = "昨日养子完成了以下任务：\n";
        foreach (var id in _assignedTaskIds)
        {
            var m = DatabaseManager.Instance.GetMission(id);
            if (m == null) continue;
            if (m.type == ChildTaskType.Course)
            {
                AddStat(m.stat, m.statGain);
                msg += $"·{m.name}，获得 {m.statGain} 点 {m.stat}\n";
            }
            else if (m.type == ChildTaskType.Expedition)
            {
                //根据去往的目的地，随机获取草药素材
                if (gained != null) gained.AddRange(GatherManager.Instance.CompleteGather(m.zoneId));
            }
        }
        //一起弹窗
        if (msg != "昨日养子完成了以下任务：\n")
            PopupManager.Instance.ShowInfo("任务完成", msg);
        if (gained != null && gained.Count > 0)
            PopupManager.Instance.ShowItem(gained);
        _assignedTaskIds.Clear();
    }
    #endregion

    #region ISaveable
    public string SerializeState() => JsonUtility.ToJson(new ChildSaveData
    {
        magic = _magic,
        stamina = _stamina,
        commonSense = _commonSense,
        alchemy = _alchemy,
        taskDayIndex = _taskDayIndex,
        assignedTaskIds = new List<string>(_assignedTaskIds)
    });

    public void DeserializeState(string json, int version)
    {
        if (string.IsNullOrEmpty(json)) { ResetState(); return; }
        var dto = JsonUtility.FromJson<ChildSaveData>(json);
        if (dto == null) return;
        _magic = dto.magic; _stamina = dto.stamina; _commonSense = dto.commonSense; _alchemy = dto.alchemy;
        _taskDayIndex = dto.taskDayIndex;
        _assignedTaskIds.Clear();
        if (dto.assignedTaskIds != null) _assignedTaskIds.AddRange(dto.assignedTaskIds);
    }

    public void ResetState() { _magic = _stamina = _commonSense = _alchemy = 0; _assignedTaskIds.Clear(); _taskDayIndex = -1; }
    #endregion
}