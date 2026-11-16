using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 游戏内计时器（炼药、Buff、作物生长等）,炼药的结束通过回调onComplete触发
/// </summary>
public class GameTimer
{
    /**
     *  炼药使用示列：
     *        var timer = TimeManager.Instance.CreateTimer(timerId, durationSeconds, looping: false);
     *              
     * 
     */
    public string TimerId { get; private set; }          // 唯一标识
    public float StartGameTime { get; set; }     // 开始时的游戏总秒数
    public float Duration { get; private set; }          // 持续时间（秒）
    public bool IsFinished { get; set; }
    public bool IsLooping { get; private set; }
    /// <summary>
    /// 是否自动触发完成回调（true=到期自动触发，false=由外部查询触发）
    /// </summary>
    public bool AutoTriggerOnComplete { get; set; } = true;
    public int LoopCount { get; private set; } = 0;              // 循环次数（仅在 IsLooping 为 true 时有效）
    public Action OnComplete { get; set; }
    public Action<float> OnTick { get; set; }            // 每秒回调，传入剩余时间

    public GameTimer(string id, float duration, bool looping = false,Action onComplete = null)
    {
        TimerId = id;
        Duration = duration;
        IsLooping = looping;
        IsFinished = false;
        StartGameTime = TimeManager.Instance.CurrentTotalSeconds;
        if (onComplete != null) {
            OnComplete = onComplete;
        }
    }

    /// <summary>
    /// 获取剩余时间（秒）
    /// </summary>
    public float GetRemainingTime()
    {
        if (IsFinished) return 0;
        float elapsed = (TimeManager.Instance.CurrentTotalSeconds - StartGameTime)/(TimeManager.Instance.GameTimeScale); //显示内已经流逝的时间
        return Mathf.Max(0, Duration - elapsed);
    }

    /// <summary>
    /// 获取进度 0~1
    /// </summary>
    public float GetProgress()
    {
        if (Duration <= 0) return 1;
        float elapsed = (TimeManager.Instance.CurrentTotalSeconds - StartGameTime) / TimeManager.Instance.GameTimeScale;
        return Mathf.Clamp01(elapsed / Duration);
    }

    /// <summary>
    /// 更新计时器状态（由 TimeManager 每帧调用），返回真表示计时器已经结束,就要移除
    ///     isloop isFinished AutoTriggerOnComplete
    ///       x       √         √                   true    计时器触发一次OnComplete
    ///       x        √        x                   false  等待外部将计时器的AutoTriggerOnCompletes设为true即可返回true,然后再触发一次OnComplete
    ///       x        x         √               false 计时器都没完成返回什么玩意 
    ///       x      x         x                false 什么都没有发生 
    ///       √       x         x                false 什么都没有 
    ///       √       x         √                  false   每次计时器完成一次的时候会触发一次OnComplete
    ///       √       √         √               false 此时会将finished置为false，并且每次计时器完成一次的时候会触发一次OnComplete
    ///       √       √         x              false 此时会将finished置为false并且不会触发任何事情，直到外部将计时器的AutoTriggerOnCompletes设为true然后回触发计时器完成事件根据执行的次数来发放结果
    ///       总结：
    ///         开启AutoTriggerOnComplete,但未开启isloop时，计时器结束就会被移除并触发一次OnComplete
    ///         开启AutoTriggerOnComplete,并且开启isloop时,计时器会一直循环并且每次倒计时结束会触发一次OnComplete，直到停止循环后移除
    ///         关闭AutoTriggerOnComplete,但未开启isLoop时，计时器不会被移除，直到外部修改AutoTriggerOnComplete(此时Finished标为true），然后才触发onComplete
    ///         关闭AutoTriggerOnComplete,并且开启isloop时。计时器不会被移除，直到外部修改AutoTriggerOnComplete,触发onComplete外部根据需求是否选择循环次数参与onCompleted当前结算，如果参与就计算完毕后将循环次数置为0，然后回到情况2
    /// </summary>
    public bool Update()
    {
        if (IsFinished) return true;

        float remaining = GetRemainingTime();
        OnTick?.Invoke(remaining);

        if (remaining <= 0)
        {
            IsFinished = true;
            if (AutoTriggerOnComplete)
            {
                OnComplete?.Invoke();
                //EventBus.Instance.Publish(new TimerCompletedEvent(TimerId));
            }
            else
            {
                IsFinished = false;
            }
            if (IsLooping)
            {
                LoopCount++;
                // 循环计时器：重置开始时间
                StartGameTime = TimeManager.Instance.CurrentTotalSeconds;
                IsFinished = false;
                return false;
            }
            if (IsFinished) 
                return true;
        }
        return false;
    }

    /// <summary>
    /// 手动触发完成回调（由外部调用，用于“拉取”模式）
    /// </summary>
    public void TriggerLoopComplete()
    {
        AutoTriggerOnComplete = true;
        IsLooping = false;
    }
    public void TriggerComplete()
    {
        AutoTriggerOnComplete = true;
    }
    /// <summary>
    /// 强制结束计时器
    /// </summary>
    public void ForceFinish()
    {
        if (!IsFinished)
        {
            IsFinished = true;
            OnComplete?.Invoke();
        }
    }

    /// <summary>
    /// 序列化数据（用于存档）
    /// </summary>
    public TimerSaveData GetSaveData()
    {
        return new TimerSaveData
        {
            TimerId = TimerId,
            StartGameTime = StartGameTime,
            AutoTriggerOnComplete = AutoTriggerOnComplete,
            Duration = Duration,
            IsLooping = IsLooping,
            IsFinished = IsFinished
        };
    }

    [Serializable]
    public class TimerSaveData
    {
        public string TimerId;
        public float StartGameTime;
        public float Duration;
        public bool AutoTriggerOnComplete;
        public bool IsLooping;
        public bool IsFinished;
    }
}