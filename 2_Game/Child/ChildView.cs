using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 养子界面 View：只负责展示与接收点击，业务逻辑走 ChildManager / DatabaseManager。
/// 流程：选时间段(上午/中午/下午) → 给该时段选任务(课程/采集) → 三个时段排满 → 确认提交。
/// 字段名沿用旧预制体绑定（Potion=魔药Alchemy、Endurance=耐力Stamina、CommonKnowledge=常识CommonSense），避免重绑。
/// </summary>
public class ChildView : BaseUIView
{
    #region 引用
    [Header("养子信息")]
    [SerializeField] private TextMeshProUGUI ChildName;         // 养子名字（暂未入库，保持预制体静态文案）
    [SerializeField] private Image ChildPortrait;               // 养子立绘
    [SerializeField] private ChildsPortrait[] Portraits;        // 各课程立绘
    [SerializeField] private Sprite normalP;                    // 默认立绘

    [Header("属性")]
    [SerializeField] private TextMeshProUGUI Magic;             // 魔法
    [SerializeField] private TextMeshProUGUI Potion;            // 魔药(Alchemy)
    [SerializeField] private TextMeshProUGUI CommonKnowledge;   // 常识(CommonSense)
    [SerializeField] private TextMeshProUGUI Endurance;         // 耐力(Stamina)
    [SerializeField] private TextMeshProUGUI AddMagic;
    [SerializeField] private TextMeshProUGUI AddPotion;
    [SerializeField] private TextMeshProUGUI AddCommonKnowledge;
    [SerializeField] private TextMeshProUGUI AddEndurance;

    [Header("时间段")]
    [SerializeField] private Button Morning;
    [SerializeField] private Button Noon;
    [SerializeField] private Button Afternoon;
    [SerializeField] private Image MorningPanel;
    [SerializeField] private Image NoonPanel;
    [SerializeField] private Image AfternoonPanel;

    [Header("结果展示")]
    [SerializeField] private TextMeshProUGUI ObtainedInfo;      // 获得内容提示
    [SerializeField] private Image CurrentImage;                // 任务图标

    [Header("任务按钮")]
    [SerializeField] private Button Course_0;
    [SerializeField] private Button Course_1;
    [SerializeField] private Button Course_2;
    [SerializeField] private Button Course_3;
    [SerializeField] private Button Gather_0;
    [SerializeField] private Button Gather_1;
    [SerializeField] private Button Gather_2;
    [SerializeField] private Button Gather_3;

    [Header("确认")]
    [SerializeField] private Button ConfirmButton;
    //[SerializeField] private Button BackButton;
    #endregion

    #region 配置
    [Serializable]
    public class ChildsPortrait
    {
        public Sprite Portrait;
        public string course;   // 旧键：Magic / Potion / CommonKnowledge / Endurance
    }

    private const int SlotCount = 3;                            // 上午/中午/下午
    private static readonly string[] SlotNames = { "上午", "中午", "下午" };
    #endregion

    #region 运行时（内存优化：固定数组复用，避免每次打开 new Dictionary）
    private readonly string[] _selectedIds = new string[SlotCount];   // 每时段选中任务 id（null=未选）
    private readonly Image[] _timePanels = new Image[SlotCount];      // 高亮面板
    private readonly Button[] _missionButtons = new Button[8];        // 4 课程 + 4 采集
    private int _currentSlot;                                         // 当前时段 0/1/2
    private bool _isPlanned;                                          // 今天是否已安排
    #endregion

    #region 生命周期
    protected override void OnOpen()
    {
        base.OnOpen();
        BuildSlotArrays();
        BindButtons();
        RefreshStats();
        LoadTodayPlan();
        SelectSlot(0);
    }

