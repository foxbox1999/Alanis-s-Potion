using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

#region SystemEvents
public class GameStateChangedEvent : EventBase
{
    public IGameState NewState;
}

public class SubStateChangedEvent : EventBase
{
    public SubState SubState;
}
#endregion

#region SaveEvents
/// <summary>存档成功事件。</summary>
public class SaveCompletedEvent : EventBase
{
    /// <summary>槽位索引。</summary>
    public int SlotIndex { get; }
    public SaveCompletedEvent(int slotIndex) { SlotIndex = slotIndex; }
}

/// <summary>存档失败事件。</summary>
public class SaveFailedEvent : EventBase
{
    /// <summary>槽位索引。</summary>
    public int SlotIndex { get; }

    /// <summary>失败原因。</summary>
    public string Reason { get; }
    public SaveFailedEvent(int slotIndex, string reason) { SlotIndex = slotIndex; Reason = reason; }
}

/// <summary>读档结果事件（SlotIndex 为 -1 表示“开始新游戏”）。</summary>
public class SaveLoadedEvent : EventBase
{
    /// <summary>槽位索引；-1 表示新游戏。</summary>
    public int SlotIndex { get; }

    /// <summary>是否成功。</summary>
    public bool Success { get; }

    /// <summary>失败原因（成功时为 null）。</summary>
    public string Error { get; }
    public SaveLoadedEvent(int slotIndex, bool success, string error)
    { SlotIndex = slotIndex; Success = success; Error = error; }
}

/// <summary>存档被删除事件。</summary>
public class SaveDeletedEvent : EventBase
{
    /// <summary>槽位索引。</summary>
    public int SlotIndex { get; }
    public SaveDeletedEvent(int slotIndex) { SlotIndex = slotIndex; }
}

/// <summary>开始新游戏事件（所有模块已重置）。</summary>
public class NewGameStartedEvent : EventBase { }
#endregion

#region Assets
/// <summary>
/// 资源加载完成事件
/// </summary>
public class AssetLoadedEvent : EventBase
{
    public string Key { get; }
    public UnityEngine.Object Asset { get; }
    public AssetLoadedEvent(string key, UnityEngine.Object asset)
    {
        Key = key;
        Asset = asset;
    }
}
#endregion

#region Settings
/// <summary>
/// 设置更改事件
/// </summary>
public class SettingsChangedEvent : EventBase
{
    public Settings NewSettings { get; }
    public SettingsChangedEvent(Settings newSettings)
    {
        NewSettings = newSettings;
    }
}

#endregion

#region UI
/// <summary>
/// 面板打开事件
/// </summary>
public class UIPanelOpenedEvent : EventBase
{
    public string Key { get; }
    public BaseUIView View { get; }

    public UIPanelOpenedEvent(string key, BaseUIView view)
    {
        Key = key;
        View = view;
    }
}

/// <summary>
/// 面板关闭事件
/// </summary>
public class UIPanelClosedEvent : EventBase
{
    public string Key { get; }
    public BaseUIView View { get; }

    public UIPanelClosedEvent(string key, BaseUIView view)
    {
        Key = key;
        View = view;
    }
}

/// <summary>
/// 请求打开面板事件（用于解耦）
/// </summary>
public class UIOpenRequestEvent : EventBase
{
    public string Key { get; }
    public UIPanelData Data { get; }

    public UIOpenRequestEvent(string key, UIPanelData data = null)
    {
        Key = key;
        Data = data;
    }
}

/// <summary>
/// 请求关闭面板事件（用于解耦）
/// </summary>
public class UICloseRequestEvent : EventBase
{
    public string Key { get; }

    public UICloseRequestEvent(string key)
    {
        Key = key;
    }
}

/// <summary>
/// 场景加载完成事件
/// </summary>
public class SceneLoadedEvent : EventBase
{
    public string SceneName { get; }
    public SceneLoadedEvent(string sceneName)
    {
        SceneName = sceneName;
    }
}
#endregion

#region 时间控制
public class TimeOfDayChangedEvent : EventBase
{
    public TimeOfDay TimeOfDay = TimeOfDay.None;
}
public class NewDayEvent : EventBase
{
    public int DayIndex;
}
#endregion