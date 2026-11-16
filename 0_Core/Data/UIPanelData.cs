using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面板打开时传递的数据
/// </summary>
public class UIPanelData
{
    /// <summary>
    /// 自定义数据（可以是任何类型）
    /// </summary>
    public object CustomData { get; set; }

    public Action OnUIOpen;
    /// <summary>
    /// 是否在打开时清空栈（用于跳转到新页面，不可返回）
    /// </summary>
    public bool ClearStack { get; set; } = false;

    public UIPanelData() { }

    public UIPanelData(object customData)
    {
        CustomData = customData;
    }

    /// <summary>
    /// 泛型获取自定义数据
    /// </summary>
    public T GetCustomData<T>() where T : class
    {
        return CustomData as T;
    }
}
