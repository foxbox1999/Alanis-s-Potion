using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
/// <summary>
/// 事件总线的核心实现类
/// 线程安全、高性能、支持优先级和过滤
/// 设计模式：观察者模式 + 中介者模式
/// 通过sealed关键字防止继承，确保唯一实现
/// </summary>
public sealed class EventBus : IEventBus
{
    /** EventBus 事件总线
     * EventBus.cs 事件总线实现
     * 
     * 作用：
     *      1. 实现事件的发布与订阅机制，促进组件间的解耦。 √
     *      2. 支持同步和异步事件处理，提升系统响应能力。 √
     *      3. 提供事件优先级管理，确保关键事件优先处理。 √
     *      4. 自动管理订阅生命周期，防止内存泄漏。 √
     *      5. 易于扩展和维护，适应复杂项目需求。 √
     *      6. 提高代码的可读性和可维护性。 √
     *      7. 促进模块化设计，增强系统的灵活性。 √
     *      8. 支持多种事件类型，满足不同业务需求。 
     *      9. 提供清晰的事件流，便于调试和监控。
     *      10. 增强团队协作，通过统一的事件机制减少冲突。
     *      11. 提升整体系统性能，优化资源利用。
     *      12. 支持跨线程事件处理，提升并发能力。
     *      13. 提供详细的日志记录，便于问题追踪。
     *      14. 促进代码复用，减少重复开发工作。
     *      15.支持事件过滤，提升事件处理效率。
     *      16. 提供事件调度功能，优化事件处理顺序。
     *      17. 支持事件链式调用，增强事件处理能力。
     *      18. 提供事件统计功能，便于性能分析。
     *      19. 支持事件优先级动态调整，提升系统灵活性。
     *      20. 提供事件回调机制，增强事件处理的互动性。
     *      21. 支持事件批量处理，提升系统吞吐量。
     *      22. 提供事件持久化功能，确保关键事件不丢失。
     *      23. 支持事件重试机制，提升系统可靠性。
     *      24. 提供事件监控功能，实时掌握系统状态。
     *      25. 支持事件分发策略，优化事件处理流程。
     *      26. 提供事件优先级队列，提升事件处理效率。
     *      27. 支持事件订阅过滤，减少不必要的事件处理。
     *      28. 提供事件处理超时机制，防止系统阻塞。
     *      29. 支持事件处理结果反馈，提升系统互动性。
     *  
     *  用法：
     *      1. 订阅事件：使用Subscribe方法订阅特定类型的事件，并提供处理函数。 √
     *      2. 取消订阅：使用Unsubscribe方法取消对特定事件的订阅。  √
     *      3. 发布事件：使用Publish方法发布事件，触发所有订阅该事件的处理函数。 √
     *      4. 异步发布：使用PublishAsync方法异步发布事件，适用于需要非阻塞处理的场景。 √
     *      5. 优先级订阅：在订阅事件时指定优先级，确保关键事件优先处理。 √
     *      6. 生命周期管理：通过返回的订阅令牌自动管理订阅的生命周期，防止内存泄漏。 √
     *      7. 清空订阅：使用Clear方法清除所有事件订阅，适用于重置或销毁场景。 √ 
     *      8. 多线程支持：在多线程环境中安全地发布和订阅事件，确保数据一致性。 √
     *      9. 事件过滤：在订阅时添加过滤条件，只处理符合条件的事件。  √
     *      10. 事件链式调用：支持多个事件处理函数按顺序调用，增强事件处理能力。
     *      11. 事件调度：根据事件优先级和处理时间调度事件处理顺序，优化系统性能。
     *      12. 事件统计：记录事件处理次数和时间，便于性能分析和优化。
     *      13. 事件回调：在事件处理完成后执行回调函数，增强系统互动性。
     *      14. 批量处理：支持一次发布多个事件，提升系统吞吐量。
     *      15. 持久化事件：将关键事件保存到存储中，确保系统重启后事件不丢失。
     *      16. 重试机制：对于处理失败的事件，支持自动重试，提升系统可靠性。
     *      17. 监控功能：实时监控事件处理状态，及时发现和解决问题。
     *      18. 分发策略：根据不同的业务需求选择合适的事件分发策略，优化处理流程。
     *      19. 优先级队列：使用优先级队列管理事件处理顺序，提升处理效率。 √
     *      20. 订阅过滤：在订阅时指定过滤条件，减少不必要的事件处理开销。
     *      21. 处理超时：为事件处理设置超时时间，防止系统长时间阻塞。
     *      22. 结果反馈：在事件处理完成后返回结果，提升系统互动性和用户体验。'
     *      23. 日志记录：记录事件发布和处理日志，便于问题追踪和调试。 √
     *      24. 扩展性：通过继承和接口实现，方便扩展事件总线功能，满足复杂项目需求。
     *      25. 模块化设计：将事件总线作为独立模块，促进系统模块化设计和团队协作。
     *      26. 性能优化：通过合理设计事件处理流程，提升整体系统性能和资源利用率。
     *      
     *  
     *  接口：
     *  
     *      订阅接口（核心功能）:
     *      IDisposable Subscribe<TEvent>(Action<TEvent> handler) 简单订阅	默认优先级，无过滤	快速实现简单事件处理
     *      IDisposable Subscribe<TEvent>(Action<TEvent> handler, int priority, Func<TEvent, bool> filter)  带优先级和过滤的订阅	高级订阅，精准控制	复杂业务逻辑，需要条件处理
     *      IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler) 对象处理器订阅	适合依赖注入，状态管理	复杂处理器，需要状态保存
     *      
     *      发布接口（核心功能）:
     *      void Publish<TEvent>(TEvent @event) 	同步发布	阻塞主线程，简单快速	简单事件，不影响性能
     *      Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 异步发布	非阻塞，适合复杂处理	需要等待处理结果的场景
     *      Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events) 	批量发布	优化性能，减少锁竞争	需要发布多个事件的场景
     *       
     *      取消订阅接口
     *      void UnSubscribeEvent<TEvent>(IEventHandler<TEvent> handler) 取消特定处理器订阅	需要精确匹配处理器	需要取消特定处理器
     *      void UnsubscribeAll() 	取消所有订阅	清理所有事件订阅,场景切换、游戏关闭时
     *      void Dispose() 	释放事件总线资源	自动调用 自动调用UnsubscribeAll()	对象生命周期结束时
     *      
     *      辅助接口
     *      void OpenEventBus()	重新启用已关闭的事件总线	用于动态启用/禁用事件总线
     *  使用方法总结：
     *  
     *      1.定义事件类 里面一般用于放置需要传递的信息
     *      
     *      2.1 简单订阅事件
     *          public class OtherClass{
     *              配置订阅事件全局访问入口（重要！
     *              
     *              public void SubMethod(){
     *                  var subscription = EventBus.Instance.Subscribe<事件类名>(处理方法名);
     *                  //带条件过滤的订阅
     *                  var sub2 = .Subscribe<事件类名>(
     *                      handler: 处理方法名,
     *                      priority: 优先级,
     *                      filter: e => e.某个属性 >= 4  // 只处理品质4以上的药水
     *                  );
     *              }
     *              |filter: e => e.某个属性 >= 4|  就等同于  |filter: 判断方法名(e)|
     *              public bool 判断方法名(事件类名 e){
     *                  return e.QualityLevel > 4;
     *              }
     *              
     *              public void 处理方法名(事件类名 参数名){
     *                  操作;
     *              }
     *          }
     *          
     *          
     *      2.2 复杂事件订阅 实现IEventHandler
     *          // 1 实现接口
     *          public class AchievementEventHandler : IEventHandler<事件类名>
     *          {
     *              public int Priority => 10;  // 优先级（数字越小越先执行）
     *              public bool IsEnabled { get; set; } = true;
     *          
     *              public async Task HandleAsync(PlayerLevelUpEvent @event, CancellationToken cancellationToken)
     *              {
     *                  await Task.Delay(100, cancellationToken); // 模拟异步操作
     *                  其他操作;
     *                  其他对@event增删读取的操作
     *                  
     *              }
     *          }
     *          // 2 实例化处理器
     *          public class OtherClass{
     *              配置订阅事件全局访问入口（重要！
     *              var achievementHandler = new AchievementEventHandler();
     *              //添加订阅
     *              var subscription = EventBus.Instance.Subscripe(achievementHandler);
     *          }
     *        3 取消订阅入口
     *        public class OtherClass{
     *              1 配置订阅事件全局访问令牌缓存（重要！
     *              private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
     *              1订阅某个事件
     *              
     *              2 清理订阅
     *              public void Dispose(){
     *                  foreach (var subscription in _subscriptions)
     *                  {
     *                      subscription.Dispose();
     *                  }
     *                  _subscriptions.Clear();
     *              }
     *        
     *        
     *        }
     *     3 发布事件
     *          public class ThridClass{
     *          
     *              //真实场景中就是调用下面两个方法来实现触发订阅者里面的方法
     *              //同步发布
     *              public void GetAchievement(){
     *                  1.创建需要触发的事件
     *                  2.发布事件
     *                  2.1 同步发布
     *                      EventBus.Instance.Publish(事件对象名);
     *                  2.2 异步发布
     *                      Await EventBus.Instance.PublishAsync(异步事件对象名);
     *                  
     *              }
     *              //异步发布
     *              public Async Task GetAchievementAsync(){
     *                  1.创建需要触发的事件
     *                  
     *                  2.发布事件
     *                  2.2 异步发布
     *                  Await EventBus.Instance.PublishAsync(异步事件对象名);
     *                  
     *                  2.3批量发布
     *                  var events = new List<事件类名>();
     *                  foreach(var item in otherList){
     *                      events.Add(new 事件类名(相关参数));
     *                  }
     *                  await EventBus.Instance.PublishBatchAsync(events);               
     *              }
     *          }  
     *          
     *      其他用法:
     *      1.取消订阅
     *      2.清空订阅
     *      
     */

