using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;


// IEventHandler.cs - 事件处理器接口
// 用于创建可复用、可测试的事件处理器类
public interface IEventHandler<TEvent> where TEvent : class, IEvent
{
    /// <summary>
    /// 处理器执行优先级 (数值越小优先级越高)
    /// -1000 (系统级) ~ 1000 (业务级)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 处理器启用状态 (临时禁用而不取消订阅)
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 异步处理事件的核心方法
    /// 所有事件处理逻辑应在此实现
    /// </summary>··
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步处理任务</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
