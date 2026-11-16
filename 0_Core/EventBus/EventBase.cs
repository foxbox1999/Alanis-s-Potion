using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



// 提供事件公共功能的抽象基类，建议自定义事件继承此类
public abstract class EventBase : IEvent
{
    /**
     * 作用：预生成事件的唯一标识符和时间戳，确保每个事件实例都有独特的ID和创建时间。
     * 
     * 访问修饰符：protected，允许子类访问和继承。
     * 
     * 访问方式：通过属性公开，只读访问EventId和Timestamp，Source属性可读写。
     * 
     * 
     * 
     * 
     */

    // 自动生成的唯一标识符
    public string EventId { get; }

    // 事件创建时的UTC时间
    public DateTime Timestamp { get; }

    // 事件源对象
    //public object Source { get; set; }

    /// <summary>
    /// 默认构造函数，自动初始化ID和时间戳
    /// </summary>
    protected EventBase()
    {
        // 使用GUID确保全局唯一性
        EventId = Guid.NewGuid().ToString();
        // 使用UTC时间避免时区问题
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// 指定事件ID的构造函数
    /// 用于需要自定义ID的场景（如网络同步）
    /// </summary>
    /// <param name="eventId">自定义事件ID，如为null或空则使用GUID</param>
    protected EventBase(string eventId) : this()  // 先调用默认构造函数
    {
        // 验证并设置自定义ID
        if (!string.IsNullOrEmpty(eventId))
            EventId = eventId;
        // 如果eventId无效，保持默认构造函数生成的GUID
    }
}