    #region 单列模式实现

    //在别的类实例化，将该类设为单例（适配unity）保证全局唯一性

    ////饿汉式实现，线程安全，性能高，程序创建时就实例化，不会延迟加载，适合大部分场景，全局唯一访问点
    private static EventBus _instance;
    public static EventBus Instance => _instance ??= new EventBus();
    #endregion

    #region 知识点复习

    //字典的增删查改方法：

    //添加元素：dictionary.Add(key, value) 或 dictionary[key] = value;
    //删除元素：dictionary.Remove(key);
    //查找元素：dictionary.TryGetValue(key, out value) 或 dictionary.ContainsKey(key);
    //更新元素：dictionary[key] = newValue;
    //遍历元素：foreach (var kvp in dictionary) { var key = kvp.Key; var value = kvp.Value; }
    //清空字典：dictionary.Clear();
    //获取键集合：var keys = dictionary.Keys;
    //获取值集合：var values = dictionary.Values;
    //获取元素数量：var count = dictionary.Count;
    //合并字典：foreach (var kvp in otherDictionary) { dictionary[kvp.Key] = kvp.Value; }
    //创建字典：var dictionary = new Dictionary<TKey, TValue>();
    //创建带初始容量的字典：var dictionary = new Dictionary<TKey, TValue>(initialCapacity);
    //创建带自定义比较器的字典：var dictionary = new Dictionary<TKey, TValue>(comparer);
    //创建带初始容量和自定义比较器的字典：var dictionary = new Dictionary<TKey, TValue>(initialCapacity, comparer);
    //获取默认值：var value = dictionary.GetValueOrDefault(key, defaultValue);
    //尝试添加元素：var added = dictionary.TryAdd(key, value);
    //尝试更新元素：var updated = dictionary.TryUpdate(key, newValue, comparisonValue);
    //尝试移除元素：var removed = dictionary.TryRemove(key, out value);
    //获取或添加元素：var value = dictionary.GetOrAdd(key, valueFactory);
    //获取或更新元素：var value = dictionary.GetOrUpdate(key, addValueFactory, updateValueFactory);

