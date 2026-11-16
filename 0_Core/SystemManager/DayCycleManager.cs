using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayCycleManager : MonoSingleton<DayCycleManager>
{
    [SerializeField] private float _restHour = 23f;   // 23:50 触发
    [SerializeField] private float _restMinute = 50f;

    private TimeOfDay _currentTod = TimeOfDay.None;
    private bool _restTriggered;

    // 时间范围 → 时段
    public static TimeOfDay GetTimeOfDay(int hour)
    {
        if (hour >= 8 && hour < 17) return TimeOfDay.Day;
        if (hour >= 17 && hour < 19) return TimeOfDay.Dusk;
        return TimeOfDay.Night;   // 19~24 和 0~8 都是夜
    }

    private void Update()
    {
        if (TimeManager.Instance.IsPaused) return;
        var t = TimeManager.Instance.TimeData;

        // 1. 昼夜切换
        TimeOfDay tod = GetTimeOfDay(t.Hour);
        if (tod != _currentTod)
        {
            _currentTod = tod;
            EventBus.Instance.Publish(new TimeOfDayChangedEvent { TimeOfDay = tod });
        }

        // 2. 23:50 强制休息
        if (!_restTriggered && t.Hour >= _restHour && t.Minute >= _restMinute)
        {
            _restTriggered = true;
            StartCoroutine(ForcedRest());
        }
    }

    private IEnumerator ForcedRest()
    {
        // 1. 冻结玩家 + 暂停时间
        PlayerManager.Instance.FreezePlayer();
        TimeManager.Instance.Pause();

        // 2. 弹窗「过晚该休息了」，点确定后继续（用一个异步确认）
        bool confirmed = false;
        PopupManager.Instance.ShowConfirm("过晚该休息了", "夜深了，该休息了。", () => confirmed = true);
        yield return new WaitUntil(() => confirmed);

        // 3. 黑屏
        yield return ScreenFader.Instance.FadeToBlackAsync();

        // 4. 快进到次日 8:00（日界在 8 点）
        float add = SecondsToNext8AM(TimeManager.Instance.CurrentTotalSeconds);
        TimeManager.Instance.AddTime(add);

        // ★ 关闭其他 UI + 卸载玩家（避免残留 UI 和玩家乱动/误交互）
        UIManager.Instance.CloseAll();
        PlayerManager.Instance.UninstallPlayerObject();

        // 6. 恢复时间 + 亮屏

        _restTriggered = false;
        yield return ScreenFader.Instance.FadeFromBlackAsync();

        _ = UIManager.Instance.SwitchPanelAsync(
    openKey: UIID._mainmenu,
    openData: new UIPanelData { ClearStack = true },
    closeKey: null,
    duringBlack: null,
    fade: true
);
    }

    private float SecondsToNext8AM(float totalSec)
    {
        float day = Mathf.Floor(totalSec / 86400f);
        return (day + 1) * 86400f - totalSec;   // 次日 8:00（日界）减去当前
    }
}