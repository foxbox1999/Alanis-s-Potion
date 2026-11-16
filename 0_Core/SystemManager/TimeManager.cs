using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏时间管理器（单例）
/// 职责：
/// 1. 维护游戏内日期时间（起始于 1509-04-20 08:00:00）
/// 2. 支持时间倍速（默认12倍），与 Unity 引擎时间解耦
/// 3. 管理游戏内计时器（炼药、Buff、作物生长等）
/// 4. 发布时间事件（每分钟、每小时、每天）
/// 5. 支持暂停/恢复，与 GameManager 状态联动
/// 6. 支持存档/读档
/// </summary> 
public class TimeManager : MonoSingleton<TimeManager>,ISaveable
{
    #region 常量
    private const int START_YEAR = 1509;
    private const int START_MONTH = 4;
    private const int START_DAY = 20;
    private const int START_HOUR = 8;
    private const int START_MINUTE = 0;
    #endregion

    #region 存档字段
    /// <summary>存档模块唯一键。</summary>
    public string SaveKey => "time";

    /// <summary>存档格式版本。</summary>
    public int SaveVersion => 1;
    #endregion

    #region 配置

    [Header("时间配置")]
    [Tooltip("游戏时间倍速（12倍 = 游戏1天/现实2小时）")]
    [SerializeField] private float _gameTimeScale = 12f;

    [Tooltip("逻辑更新间隔（游戏内秒数，多久执行一次逻辑判断，除以12为现实时间），建议0.1~0.5")]
    [SerializeField] private float _tickInterval = 6.0f;

    #endregion

    #region 核心数据
    /// <summary>
    /// 游戏时间数据，用于运行时存储和管理游戏内的时间信息
    /// </summary>
    private GameTimeData _timeData;

    /// <summary>序列化时间状态（TimeSaveData 已是 [Serializable]，直接 ToJson）。</summary>
    public string SerializeState() => JsonUtility.ToJson(GetSaveData());
    /// <summary>
    /// 累积时间,是乘以倍速后的时间，单位s，用于处理 Tick 间隔
    /// </summary>
    private float _accumulator = 0f;
    private bool _isPaused = false;
    private DateTime _baseTime;                     // 起始时间对应的 DateTime
    private Dictionary<string, GameTimer> _activeTimers = new Dictionary<string, GameTimer>();
    private List<string> _finishedTimers = new List<string>(); // 待移除的计时器ID
    #endregion

    #region 事件订阅令牌
    private IDisposable _pauseSubscription; // 订阅暂停事件的令牌
    private IDisposable _resumeSubscription; // 订阅恢复事件的令牌
    #endregion

    #region 公共属性
    /// <summary>
    /// 用于查看运行时游戏内时间
    /// </summary>
    public GameTimeData TimeData => _timeData;
    /// <summary>
    /// 游戏内总秒数，是乘以时间倍速之后的
    /// </summary>
    public float CurrentTotalSeconds => _timeData?.TotalSeconds ?? 0f;
    public bool IsPaused => _isPaused;

    /// <summary>
    /// 游戏时间倍速（可动态修改）
    /// </summary>
    public float GameTimeScale
    {
        get => _gameTimeScale;
        set
        {
            _gameTimeScale = Mathf.Max(0, value);
            //EventBus.Instance.Publish(new GameTimeScaleChangedEvent(_gameTimeScale));
        }
    }
    #endregion

    #region 生命周期