    //list的增删查改方法：

    //添加元素：list.Add(item);
    //删除元素：list.Remove(item) 或 list.RemoveAt(index);
    //查找元素：list.Contains(item) 或 list.IndexOf(item);
    //更新元素：list[index] = newItem;
    //遍历元素：foreach (var item in list) { /* ... */ }
    //清空列表：list.Clear();
    //获取元素数量：var count = list.Count;
    //插入元素：list.Insert(index, item);
    //添加多个元素：list.AddRange(collection);
    //移除多个元素：list.RemoveAll(predicate);
    //转换为数组：var array = list.ToArray();
    //排序列表：list.Sort();
    //反转列表：list.Reverse();
    //查找符合条件的元素：var item = list.Find(predicate);
    //查找所有符合条件的元素：var items = list.FindAll(predicate);
    //获取范围内的元素：var range = list.GetRange(index, count);
    //转换为只读列表：var readOnlyList = list.AsReadOnly();
    //转换为字符串：var str = string.Join(", ", list);
    //二分查找（前提是列表已排序）：var index = list.BinarySearch(item);
    //转换为链表：var linkedList = new LinkedList<T>(list);
    //转换为集合：var set = new HashSet<T>(list);
    //转换为字典（需要指定键选择器）：var dict = list.ToDictionary(item => item.Key);


    #endregion

    #region 简单版核心数据结构
    //private Dictionary<Type, List<Delegate>> _eventHandlers =
    //    new Dictionary<Type, List<Delegate>>();

    //// 新增：存储异步处理器
    //private Dictionary<Type, List<Delegate>> _asyncHandlers =
    //    new Dictionary<Type, List<Delegate>>();


    #region 异步同步+优先级
    /// <summary>
    /// 处理器包装类，存储处理器和它的优先级
    /// </summary>
    //private class HandlerWrapper
    //{
    //    //作用：将处理器委托包装起来，添加属性：
    //    public Delegate Handler;  // 处理器委托
    //    public int Priority;      //优先级
    //    public bool IsAsync;      //是否异步

    //    public HandlerWrapper(Delegate handler, int priority, bool isAsync)
    //    {
    //        Handler = handler;
    //        Priority = priority;
    //        IsAsync = isAsync;
    //    }
    //}

    #endregion

    #region 线程安全+内存安全

    /// <summary>
    /// 处理器包装类，存储处理器和它的优先级
    /// </summary>
    //private class HandlerWrapper
    //{
    //    //作用：将处理器委托包装起来，添加属性：
    //    public Delegate Handler;  // 处理器委托
    //    public int Priority;      //优先级
    //    public bool IsAsync;      //是否异步
    //    public bool IsDisposed; // 新增：标记是否已释放
    //    public HandlerWrapper(Delegate handler, int priority, bool isAsync, bool isDisposed)
    //    {
    //        Handler = handler;
    //        Priority = priority;
    //        IsAsync = isAsync;
    //        IsDisposed = isDisposed;
    //    }
    //}
    #endregion

    ////管理更加细化，更方便的给处理器添加属性
    //private Dictionary<Type, List<HandlerWrapper>> _handlers = new Dictionary<Type, List<HandlerWrapper>>();

    ////同步锁?
    //private readonly object _lock = new object();

    #endregion

    #region 简单版核心功能实现

    #region 带优先级的核心功能实现

    /// <summary>
    /// 带优先级,线程安全的的同步订阅
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="handler"></param>
    /// <param name="priority"></param>
    //public void SimpleSubcribe<TEvent>(Action<TEvent> handler, int priority = 0) where TEvent : class, IEvent
    //{
    //    //获取事件类型
    //    var eventType = typeof(TEvent);
    //    //同一时间只有一个线程能访问
    //    lock (_lock)
    //    {
    //        //如果没有该事件类型的处理器(委托)列表，则创建一个新的列表
    //        if (!_handlers.ContainsKey(eventType))
    //        {
    //            _handlers[eventType] = new List<HandlerWrapper>();
    //        }
    //        // 将处理器（委托）添加到对应事件的处理器列表中
    //        _handlers[eventType].Add(new HandlerWrapper(handler, priority, false));

    //        Log($"{handler.ToString()}已同步订阅事件{eventType.Name}");

    //        // 按优先级排序（优先级数字小的在前）
    //        _handlers[eventType] = _handlers[eventType]
    //            .OrderBy(h => h.Priority)
    //            .ToList();
    //    }
    //}

    /// <summary>
    /// 带优先级的异步订阅
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="handler"></param>
    /// <param name="priority"></param>
    //public void SimpleSubcribeAsync<TEvent>(Func<TEvent, Task> handler, int priority)
    //{
    //    var eventType = typeof(TEvent);
    //    //同一时间只有一个线程能访问
    //    lock (_lock)
    //    {

    //        if (!_handlers.ContainsKey(eventType))
    //        {
    //            _handlers[eventType]
    //                = new List<HandlerWrapper>();
    //        }

    //        _handlers[eventType].Add(new HandlerWrapper(handler, priority, true));

    //        Log($"{handler.ToString()}已异步订阅事件{eventType.Name}");

    //        // 按优先级排序（优先级数字小的在前）
    //        _handlers[eventType] = _handlers[eventType]
    //            .OrderBy(h => h.Priority)
    //            .ToList();
    //    }
    //}

    /// <summary>
    /// 发布事件，同时通知同步和异步的，使用Task.Run 不会阻塞方法
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="eventData"></param>
    //public void SimplePulishRun<TEvent>(TEvent eventData) where TEvent : class, IEvent
    //{
    //    var eventType = typeof(TEvent);

