using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 弹窗管理器（薄门面）：集中「弹窗类型 → 面板 key + 数据组装」。
/// 只调用 UIManager 打开面板，不自己管理 UI 生命周期（复用 UIManager 的栈/池/分层）。
/// </summary>
public class PopupManager : MonoSingleton<PopupManager>
{
    /// <summary>通知弹窗（单确认）。</summary>
    public void ShowInfo(string title, string message, Action onConfirm = null)
        => Open(UIID._infoPopup, new PopupData { title = title, message = message, onConfirm = onConfirm });

    /// <summary>选择弹窗（确认 + 取消）。</summary>
    public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null)
        => Open(UIID._confirmPopup, new PopupData { title = title, message = message, onConfirm = onConfirm, onCancel = onCancel });

    /// <summary>物品获得弹窗（单确认，展示物品）。</summary>
    public void ShowItem(List<ItemStackData> items, Action onConfirm = null)
        => Open(UIID._itemPopup, new PopupData { items = items, onConfirm = onConfirm });

    /// <summary>侧边消息（非阻塞，右上角滑入）。</summary>
    public void ShowSidePrompt(string message)
        => SidePromptManager.Instance.Show(message);

    /// <summary>统一打开：用强类型数据打开面板。</summary>
    private void Open(string key, PopupData data)
        => UIManager.Instance.OpenPanel(key, data);
}