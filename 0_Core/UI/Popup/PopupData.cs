using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>弹窗数据：所有弹窗共用，按需填字段。代替 SendMessage 传参。</summary>
public class PopupData : UIPanelData
{
    /// <summary>标题。</summary>
    public string title;

    /// <summary>正文。</summary>
    public string message;

    /// <summary>确认回调。</summary>
    public Action onConfirm;

    /// <summary>取消回调（选择弹窗用）。</summary>
    public Action onCancel;

    /// <summary>物品列表（物品弹窗用）。</summary>
    public List<ItemStackData> items;
}