    //    List<HandlerWrapper> handlersToExecute;

    //    lock (_lock)
    //    {
    //        if (!_handlers.ContainsKey(eventType)) return;
    //        // 创建处理器列表的副本,可以在锁外执行处理器，减少锁持有时间
    //        handlersToExecute = new List<HandlerWrapper>(_handlers[eventType]);
    //    }

    //    // 2. 在锁外执行所有处理器
    //    // 这样可以避免处理器执行时间过长阻塞其他线程
    //    foreach (var handler in handlersToExecute)
    //    {
    //        try
    //        {
    //            if (handler.IsAsync)
    //            {
    //                // 使用Task.Run而不是直接await，避免阻塞当前线程
    //                Task.Run(async () =>
    //                {
    //                    try
    //                    {
    //                        await ((Func<TEvent, Task>)handler.Handler)(eventData);
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        HandleError(ex, eventData, handler);
    //                    }
    //                });
    //            }
    //            else
    //            {
    //                //同步时间直接执行
    //                Action<TEvent> syncAction = (Action<TEvent>)handler.Handler;
    //                syncAction(eventData);
    //            }
    //        }
    //        catch (Exception ex) {
    //            // 异常处理：一个处理器出错不应该影响其他处理器
    //            HandleError(ex, eventData, handler);
    //        }

    //    }
    //}

    /// <summary>
    /// 异常处理方法（线程安全的错误处理）
    /// </summary>
    //private void HandleError<TEvent>(Exception ex, TEvent eventData, HandlerWrapper wrapper)
    //{
    //    // 记录错误日志（线程安全的方式）
    //    lock (_lock)
    //    {
    //        UnityEngine.Debug.LogError($"事件处理失败: {typeof(TEvent).Name}," +
    //            $" 错误: {ex.Message}," +
    //            $"错误处理器：{wrapper}");
    //    }
    //    // 可以在这里添加更多错误处理逻辑
    //    // 比如：禁用出错的处理器、发送错误报告等
    //    var type = typeof(TEvent);
    //    _handlers[type].Remove(wrapper);
    //    UnityEngine.Debug.LogError($"错误处理器：{wrapper} 已经移除");
    //}

    /// <summary>
    /// 发布事件，同时通知同步和异步的，使用Task.WhenAll 不会阻塞方法
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="eventData"></param>
    //public async void SimplePulishWhenAll<TEvent>(TEvent eventData) where TEvent : class, IEvent
    //{
    //    var eventType = typeof(TEvent);

    //    if (!_handlers.ContainsKey(eventType)) return;
    //    List<Task> tasks = new List<Task>();

    //    foreach (var handler in _handlers[eventType])
    //    {
    //        if (handler.IsAsync)
    //        {
    //            Func<TEvent, Task> asyncFunc = (Func<TEvent, Task>)handler.Handler;
    //            tasks.Add(asyncFunc(eventData));
    //        }
    //        else
    //        {
    //            Action<TEvent> syncAction = (Action<TEvent>)handler.Handler;
    //            syncAction(eventData);
    //        }

    //    }
    //    // 等待所有异步任务完成
    //    await Task.WhenAll(tasks);
    //}

    #endregion

    #region 异步和同步的核心
    /// <summary>
    /// 订阅实现：将处理器(委托)添加到对应事件类型的列表中
    /// 订阅事件：告诉事件总线“当XX事件发生时，请执行我的方法”
    /// </summary>
    /// <typeparam name="TEvent">你要监听的事件类型</typeparam>
    /// <param name="handler">事件发生时执行的方法（委托）</param>
    //public void SimpleSubscribe<TEvent>(Action<TEvent> handler)
    //{
    //    //获取事件类型
    //    var eventType = typeof(TEvent);
    //    //如果没有该事件类型的处理器(委托)列表，则创建一个新的列表
    //    if (!_eventHandlers.ContainsKey(eventType))
    //    {
    //        _eventHandlers[eventType] = new List<Delegate>();
    //    }
    //    // 将处理器（委托）添加到对应事件的处理器列表中
    //    _eventHandlers[eventType].Add(handler);

    //    Log($"{handler.ToString()}已订阅事件{eventType.Name}");
    //}
    ///<summary>
    /// 异步订阅实现：将异步处理器(委托)添加到对应事件类型的列表中
    /// 如果你希望等待所有异步处理器完成
    ///</summary>

    //Func<TEvent, Task> 类型的委托，表示接受 TEvent 类型参数并返回 Task 的方法 方法委托，最后一个类型参数是返回值类型，前面的类型参数是输入参数类型
    //public void SimpleSubscribeAsync<TEvent>(Func<TEvent, Task> handler)
    //{
    //    //获取事件类型
    //    var eventType = typeof(TEvent);
    //    //如果没有该事件类型的处理器(委托)列表，则创建一个新的列表
    //    if (!_asyncHandlers.ContainsKey(eventType))
    //    {
    //        _asyncHandlers[eventType] = new List<Delegate>();
    //    }
    //    // 将处理器（委托）添加到对应事件的处理器列表中
    //    _asyncHandlers[eventType].Add(handler);
    //    Log($"{handler}已异步订阅事件{eventType.Name}");
    //}



    ///<summary>
    ///发布事件：通知所有订阅者“XX事件发生了” 游戏的逻辑正常运行，不会等待所有异步处理器完成
    ///</summary>
    /// <typeparam name="TEvent">要发布的事件类型</typeparam>
    /// <param name="eventData">事件的具体数据</param>
    //public void SimplePublish<TEvent>(TEvent eventData)
    //{
    //    var eventType = typeof(TEvent);

