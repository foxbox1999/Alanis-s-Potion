using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


// IEventBus.cs - 事件总线接口
// 定义事件总线的核心操作契约
public interface IEventBus : IDisposable
{
    /// <summary>
    /// 订阅指定类型的事件（简化版）
    /// 使用Action委托作为事件处理器
    /// 返回IDisposable订阅令牌，用于取消订阅
    /// </summary>
    /// <typeparam name="TEvent">事件类型，必须实现IEvent接口</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>订阅令牌，Dispose()时自动取消订阅</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class, IEvent;

    /// <summary>
    /// 订阅事件（完整版，支持优先级和过滤）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <param name="priority">
    /// 处理器优先级（0=默认，负数=更高优先级，正数=更低优先级）
    /// 相同优先级按添加顺序执行
    /// </param>
    /// <param name="filter">
    /// 事件过滤器，返回true时处理器才会被执行
    /// 可用于基于事件内容的条件执行
    /// </param>
    /// <returns>订阅令牌</returns>
    IDisposable Subscribe<TEvent>(
        Action<TEvent> handler,
        int priority = 0,          
        Func<TEvent, bool> filter = null)  //Func<TEvent, bool> 类型的过滤器,属于方法委托
        where TEvent : class, IEvent;

    /// <summary>
    /// 使用独立处理器对象订阅事件
    /// 适用于需要依赖注入或复杂状态管理的处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">实现了IEventHandler接口的处理器对象</param>
    /// <returns>订阅令牌</returns>
    IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : class, IEvent;

    /// <summary>
    /// 发布事件（同步阻塞方式）
    /// 在当前线程立即执行所有匹配的处理器
    /// 注意：处理器异常会被捕获并记录，不会中断其他处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    void Publish<TEvent>(TEvent @event)
        where TEvent : class, IEvent;

    /// <summary>
    /// 发布事件（异步非阻塞方式）
    /// 在后台线程执行处理器，避免阻塞主线程
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步操作的Task</returns>
    Task PublishAsync<TEvent>(TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : class, IEvent;


    /// <summary>
    /// 批量发布多个事件（异步）
    /// 性能优化：减少锁竞争和上下文切换
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="events">事件集合</param>
    /// <returns>表示异步操作的Task</returns>
    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events)
        where TEvent : class, IEvent;

    /// <summary>
    /// 取消所有事件订阅
    /// 通常在场景切换或游戏关闭时调用
    /// 原Clear()方法已弃用，改为UnsubscribeAll() 易于理解
    /// </summary>
    void UnsubscribeAll();

    //void UnSubscribeEvent<TEvent>(IEventHandler<TEvent> handler) where TEvent : class, IEvent;
    /// <summary>
    /// 获取事件总线的运行统计信息
    /// 用于监控、调试和性能分析
    /// </summary>
    /// <returns>包含统计数据的对象</returns>
    //EventBusStatistics GetStatistics();
}
