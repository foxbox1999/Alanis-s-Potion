using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueRouter
{
    /// <summary>返回该 NPC 当前该播的对话 ID。</summary>
    public static int GetStartId(string npcId, int defaultId)
    {
        // 以后按上下文覆盖，例如：
        // if (npcId == "Viggo" && WorldManager.Instance.RentDaysLeft <= 0)
        //     return 收租对话ID;   // 收租日
        return defaultId;   // 平时
    }
}