    //    //检查是否有订阅该事件的处理器
    //    if (_eventHandlers.ContainsKey(eventType))
    //    {
    //        //遍历所有订阅该事件的处理器，并调用它们
    //        foreach (var handler in _eventHandlers[eventType])
    //        {
    //            //将事件数据传递给处理器
    //            ((Action<TEvent>)handler)?.Invoke(eventData);
    //        }
    //        Log($"{eventType.Name}事件已发布，通知了{_eventHandlers[eventType].Count}个订阅者");
    //    }
    //    else
    //    {
    //        LogWarning($"没有订阅者订阅{eventType.Name}事件");
    //    }

    //    //处理异步订阅者
    //    if (_asyncHandlers.ContainsKey(eventType))
    //    {
    //        foreach (var asyncHandler in _asyncHandlers[eventType])
    //        {
    //            //启动异步处理器
    //            var handlerFunc = (Func<TEvent, Task>)asyncHandler;

    //            //这样启动的线程不能使用unity的API，只能在主线程调用，会直接卡在那里，这个线程会一直执行直到关闭游戏
    //            //也可以使用unity的api，但必须要手动切换回主线程才能使用
    //            Task.Run(async () => await handlerFunc(eventData));
    //            // ↑ 调用此方法的协程不会在此处暂停
    //            // ↓ 直到所有tasks完成后才继续执行
    //        }
    //        Log($"{eventType.Name}事件已发布，通知了{_asyncHandlers[eventType].Count}个异步订阅者");
    //    }
    //    else
    //    {
    //        LogWarning($"没有异步订阅者订阅{eventType.Name}事件");
    //    }
    //}

    /// <summary>
    /// 异步发布事件（新增） 游戏逻辑会等待该方法完成（相当于阻塞了主线程）？
    /// 如果你希望等待所有异步处理器完成
    /// </summary>
    //public async Task PublishAsync<TEvent>(TEvent eventData)
    //{
    //    Type eventType = typeof(TEvent);

    //    // 执行同步处理器
    //    if (_eventHandlers.ContainsKey(eventType))
    //    {
    //        foreach (Delegate handler in _eventHandlers[eventType])
    //        {
    //            Action<TEvent> typedHandler = (Action<TEvent>)handler;
    //            typedHandler(eventData);
    //        }
    //    }

    //    // 执行异步处理器，并等待它们全部完成
    //    if (_asyncHandlers.ContainsKey(eventType))
    //    {
    //        List<Task> tasks = new List<Task>();

    //        foreach (Delegate handler in _asyncHandlers[eventType])
    //        {
    //            Func<TEvent, Task> typedHandler = (Func<TEvent, Task>)handler;
    //            //这里是利用事件的第一种使用方式，事件中心其实就是存放事件的地方Func f = method();  使用就是 f();
    //            //提供让事件被使用的方法和事件被订阅的方法，通过事件的类型管理
    //            tasks.Add(typedHandler(eventData));
    //        }
    //        // 这里使用 Task.WhenAll 来等待所有异步任务完成，可以执行Api

    //        // 等待所有异步任务完成
    //        await Task.WhenAll(tasks);
    //        // ↑ 调用此方法的协程会在此处暂停
    //        // ↓ 直到所有tasks完成后才继续执行
    //    }
    //}

    #endregion

    #region 线程安全以及内存安全核心功能实现
    //订阅令牌具体包装
    //private class SubscriptionToken<TEvent> : IDisposable where TEvent : class, IEvent
    //{
    //    private EventBus _eventBus;
    //    private Action<TEvent> _action;
    //    private HandlerWrapper _wrapper;
    //    public SubscriptionToken(EventBus _eventBus, Action<TEvent> action, HandlerWrapper wrapper)
    //    {
    //        _eventBus = _eventBus;
    //        _action = action;
    //        _wrapper = wrapper;
    //    }
    //    //取消订阅
    //    public void Dispose()
    //    {
    //        _eventBus.Unsubscribe(_action, _wrapper);

    //        //清除引用，减少GC压力
    //        _eventBus = null;
    //        _action = null;
    //        _wrapper = null;

    //    }

    //}
    ////取消订阅，专门给SubscriptionToken使用的
    //private void Unsubscribe<TEvent>(Action<TEvent> action, HandlerWrapper wrapper)
    //{
    //    Type eventType = typeof(TEvent);

    //    lock (_lock)
    //    {
    //        // 标记为已释放
    //        wrapper.IsDisposed = true;

    //        if (_handlers.ContainsKey(eventType))
    //        {
    //            // 从列表中移除
    //            _handlers[eventType].Remove(wrapper);

    //            if (_handlers[eventType].Count == 0)
    //            {
    //                _handlers.Remove(eventType);
    //            }
    //        }
    //    }
    //}

    ////订阅事件，返回订阅令牌，订阅令牌可以用来取消订阅 ,只有同步事件，异步事件差不多道理
    //public IDisposable SubscibeSimple<TEvent>(Action<TEvent> handler, int priority = 0) where TEvent : class, IEvent
    //{
    //    var eventType = typeof(TEvent);
    //    //锁保证同时只有一个线程访问
    //    lock (_lock)
    //    {
    //        if (!_handlers.ContainsKey(eventType))
    //        {
    //            //如果处理器字典里面没有该事件类型的处理器集合，则创建一个
    //            _handlers[eventType] = new List<HandlerWrapper>();
    //        }
    //        //将传入的处理器包装起来，默认不需要异步，也还没有被释放
    //        HandlerWrapper wrapper = new HandlerWrapper(handler, priority, false, false);

    //        //将包装好的处理器放入该类型处理的仓库里面
    //        _handlers[eventType].Add(wrapper);
    //        //根据优先级排序
    //        _handlers[eventType] = _handlers[eventType].OrderBy(x => x.Priority).ToList();

    //        return new SubscriptionToken<TEvent>(this, handler, wrapper);

    //    }

    //}

    ///// <summary>
    ///// 发布事件时跳过已释放的处理器
    ///// </summary>
    //public void SimplePublish<TEvent>(TEvent eventData)
    //{
    //    Type eventType = typeof(TEvent);

