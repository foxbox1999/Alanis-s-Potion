using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    /**
     *  游戏的核心管理器，负责游戏状态机、全局运行时标志、核心服务初始化等。
     */
    #region 配置
    //[SerializeField] private EventManager eventManager;
    //[SerializeField] private SaveManager saveManager;
    [SerializeField] private UIManager _ui;
    [SerializeField] private AssetManager _asset;
    [SerializeField] private AudioManager _audio;
    [SerializeField] private bool isTest = false;
    public string testAim = "";
    private PlayerManager _player;
    private ConfigManager _config;
    private IEventBus _eventBus;
    private Dictionary<string, IDisposable> _subscriptions;

    public IGameState Boot { get; private set; }
    public IGameState Loading { get; private set; }
    public IGameState StartState { get; private set; }
    public IGameState GameLoop { get; private set; }
    public IGameState Paused { get; private set; }
    public IGameState Test { get; private set; }

    private IGameState _currentState;
    public IGameState CurrentState
    {
        get
        {
            return _currentState;
        }
    }
    #endregion

    #region 运行时状态
    /// <summary>是否处于「忙碌」状态（采集动画/弹窗/转场等阻塞活动中）。睡眠提醒等空闲才触发。</summary>
    public bool IsBusy => _busyCount > 0;
    private int _busyCount;
    // --- 全局运行时标志 ---
    public bool IsGamePaused { get; private set; }
    #endregion

    #region 生命周期函数
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        _eventBus = EventBus.Instance;
        Boot = new BootState(this);
        Loading = new LoadingState(this);
        StartState = new StartState(this);
        GameLoop = new GameLoopState(this);
        Paused = new PausedState(this);
        Test = new TestState(this);
        InitState();
        SubEvent();
    }

    protected override void Start()
    {
        base.Start();
        InitializeCoreServices();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 取消订阅所有事件
        if (_subscriptions != null)
        {
            foreach (var sub in _subscriptions.Values)
            {
                sub?.Dispose();
            }
            _subscriptions.Clear();
            _subscriptions = null;
        }
    }
    private void Update()
    {
        // 只有非暂停且状态不为空时，才更新当前状态（如对话状态的逐字打印）
        if (!IsGamePaused)
        {
            _currentState?.Update();
        }
    }

    #endregion

    #region 初始化控制
    /// <summary>
    /// 初始化核心服务，包括资源管理器、音频管理器、配置管理器等。 在各个管理器都处于就绪状态后，游戏可以进入主菜单或加载场景。
    /// </summary>
    private void InitializeCoreServices()
    {
        _config = ConfigManager.Instance; // 配置管理器
        _player = PlayerManager.Instance; // 玩家管理器

        _config.LoadOrCreateUserSettings();
        _player.UninstallPlayerObject(); // 卸载玩家对象，确保游戏开始时没有残留的玩家对象
        // 3 如有其他初始化（例如预加载资源、初始化存档等），在此添加
        // 例如：SaveManager.Instance.LoadGameData();
        if (!isTest)
            ChangeState(StartState);
        else
            ChangeState(Test);
    }
    /// <summary>
    /// 订阅游戏管理器需要监听的全局事件，例如暂停、恢复、状态切换等。
    /// </summary>
    private void SubEvent()
    {
        SafeSubscribe<NewGameStartedEvent>("NewGame", OnNewGameStarted);
    }

    /// <summary>
    /// 初始化游戏状态机，设置初始状态为 Boot 状态。
    /// </summary>
    private void InitState()
    {
        ChangeState(Boot);
    }
    /// <summary>
    /// 初始化游戏状态机，设置初始状态为指定的状态。
    /// </summary>
    /// <param name="_state"></param>
    private void InitState(IGameState _state)
    {
        if (_state == null)
            return;
        ChangeState(_state);
    }
    #endregion

    #region Cores
    /// <summary>
    /// 切换游戏状态机的当前状态
    /// </summary>
    /// <param name="newState"></param>
    public void ChangeState(IGameState newState)
    {
        if (newState == null || newState == _currentState) return;
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
        EventBus.Instance.Publish(new GameStateChangedEvent { NewState = newState });
    }

    /// <summary>供 PausedState 设置暂停标记。</summary>
    public void SetPaused(bool paused) => IsGamePaused = paused;
    /// <summary>
    /// busy事务+1
    /// </summary>
    public void EnterBusy() => _busyCount++;
    /// <summary>
    /// busy事务-1 最小为0
    /// </summary>
    public void ExitBusy() => _busyCount = Mathf.Max(0, _busyCount - 1);
    #endregion

    #region EventHandler
    private void OnNewGameStarted(NewGameStartedEvent e)
    {
        if (isTest)
            return;
        // 处理新游戏开始事件，例如重置游戏数据、加载初始场景等
        Debug.Log("新游戏开始！");
        // 1.切换游戏状态为 GameLoop
        ChangeState(GameLoop);
        // 2.关闭所有 UI 面板，确保新游戏开始时没有残留的 UI
        UIManager.Instance.CloseAll();
        // 3.流程管理器开始第一段流程控制
        // 3.1 获取当前剧情步骤
        //PlotDirector.Instance.GetCurrentStep();
        // 3.2 开始第一段剧情流程
        PlotDirector.Instance.Advance();
    }

    #endregion

    #region Tools
    private void SafeSubscribe<T>(string key, Action<T> action, int priority = 0) where T : EventBase
    {
        if (_eventBus == null)
        {
            return;
        }
        if (_subscriptions == null)
            _subscriptions = new Dictionary<string, IDisposable>();

        if (_subscriptions.TryGetValue(key, out var oldSub))
            oldSub?.Dispose();

        _subscriptions[key] = _eventBus.Subscribe(action, priority);
    }
    #endregion
}
