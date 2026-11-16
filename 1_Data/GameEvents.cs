using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#region 对话
/// <summary>请求开始一段对话。</summary>
public class RequestDialogue : EventBase
{
    public int StartID;
    public List<UnityAction> actions;
    public Sprite BackGround;
    public UnityAction OnDialogueComplete;
    public bool NeedBlackFade = false;   // 【新增】本次对话是否需要黑屏转场
}

/// <summary>请求 UIManager 打开对话 UI。</summary>
public class DialogueUIOpenRequestEvent : EventBase
{
    public int StartID;
    public bool NeedBlackFade = false;
}

/// <summary>对话 UI 就绪（携带 View）。</summary>
public class DialogueUIReadyEvent : EventBase
{
    public DialogueView View;
}

/// <summary>对话结束。</summary>
public class DialogueEndedEvent : EventBase { }

/// <summary>请求跳过/结束当前对话。</summary>
public class RequestEndDialogue : EventBase { }


#endregion

#region 游戏流程
/// <summary>
/// 结束捏脸 1.PlayerDataManager更新数据
///         2.更新PlayerDataManaager里面的捏脸形象
///         3.判断当前剧情在那一步，如果是plot是1更新到2并且调用plotDirector.Advance
/// </summary>
public class OnAvatarCustomizationOver : EventBase
{
    public Dictionary<string, string> _parts;
}
#endregion