    //    List<HandlerWrapper> handlersToExecute;

    //    lock (_lock)
    //    {
    //        if (!_handlers.ContainsKey(eventType)) return;

    //        // 过滤掉已释放的处理器
    //        handlersToExecute = _handlers[eventType]
    //            .Where(w => !w.IsDisposed)
    //            .ToList();
    //    }

    //    foreach (HandlerWrapper wrapper in handlersToExecute)
    //    {
    //        if (!wrapper.IsDisposed) // 再次检查，确保安全
    //        {
    //            try
    //            {
    //                if (wrapper.IsAsync)
    //                {
    //                    // 使用Task.Run而不是直接await，避免阻塞当前线程
    //                    Task.Run(async () =>
    //                    {
    //                        try
    //                        {
    //                            await ((Func<TEvent, Task>)wrapper.Handler)(eventData);
    //                        }
    //                        catch (Exception ex)
    //                        {
    //                            //HandleError(ex, eventData, wrapper);
    //                        }
    //                    });
    //                }
    //                else
    //                {
    //                    //同步时间直接执行
    //                    Action<TEvent> syncAction = (Action<TEvent>)wrapper.Handler;
    //                    syncAction(eventData);
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                // 异常处理：一个处理器出错不应该影响其他处理器
    //                //HandleError(ex, eventData, wrapper);
    //            }
    //        }
    //    }
    //}

    #endregion

    #endregion
    /// <summary>
    /// 取消订阅：移除指定事件类型的处理器
    /// 告诉事件总线“我不想再接收XX事件了”
    /// </summary>
    /// <typeparam name="TEvent">你要监听的事件类型</typeparam>
    /// <param name="handler">事件发生时执行的方法（委托）</param>
    //public void Unsubscribe<TEvent>(Action<TEvent> handler)
    //{
    //    Type eventType = typeof(TEvent);

    //    lock (_lock)
    //    {
    //        if (_handlers.ContainsKey(eventType))
    //        {
    //            // 找到要移除的处理器
    //            var wrapperToRemove = _handlers[eventType]
    //                .FirstOrDefault(w => !w.IsAsync && w.Handler.Equals(handler));

    //            if (wrapperToRemove != null)
    //            {
    //                _handlers[eventType].Remove(wrapperToRemove);
    //            }

    //            // 如果列表空了，移除整个条目
    //            if (_handlers[eventType].Count == 0)
    //            {
    //                _handlers.Remove(eventType);
    //            }
    //        }
    //    }
    //}

    //public void SimpleClear()
    //{
    //    _handlers.Clear();
    //    Log("已清除所有事件订阅");
    //}

    #region 核心数据结构
    // 核心数据结构：用字典存储事件类型和对应的处理器列表
    // Dictionary<事件类型, 该事件的所有处理器（委托）>
    //目前不做高性能优化，可以不考虑字典，用普通字典

    // 核心数据结构：存储事件类型与处理器包装的映射，ConcurrentDictionary是字典的高级形式，自动保证线程安全
    private readonly ConcurrentDictionary<Type, List<EventHandlerWrapper>> _handlers =
        new ConcurrentDictionary<Type, List<EventHandlerWrapper>>();

    // 用于线程安全的锁对象
    private readonly object _lock = new object();

    // 是否已释放标志
    private bool _disposed = false;

    // 订阅计数器，用于生成唯一订阅ID,计数是用来确定唯一性的，而且根据程序的生命周期，并没有被持久化，所以每次重启都会重置
    private int _subscriptionCounter = 0;
    #endregion

    #region 处理器包装
    /// <summary>
    /// 处理器包装类，统一处理委托和对象处理器
    /// </summary>
    private class EventHandlerWrapper : IDisposable
    {
        // 处理器ID（用于取消订阅）提供精确取消订阅的方式
        public int SubscriptionId { get; }

        // 委托处理器（用于Action<TEvent>） 支持简单的匿名函数或方法 不需要异步执行的用这个
        public Delegate DelegateHandler { get; }

        // 对象处理器（用于实现了IEventHandler<TEvent>接口的） 支持完整的异步处理器对象,处理复杂的异步逻辑，用异步的就用这个
        public IEventHandler<IEvent> ObjectHandler { get; }

        // 优先级
        public int Priority { get; }

        // 过滤器 ，提供条件订阅功能
        /**
         * 列如：// 只处理品质3以上的药水事件
         * bus.Subscribe<PotionBrewedEvent>(
         *  handler: e => DoSomething(e),
         *  filter: e => e.QualityLevel >= 3
         * )；
         */
        //这里的Filter就是条件
        public Func<IEvent, bool> Filter { get; }

        // 是否启用，可以临时禁用处理器而不移除，原子操作，更安全,在Dispose()中标记为禁用，然后从列表中移除
        public bool IsEnabled { get; set; } = true;

        // 是否为异步处理器。自动判断
        public bool IsAsync => ObjectHandler != null;

        // 事件类型 知道包装器处理什么类型的事件,类型安全：在泛型类型擦除后保留类型信息
        public Type EventType { get; }

        public EventHandlerWrapper(
            int subscriptionId,
            Type eventType,
            Delegate delegateHandler,
            IEventHandler<IEvent> objectHandler,
            int priority,
            Func<IEvent, bool> filter)
        {
            SubscriptionId = subscriptionId;
            EventType = eventType;
            DelegateHandler = delegateHandler;
            ObjectHandler = objectHandler;
            Priority = priority;
            Filter = filter;
        }

        public void Dispose()
        {
            IsEnabled = false;
        }

