using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// IEvent.cs - 事件标记接口
// 所有事件的根接口，提供事件的基本元数据
public interface IEvent
{
    /// <summary>
    /// 事件唯一标识符，用于去重、追踪和调试
    /// 通常使用GUID生成，确保全局唯一性
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// 事件发布的UTC时间戳
    /// 用于事件排序、延迟处理和性能分析
    /// </summary>
    //DateTime Timestamp { get; }

    /// <summary>
    /// 事件发布者（源对象）
    /// 用于追踪事件来源，可为null
    /// 注意：避免循环引用，大型对象应谨慎使用
    /// </summary>
    //object Source { get; set; }
}
