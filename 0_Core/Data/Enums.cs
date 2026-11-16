    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UILayer
{
    ScreenUI,   // 屏幕空间-覆盖（主菜单、背包、炼药、采集等所有功能面板）
    TopLayer    // 顶层（弹窗、对话、全屏特效，必须盖住一切）
}
public enum SubState { FreeRoam, FullUI, Dialog ,Exit}

/// <summary>物品大类（背包分类展示 + 送礼限制）。</summary>
public enum ItemCategory { Herb = 0, Potion = 1, Dessert = 2, Other = 3 }

/// <summary>阵营（怀疑度归属）。</summary>
public enum FactionType { Townsfolk = 0, Church = 1, Witch = 2, Beast = 3 }

/// <summary>病人状态  等待治疗 治疗了 已经延期 已经离去</summary>
public enum PatientStatus { Waiting = 0, Treated = 1, Deferred = 2, Gone = 3 }

/// <summary>养子任务类型：上课 / 野外探索。</summary>
public enum ChildTaskType { Course = 0, Expedition = 1 }

/// <summary>采集区域：嘶嘶森林 / 斯拉姆荒野 / 珍稀丘陵灌丛 / 佩特兰山。</summary>
public enum GatherRegion { SisiForest = 0, SlamWilderness = 1, HillThicket = 2, PetranMountain = 3 }

/// <summary>炼药任务状态。</summary>
public enum AlchemyJobState { Running = 0, Ready = 1, Claimed = 2 }

/// <summary>
/// 背包打开上下文
/// </summary>
public enum InventoryContext
{
    View,      // 普通查看
    Select,    // 选择物品（送礼、任务等）
    Sell,      // 出售
}

/// <summary>弹窗类型。</summary>
public enum PopupType
{
    /// <summary>通知弹窗（单确认，普通样式）。</summary>
    Info = 0,
    /// <summary>选择弹窗（确认 + 取消）。</summary>
    Confirm = 1,
    /// <summary>物品获得弹窗（单确认，展示物品）。</summary>
    Item = 2,
    /// <summary>侧边消息（非阻塞，右上角滑入）。</summary>
    SidePrompt = 3
}

/// <summary>症状的呈现方式。</summary>
public enum SymptomDisplayType
{
    Verbal,   // 口述：病人说出来的
    Visual    // 可见：医生能直接看到的
}

/// <summary>养子属性类型。</summary>
public enum ChildStatType
{
    Magic,          // 魔法
    Stamina,        // 耐力
    CommonSense,    // 常识
    Alchemy         // 魔药
}

/// <summary>
/// 一天之中的时间
/// </summary>
public enum TimeOfDay { Day, Dusk, Night , None }

/// <summary>
/// 提示框目标方向
/// </summary>
public enum GuideArrowDir { Up, DownL,DownR,Left, Right }

/// <summary>看病治疗结果。</summary>
public enum TreatResult
{
    Error,
    Success,        // 治好了，给奖励
    NoMedicine,     // 没药，无奖惩，病人明天再来
    WrongMedicine   // 用错药，惩罚
}

public enum ItemVisual { Default, Matched, Selected }

/// <summary>商店类型：甜品店 / 草药店。</summary>
public enum ShopType { Dessert = 0, Herb = 1 }

public enum SessionState { FreeRoam = 0, MainMenu = 1 }