        // 执行处理器统一执行接口 还能保证执行的顺序
        public async Task ExecuteAsync(IEvent @event, CancellationToken cancellationToken)
        {
            if (!IsEnabled) return;
            if (Filter != null && !Filter(@event)) return;
            //Debug.Log("开始尝试");
            try
            {
                if (ObjectHandler != null)
                {
                    //Debug.Log("执行异步方法");
                    // 异步处理器对象
                    await ObjectHandler.HandleAsync(@event, cancellationToken);
                }
                else if (DelegateHandler != null)
                {

                    // 同步委托处理器
                    if (DelegateHandler is Action<IEvent> action)
                    {
                        action(@event);
                        //UnityEngine.Debug.Log($"事件处理器：类型action");
                    }
                    else
                    {
                        // 动态调用委托
                        DelegateHandler.DynamicInvoke(@event);
                        UnityEngine.Debug.LogWarning($"事件处理器：未知的委托类型");
                    }
                    // 这里可以添加其他委托类型的支持
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不中断其他处理器
                UnityEngine.Debug.LogError($"事件处理器执行失败 (EventId: {@event.EventId}): {ex.Message}");

            }
        }
    }

    #endregion

    #region 订阅令牌
    /// <summary>
    /// 订阅令牌和处理器包装高度绑定的
    /// 后面应该有获取令牌的方式，只在内部使用，其他类访问不了，但其他类能看到IDisposable所以能用发布和订阅方法
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    private class SubscriptionToken<TEvent> : IDisposable where TEvent : class, IEvent
    {
        //事件总线，处理器包装ID，释放标识
        private readonly EventBus _eventBus;
        private readonly int _subscriptionId;
        private bool _disposed = false;

        //初始化方法
        public SubscriptionToken(EventBus eventBus, int subscriptionId)
        {
            _eventBus = eventBus;
            _subscriptionId = subscriptionId;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _eventBus.UnsubscribeInternal<TEvent>(_subscriptionId);  //取消某类事件指定id的处理器订阅，泛型决定事件类型，_subscriptionId决定具体处理器
                _disposed = true;
            }
        }
    }

    #endregion

    #region 核心功能实现


    // ======================================= 订阅相关方法 =========================================
    /// <summary>
    /// 订阅指定类型的事件（简化版）
    /// 使用Action委托作为事件处理器，默认优先级，不做条件过滤
    /// 返回IDisposable订阅令牌，用于取消订阅
    /// </summary>
    /// <typeparam name="TEvent">事件类型，必须实现IEvent接口</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>订阅令牌，Dispose()时自动取消订阅</returns>
    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class, IEvent
    {
        // 关键：显式转换为 Action<IEvent>
        
        return Subscribe(handler, 0, null);
    }
    //为什么这里使用Action，而不是Func,为什么异步又要使用Func,订阅事件是不是都不能有返回值，如果有返回值这个返回值能干什么，能用吗

    /// <summary>
    /// 订阅事件（完整版，支持优先级和条件过滤）
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
    public IDisposable Subscribe<TEvent>(
        Action<TEvent> handler,
        int priority = 0,
        Func<TEvent, bool> filter = null)  //Func<TEvent, bool> 类型的过滤器,属于方法委托
        where TEvent : class, IEvent
    {
        //首先检查是否传入了个空的处理器
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var subscriptionId = Interlocked.Increment(ref _subscriptionCounter);
        Action<IEvent> convertedHandler = e => handler((TEvent)e);
        //将条件转换为具体的委托
        Func<IEvent, bool> genericFilter = null;
        if (filter != null)
        {
            genericFilter = e => filter((TEvent)e);
        }

        //创建该处理器的包装类
        var wrapper = new EventHandlerWrapper(
            subscriptionId,
            eventType,
            convertedHandler,
            null,// 这是委托处理器，不是对象处理器
            priority,
            genericFilter
            );

        AddHandler(eventType, wrapper);

        return new SubscriptionToken<TEvent>(this, subscriptionId);
    }

    /// <summary>
    /// 使用独立处理器对象订阅事件
    /// 适用于需要依赖注入或复杂状态管理的处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">实现了IEventHandler接口的处理器对象</param>
    /// <returns>订阅令牌</returns>
    public IDisposable Subscribe<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : class, IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var subscriptionId = Interlocked.Increment(ref _subscriptionCounter);

        // 将特定类型的处理器转换为通用接口
        IEventHandler<IEvent> genericHandler = new GenericEventHandlerWrapper<TEvent>(handler);

        var wrapper = new EventHandlerWrapper(
           subscriptionId,
           eventType,
           null,                   // 这是对象处理器，不是委托
           genericHandler,
           handler.Priority,
           null // 对象处理器可以自己在HandleAsync中实现过滤逻辑
       );

        AddHandler(eventType, wrapper);