    protected override void OnRefresh()
    {
        base.OnRefresh();
        RefreshStats();   // 属性可能在每日结算后变化
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 建立基础数据关联
    /// </summary>
    private void BuildSlotArrays()
    {
        _timePanels[0] = MorningPanel;
        _timePanels[1] = NoonPanel;
        _timePanels[2] = AfternoonPanel;

        _missionButtons[0] = Course_0; _missionButtons[1] = Course_1;
        _missionButtons[2] = Course_2; _missionButtons[3] = Course_3;
        _missionButtons[4] = Gather_0; _missionButtons[5] = Gather_1;
        _missionButtons[6] = Gather_2; _missionButtons[7] = Gather_3;
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtons()
    {
        Morning.onClick.RemoveAllListeners();
        Morning.onClick.AddListener(() => SelectSlot(0));
        Noon.onClick.RemoveAllListeners();
        Noon.onClick.AddListener(() => SelectSlot(1));
        Afternoon.onClick.RemoveAllListeners();
        Afternoon.onClick.AddListener(() => SelectSlot(2));

        // 课程按钮 → 课程任务（id 与 Missions.asset 一致）
        BindMission(Course_0, "Course_Magic");
        BindMission(Course_1, "Course_Potion");
        BindMission(Course_2, "Course_CommonKnowledge");
        BindMission(Course_3, "Course_Endurance");
        // 采集按钮 → 采集任务
        BindMission(Gather_0, "Gather_forest");
        BindMission(Gather_1, "Gather_brushwood");
        BindMission(Gather_2, "Gather_wilds");
        BindMission(Gather_3, "Gather_Mountain");

        ConfirmButton.onClick.RemoveAllListeners();
        ConfirmButton.onClick.AddListener(OnConfirm);
        //BackButton.onClick.RemoveAllListeners();
        //BackButton.onClick.AddListener(CloseSelf);
    }
    /// <summary>
    /// 建立mission按钮和mission之间的关联
    /// </summary>
    /// <param name="button"></param>
    /// <param name="missionId"></param>
    private void BindMission(Button button, string missionId)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnMissionClicked(missionId));
    }

    /// <summary>读当天已安排计划：有则回显并锁定，没有则清空待选。</summary>
    private void LoadTodayPlan()
    {
        var plan = ChildManager.Instance.TodayPlan;
        _isPlanned = plan != null && plan.Count > 0;

        Array.Clear(_selectedIds, 0, _selectedIds.Length);
        if (_isPlanned)
            for (int i = 0; i < SlotCount && i < plan.Count; i++)
                _selectedIds[i] = plan[i];

        ConfirmButton.interactable = !_isPlanned;
        foreach (var b in _missionButtons)
            if (b != null) b.interactable = !_isPlanned;
    }
    #endregion

    #region 时间段
    private void SelectSlot(int slot)
    {
        _currentSlot = slot;
        //SetAsLastSibling将该transform置为父对象的第一个子对象
        _timePanels[slot].transform.SetAsLastSibling();  // 高亮面板置顶
        RefreshCurrentSlot();
    }
    /// <summary>
    /// 刷新时间
    /// </summary>
    private void RefreshCurrentSlot()
    {
        UpdateObtainedInfo();
        UpdatePropertyPreview();
    }
    #endregion

    #region 任务选择
    private void OnMissionClicked(string missionId)
    {
        if (_isPlanned) return;   // 已安排则锁定
        _selectedIds[_currentSlot] = missionId;
        RefreshCurrentSlot();
    }

    /// <summary>结果展示：课程显示 +N；采集显示该地区可采到的具体草药。</summary>
    private void UpdateObtainedInfo()
    {
        Mission m = CurrentMission();
        if (m == null)
        {
            ObtainedInfo.text = "请选择一个任务";
            CurrentImage.enabled = false;
            return;
        }

        if (m.type == ChildTaskType.Course)
        {
            ObtainedInfo.text = $"{m.name}  {StatName(m.stat)} +{m.statGain}";
        }
        else
        {
            // 采集：展示地区 + 可采集到的草药列表（对应旧 PreviewMission 的 GetHerbsByZone）
            string herbs = HerbNamesOfZone(m.zoneId);
            ObtainedInfo.text = string.IsNullOrEmpty(herbs)
                ? $"{m.name}（探索）"
                : $"{m.name}（探索）\n {herbs}";
        }

        CurrentImage.enabled = m.icon != null;
        CurrentImage.sprite = m.icon;
    }

