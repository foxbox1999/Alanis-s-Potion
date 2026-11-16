using System;
using System.Collections.Generic;

/// <summary>存档系统全局常量。</summary>
public static class SaveConstants
{
    /// <summary>存档文件整体格式版本号。发生破坏性结构变更时 +1。</summary>
    public const int FORMAT_VERSION = 1;

    /// <summary>手动存档槽位数量。</summary>
    public const int MAX_SLOTS = 3;

    /// <summary>自动存档槽位（独立于手动槽位，用负索引避免冲突）。</summary>
    public const int AUTOSAVE_SLOT = -1;
}

/// <summary>存档根对象（即写入 slot_N.dat 的内容）。</summary>
[Serializable]
public class SaveData
{
    /// <summary>整体格式版本号，等于 <see cref="SaveConstants.FORMAT_VERSION"/>。</summary>
    public int formatVersion = SaveConstants.FORMAT_VERSION;

    /// <summary>存档头（加载界面快速展示用，无需反序列化各模块）。</summary>
    public SaveHeader header = new SaveHeader();

    /// <summary>各业务模块的存档片段列表（可扩展）。</summary>
    public List<SaveModuleEntry> modules = new List<SaveModuleEntry>();
}

/// <summary>存档头：记录存档元信息，供加载界面展示日期/场景/版本。</summary>
[Serializable]
public class SaveHeader
{
    /// <summary>槽位索引。</summary>
    public int slotIndex;

    /// <summary>可选：玩家自定义的存档名称。</summary>
    public string saveName;

    /// <summary>现实保存时间（ISO8601 字符串，避免 long/double 精度问题）。</summary>
    public string realUtcTimestamp;

    /// <summary>游戏内总秒数（用于显示游戏日期）。</summary>
    public float gameTotalSeconds;

    /// <summary>游戏内累计天数（由总秒数推算，供展示）。</summary>
    public int gameDayIndex;

    /// <summary>保存时所在场景名。</summary>
    public string sceneName;

    /// <summary>剧情进度 PlotStep。</summary>
    public int plotStep;

    /// <summary>最后所在区域（LocationId 枚举值）。</summary>
    public int locationId;

    /// <summary>最后会话状态（0=HUD/可移动 1=主菜单）。</summary>
    public int sessionState;
    /// <summary>游戏版本号（用于迁移提示）。</summary>
    public string gameVersion;

    public override string ToString()
    {
        return base.ToString() + 
            $" [Slot: {slotIndex}, Save Name: {saveName}],Scene: {sceneName}";
    }
}

/// <summary>单个模块的存档片段。payload 是该模块自身 DTO（数据传输对象） 的 JSON 字符串。</summary>
[Serializable]
public class SaveModuleEntry
{
    /// <summary>模块键，对应 <see cref="ISaveable.SaveKey"/>。</summary>
    public string key;

    /// <summary>模块存档版本号，对应 <see cref="ISaveable.SaveVersion"/>。</summary>
    public int version;

    /// <summary>模块自身 DTO 的 JSON 字符串（由模块的 SerializeState 生成）。</summary>
    public string payload;
}