        return new SubscriptionToken<TEvent>(this, subscriptionId);
    }

    /// <summary>
    /// 添加处理器到字典（线程安全）
    /// </summary>
    private void AddHandler(Type eventType, EventHandlerWrapper wrapper)
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlerList))
            {
                handlerList = new List<EventHandlerWrapper>();
                _handlers[eventType] = handlerList;
            }

            handlerList.Add(wrapper);

            // 按优先级排序（优先级数字小的先执行）
            handlerList.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
    }

    // ======================================= 发布相关方法 =========================================

    /// <summary>
    /// 发布事件（同步阻塞方式）
    /// 在当前线程立即执行所有匹配的处理器
    /// 注意：处理器异常会被捕获并记录，不会中断其他处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    public void Publish<TEvent>(TEvent @event)
        where TEvent : class, IEvent
    {
        var eventType = typeof(TEvent);

        if (@event == null)
            throw new ArgumentNullException(nameof(@event));
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventBus));
        List<EventHandlerWrapper> handlersToExecute;

        // 获取当前需要执行的处理器列表（线程安全快照）
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                return;
            handlersToExecute = handlers.Where(h => h.IsEnabled).ToList();
        }

        if (handlersToExecute.Count == 0)
        {
            return;
        }
        // 执行所有处理器（同步处理器直接执行，异步处理器使用Task.Run）
        foreach (var wrapper in handlersToExecute)
        {
            if (wrapper.IsAsync)
            {
                // 异步处理器：在后台线程执行，不阻塞当前线程
                _ = Task.Run(() => wrapper.ExecuteAsync(@event, CancellationToken.None));
            }
            else
            {
                // 同步处理器：在当前线程立即执行
                wrapper.ExecuteAsync(@event, CancellationToken.None).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// 发布事件（异步非阻塞方式）
    /// 在后台线程执行处理器，避免阻塞主线程
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步操作的Task</returns>
    public async Task PublishAsync<TEvent>(TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        if (_disposed)
            throw new ObjectDisposedException(nameof(EventBus));

        var eventType = typeof(TEvent);
        List<EventHandlerWrapper> handlersToExecute;

        // 获取处理器列表
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                return;

            handlersToExecute = handlers.Where(h => h.IsEnabled).ToList();
        }

        if (handlersToExecute.Count == 0)
            return;

        // 收集所有异步任务
        var tasks = new List<Task>();

        foreach (var wrapper in handlersToExecute)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (wrapper.IsAsync)
            {
                // 异步处理器：添加到任务列表
                tasks.Add(wrapper.ExecuteAsync(@event, cancellationToken));
            }
            else
            {
                // 同步处理器：使用Task.Run包装以避免阻塞
                tasks.Add(Task.Run(() => wrapper.ExecuteAsync(@event, cancellationToken), cancellationToken));
            }
        }

        // 等待所有任务完成（或取消）
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 任务被取消，正常处理
            Log("事件发布被取消");
        }
        catch (AggregateException ae)
        {
            // 记录聚合异常中的错误
            foreach (var ex in ae.InnerExceptions)
            {
                LogError($"事件处理失败: {ex.Message}");
            }
        }
    }


    /// <summary>
    /// 批量发布多个事件（异步）
    /// 性能优化：减少锁竞争和上下文切换
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="events">事件集合</param>
    /// <returns>表示异步操作的Task</returns>
    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events)
        where TEvent : class, IEvent
    {
        if (events == null)
            throw new ArgumentNullException(nameof(events));

        var tasks = events.Select(e => PublishAsync(e));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    // ======================================= 取消订阅方法 =========================================

    //取消的接口
    public void UnSubscribeEvent<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : class, IEvent
    {
        if (handler == null) return;

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                // 找到对应的包装器并移除
                var wrapperToRemove = handlers.FirstOrDefault(h =>
                    h.ObjectHandler is GenericEventHandlerWrapper<TEvent> genericWrapper &&
                    genericWrapper.WrappedHandler.Equals(handler));

                if (wrapperToRemove != null)
                {
                    wrapperToRemove.Dispose(); // 标记为禁用
                    handlers.Remove(wrapperToRemove);

                    // 如果列表为空，移除整个键
                    if (handlers.Count == 0)
                    {
                        _handlers.TryRemove(eventType, out _);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 取消所有事件订阅
    /// 通常在场景切换或游戏关闭时调用
    /// 原Clear()方法已弃用，改为UnsubscribeAll() 易于理解
    /// </summary>
    public void UnsubscribeAll()
    {
        //锁保证只有一个在清理
        lock (_lock)
        {
            foreach (var handlerList in _handlers.Values)
            {
                if (handlerList != null)
                {
                    foreach (var handler in handlerList)
                    {
                        if (handler != null)
                            handler.Dispose();
                    }
                    handlerList.Clear();
                }
            }
            _handlers.Clear();
        }
    }

    /// <summary>
    /// 内部取消订阅方法（供SubscriptionToken调用）
    /// </summary>
    private void UnsubscribeInternal<TEvent>(int subscriptionId)
        where TEvent : class, IEvent
    {
        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                var wrapperToRemove = handlers.FirstOrDefault(h => h.SubscriptionId == subscriptionId);
                if (wrapperToRemove != null)
                {
                    wrapperToRemove.Dispose();
                    handlers.Remove(wrapperToRemove);

                    if (handlers.Count == 0)
                    {
                        _handlers.TryRemove(eventType, out _);
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnsubscribeAll();
            _disposed = true;
        }
    }

    public void OpenEventBus()
    {
        if (_disposed)
        {
            _disposed = false;
        }
    }
    #endregion

    #region 辅助类


    /// <summary>
    /// 泛型事件处理器包装器，将IEventHandler<TEvent>转换为IEventHandler<IEvent>
    /// </summary>
    private class GenericEventHandlerWrapper<TEvent> : IEventHandler<IEvent>
        where TEvent : class, IEvent
    {
        public IEventHandler<TEvent> WrappedHandler { get; }

        public int Priority => WrappedHandler.Priority;

        public bool IsEnabled
        {
            get => WrappedHandler.IsEnabled;
            set => WrappedHandler.IsEnabled = value;
        }

        public GenericEventHandlerWrapper(IEventHandler<TEvent> handler)
        {
            WrappedHandler = handler;
        }

        public async Task HandleAsync(IEvent @event, CancellationToken cancellationToken)
        {
            if (@event is TEvent typedEvent)
            {
                await WrappedHandler.HandleAsync(typedEvent, cancellationToken);
            }
        }
    }

    public List<int> GetHandlerIDByEventType<TEvent>()
    {
        Type type = typeof(TEvent);
        List<int> result = new List<int>();
        if (_handlers.ContainsKey(type))
        {

            foreach (var ID in _handlers[type])
            {
                result.Add(ID.SubscriptionId);
            }
        }
        return result;
    }

    #endregion

    #region 日志功能
    private void Log(string message)
    {
        UnityEngine.Debug.Log($"[EventBus] : {message}");
    }

    private void LogError(string message)
    {
        UnityEngine.Debug.LogError($"[EventBus] : {message}");
    }

    private void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning($"[EventBus] : {message}");
    }



    #endregion
}