    /// <summary>把某地区可采集草药名拼成「A、B、C…」。</summary>
    private string HerbNamesOfZone(string zoneId)
    {
        var herbs = DatabaseManager.Instance.GetHerbsByZone(zoneId);
        if (herbs == null || herbs.Count == 0) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < herbs.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append(herbs[i].name);
        }
        return sb.ToString();
    }

    /// <summary>属性预览：选课程时在对应属性后显示 +N，并切换立绘。</summary>
    private void UpdatePropertyPreview()
    {
        ClearAddTexts();

        Mission m = CurrentMission();
        if (m == null) { UpdateChildPortrait(null); return; }

        if (m.type == ChildTaskType.Course)
        {
            var add = AddTextOf(m.stat);
            if (add != null) add.text = $"+{m.statGain}";
            UpdateChildPortrait(m.stat);
        }
        else
        {
            UpdateChildPortrait(null);
        }
    }

    private Mission CurrentMission()
    {
        string id = _selectedIds[_currentSlot];
        return string.IsNullOrEmpty(id) ? null : DatabaseManager.Instance.GetMission(id);
    }

    private void ClearAddTexts()
    {
        AddMagic.text = "";
        AddPotion.text = "";
        AddCommonKnowledge.text = "";
        AddEndurance.text = "";
    }
    #endregion

    #region 属性显示
    private void RefreshStats()
    {
        Magic.text = ChildManager.Instance.GetStat(ChildStatType.Magic).ToString();
        Potion.text = ChildManager.Instance.GetStat(ChildStatType.Alchemy).ToString();
        CommonKnowledge.text = ChildManager.Instance.GetStat(ChildStatType.CommonSense).ToString();
        Endurance.text = ChildManager.Instance.GetStat(ChildStatType.Stamina).ToString();
    }

    /// <summary>
    /// 更新养子的属性
    /// </summary>
    /// <param name="stat"></param>
    private void UpdateChildPortrait(ChildStatType? stat)
    {
        if (stat.HasValue)
        {
            string key = CourseKeyOf(stat.Value);
            foreach (var p in Portraits)
            {
                if (p.course == key) { ChildPortrait.sprite = p.Portrait; return; }
            }
        }
        ChildPortrait.sprite = normalP;
    }

    private TextMeshProUGUI AddTextOf(ChildStatType t) => t switch
    {
        ChildStatType.Magic => AddMagic,
        ChildStatType.Alchemy => AddPotion,
        ChildStatType.CommonSense => AddCommonKnowledge,
        ChildStatType.Stamina => AddEndurance,
        _ => null
    };

    private static string StatName(ChildStatType t) => t switch
    {
        ChildStatType.Magic => "魔法",
        ChildStatType.Alchemy => "魔药",
        ChildStatType.CommonSense => "常识",
        ChildStatType.Stamina => "耐力",
        _ => ""
    };

    private static string CourseKeyOf(ChildStatType t) => t switch
    {
        ChildStatType.Magic => "Magic",
        ChildStatType.Alchemy => "Potion",
        ChildStatType.CommonSense => "CommonKnowledge",
        ChildStatType.Stamina => "Endurance",
        _ => ""
    };
    #endregion

    #region 确认
    private void OnConfirm()
    {
        if (_isPlanned) { PopupManager.Instance.ShowSidePrompt("今天已经安排过了"); return; }

        for (int i = 0; i < SlotCount; i++)
        {
            if (string.IsNullOrEmpty(_selectedIds[i]))
            {
                PopupManager.Instance.ShowSidePrompt("请将所有的时间段都安排好");
                return;
            }
        }

        PopupManager.Instance.ShowConfirm("确认安排", BuildPlanMessage(), DoAssign);
    }

    private string BuildPlanMessage()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < SlotCount; i++)
        {
            var m = DatabaseManager.Instance.GetMission(_selectedIds[i]);
            sb.Append(SlotNames[i]).Append("：").Append(m != null ? m.name : "").Append('\n');
        }
        return sb.ToString();
    }

    private void DoAssign()
    {
        ChildManager.Instance.AssignDailyTasks(new List<string>(_selectedIds));
        _ = UIManager.Instance.SwitchPanelAsync(
    openKey: UIID._mainmenu,
    closeKey: UIID._raising,
    duringBlack: null,
    fade: true
);
    }
    #endregion
}