    /// <summary>
    /// 初始化时间管理器
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        _baseTime = new DateTime(START_YEAR, START_MONTH, START_DAY, START_HOUR, START_MINUTE, 0);
        _timeData = new GameTimeData();
        _isPaused = true;
        // 确保初始数据正确
        _timeData.UpdateFromTotalSeconds(0f);
        SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        //_pauseSubscription = EventBus.Instance.Subscribe<GamePausedEvent>(_ => Pause());
        //_resumeSubscription = EventBus.Instance.Subscribe<GameResumedEvent>(_ => Resume());
        //再加一个订阅存档被加载事件
    }
    protected override void Start()
    {
        base.Start();
        SaveManager.Instance?.RegisterModule(this);
    }

    private void Update()
    {
        if (_isPaused || _timeData == null) return;

        // 获取增量时间（根据配置选择是否受 Time.timeScale 影响）
        float delta = Time.deltaTime;
        float scaledDelta = delta * _gameTimeScale;   // 应用时间倍速

        _accumulator += scaledDelta;

        //保证每次变换的时间都是_tickInterval(游戏内过了现实的_gameTimeScale倍之后)
        while (_accumulator >= _tickInterval)
        {
            _accumulator -= _tickInterval;
            Tick(_tickInterval);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _pauseSubscription?.Dispose();
        _resumeSubscription?.Dispose();
        _activeTimers.Clear();
    }
    #endregion

    #region 时间推进核心

    /// <summary>
    /// 时间滴答（固定间隔调用），用于推进游戏内时间和更新计时器，现实时间半秒执行一次
    /// </summary>
    /// <param name="deltaTime"> 传入的是_tickInterval 逻辑执行间隔</param>    
    private void Tick(float deltaTime)
    {
        // 1. 更新总秒数（游戏内总秒数）
        _timeData.TotalSeconds += deltaTime;
        _timeData.UpdateFromTotalSeconds(_timeData.TotalSeconds);
        
        // 2. 更新所有计时器
        UpdateTimers();

        // 3. 发布定时事件
        PublishTimeEvents();
    }

    /// <summary>
    /// 更新所有计时器，先清空之前的已经完成了的Timer缓存，检查是否在运行的Timer里面有没有已经完成的放到已完成的里面里面并从_activeTimers中移除，在每次逻辑触发或者快进时间时调用
    /// </summary>
    private void UpdateTimers()
    {
        _finishedTimers.Clear();

        foreach (var kvp in _activeTimers)
        {
            if (kvp.Value.Update())
            {
                _finishedTimers.Add(kvp.Key);
            }
        }

        // 移除已完成的非循环计时器
        foreach (string id in _finishedTimers)
        {
            if (_activeTimers.TryGetValue(id, out var timer) && !timer.IsLooping)
            {
                _activeTimers.Remove(id);
            }
        }
    }

    /// <summary>
    /// 发布时间事件（分钟、小时、天）
    /// </summary>
    private void PublishTimeEvents()
    {
        //// 每分钟触发
        //if (_timeData.Second == 0)
        //    EventBus.Instance.Publish(new TimeMinuteChangedEvent(_timeData));

        //// 每小时触发（0分0秒）
        if (_timeData.Second == 0 && _timeData.Minute == 0)
            Debug.Log($"[TimeManager] Tick: {_timeData.ToString()}");
        //    EventBus.Instance.Publish(new TimeHourChangedEvent(_timeData));

        //// 每天触发（0时0分0秒）
        //if (_timeData.Second == 0 && _timeData.Minute == 0 && _timeData.Hour == 0)
        //    EventBus.Instance.Publish(new TimeDayChangedEvent(_timeData));
    }
    #endregion

    #region 时间操作
    /// <summary>
    /// 获取当前时间的 DateTime 表示
    /// </summary>
    public DateTime GetCurrentDateTime()
    {
        return _baseTime.AddSeconds(_timeData.TotalSeconds);
    }

    /// <summary>
    /// 获取当前时间的格式化字符串
    /// </summary>
    public string GetTimeString(string format = "yyyy年MM月dd日 HH:mm:ss")
    {
        return GetCurrentDateTime().ToString(format);
    }

    /// <summary>
    /// 快进时间（用于睡觉、等待等），输入要增加的游戏内秒数
    /// </summary>
    public void AddTime(float seconds)
    {
        if (_timeData == null) return;
        int beforeDay = Mathf.FloorToInt(_timeData.TotalSeconds / 86400f);
        _timeData.TotalSeconds += seconds;
        _timeData.UpdateFromTotalSeconds(_timeData.TotalSeconds);

        // 强制更新所有计时器
        UpdateTimers();
        //EventBus.Instance.Publish(new TimeSkippedEvent(seconds));
        int afterDay = Mathf.FloorToInt(_timeData.TotalSeconds / 86400f);
        for (int d = beforeDay + 1; d <= afterDay; d++)
            EventBus.Instance.Publish(new NewDayEvent { DayIndex = d });
    }
    #endregion

    #region 暂停控制
    /// <summary>
    /// 暂停时间
    /// </summary>
    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;
        if (_timeData != null)
            _timeData.IsPaused = true;
        //EventBus.Instance.Publish(new TimePausedEvent());
        Debug.Log("[TimeManager] 时间已暂停");
    }

    /// <summary>
    /// 恢复时间
    /// </summary>
    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        if (_timeData != null)
            _timeData.IsPaused = false;
        //EventBus.Instance.Publish(new TimeResumedEvent());
        Debug.Log("[TimeManager] 时间已恢复");
    }
    #endregion

    #region 计时器管理
    /// <summary>
    /// 创建计时器（游戏内时间）。名称 持续时间 是否循环
    /// </summary>
    public GameTimer CreateTimer(string id, float duration, bool looping = false,Action onComplete = null)
    {
        if (_activeTimers.ContainsKey(id))
        {
            Debug.LogWarning($"[TimeManager] 计时器已存在，将被覆盖: {id}");
            RemoveTimer(id);
        }

        var timer = new GameTimer(id, duration, looping,onComplete);
        _activeTimers[id] = timer;
        //EventBus.Instance.Publish(new TimerStartedEvent(id, duration));
        return timer;
    }
    /// <summary>
    /// 从正在运行的计时器里面获取指定的计时器
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public GameTimer GetTimer(string id)
    {
        _activeTimers.TryGetValue(id, out var timer);
        return timer;
    }

    public void RemoveTimer(string id)
    {
        if (_activeTimers.TryGetValue(id, out var timer))
        {
            timer.ForceFinish();
            _activeTimers.Remove(id);
            Debug.Log($"[TimeManager] 移除计时器: {id}");
        }
    }

    public bool IsTimerFinished(string id)
    {
        if (_activeTimers.TryGetValue(id, out var timer))
            return timer.IsFinished;
        return true; // 不存在视为已完成
    }

    public float GetTimerRemaining(string id)
    {
        var timer = GetTimer(id);
        return timer != null ? timer.GetRemainingTime() : 0f;
    }
    #endregion

    #region 存档系统
    [Serializable]
    public class TimeSaveData
    {
        public float TotalSeconds;
        public bool IsPaused;
        public List<GameTimer.TimerSaveData> Timers;
    }
    /// <summary>
    /// 用于存档系统要存档时调用，将游戏的经过的总秒数（游戏内），是否已经暂停，还在运行中的Timer存入存档
    /// </summary>
    /// <returns></returns>
    public TimeSaveData GetSaveData()
    {
        var timerData = new List<GameTimer.TimerSaveData>();
        foreach (var kvp in _activeTimers)
        {
            timerData.Add(kvp.Value.GetSaveData());
        }

        return new TimeSaveData
        {
            TotalSeconds = _timeData.TotalSeconds,
            IsPaused = _timeData.IsPaused,
            Timers = timerData
        };
    }
    /// <summary>
    /// 从存档里面加载并赋值到现有的数据里面
    /// </summary>
    /// <param name="data"></param>
    public void LoadSaveData(TimeSaveData data)
    {
        if (data == null) return;

        _timeData.TotalSeconds = data.TotalSeconds;
        _timeData.IsPaused = data.IsPaused;
        _timeData.UpdateFromTotalSeconds(_timeData.TotalSeconds);

        // 重建计时器
        _activeTimers.Clear();
        if (data.Timers != null)
        {
            foreach (var timerData in data.Timers)
            {
                var timer = new GameTimer(timerData.TimerId, timerData.Duration, timerData.IsLooping)
                {
                    AutoTriggerOnComplete = timerData.AutoTriggerOnComplete,
                    StartGameTime = timerData.StartGameTime,
                    IsFinished = timerData.IsFinished
                };
                _activeTimers[timerData.TimerId] = timer;
            }
        }

        // 如果恢复时是暂停状态，应用暂停
        if (_timeData.IsPaused)
            Pause();
        else
            Resume();

        Debug.Log($"[TimeManager] 从存档恢复时间: {_timeData.ToString()}");
    }

    /// <summary>从 JSON 恢复时间状态。</summary>
    public void DeserializeState(string json, int version)
    {
        if (string.IsNullOrEmpty(json)) return;
        var data = JsonUtility.FromJson<TimeSaveData>(json);
        if (data != null) LoadSaveData(data);
    }

    /// <summary>新游戏时把时间归零、清空计时器。</summary>
    public void ResetState()
    {
        _timeData.TotalSeconds = 0f;
        _timeData.IsPaused = true;
        _timeData.UpdateFromTotalSeconds(0f);
        _activeTimers.Clear();
        _accumulator = 0f;
        _isPaused = true;
    }
    #endregion
}
