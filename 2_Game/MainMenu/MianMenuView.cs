using UnityEngine;
using UnityEngine.UI;

public class MianMenuView : BaseUIView
{
    [SerializeField] private Button Gather;
    [SerializeField] private Button Rasing;
    [SerializeField] private Button Diagnose;
    [SerializeField] private Button Cauldron;
    [SerializeField] private Button Tp;
    [SerializeField] private Button BackPack;
    private bool _sleeping = false;

    protected override void OnOpen()
    {
        base.OnOpen();
        RefreshDayGate();
        if (PlayerDataManager.Instance.PlotStep == 9)
        {
            TimeManager.Instance.Pause();
            PlotDirector.Instance.OnHomeGuideDone();
        }
        if (PlayerDataManager.Instance.PlotStep == 10)
        {
            TimeManager.Instance.Pause();
            PlotDirector.Instance.Advance();
        }
        PlayerDataManager.Instance?.SetLastSessionState(SessionState.MainMenu);
        AudioManager.Instance.PlayBGMByTime(AudioPaths.BGM_MainMenuDay, AudioPaths.BGM_MainMenuNight);
    }

    protected override void OnRefresh()
    {
        base.OnRefresh();
        RefreshDayGate();
    }

    // 新一天门控：第一天养子禁用；之后按钮都可点（点击时判断），只控时间流动
    private void RefreshDayGate()
    {
        int day = WorldManager.Instance.CurrentDay;
        bool firstDay = day == 0;

        Rasing.interactable = !firstDay;   // ★ 第一天养子直接禁用（不能点）

        // 其它按钮始终可点（未安排时点击会弹侧边提示）
        Gather.interactable = true;
        Diagnose.interactable = true;
        Cauldron.interactable = true;
        BackPack.interactable = true;
        Tp.interactable = true;

        if (firstDay || ChildManager.Instance.isDayTaskPlan)
            TimeManager.Instance.Resume();
        else
            TimeManager.Instance.Pause();
    }

    /// <summary>养子门控提示：已安排 / 未安排。</summary>
    private void IsRasingOver(bool arranged)
    {
        if (arranged)
        {
            // 养子已安排 → 点击养子弹提示，进不了养子面板
            PopupManager.Instance.ShowSidePrompt("养子任务已经安排好了，无须再次安排");
        }
        else
        {
            // 养子未安排 → 点击其它按钮弹提示，先去养子
            PopupManager.Instance.ShowSidePrompt("请先安排养子任务，才能继续游戏");
        }
    }

    // 养子：已安排 → 提示；未安排 → 打开（第一天按钮已禁用，不会走到这）
    public void onRasingClick()
    {
        if (ChildManager.Instance.isDayTaskPlan)
        {
            IsRasingOver(true);
            return;
        }
        _ = UIManager.Instance.SwitchPanelAsync(UIID._raising, closeKey: UIID._mainmenu, fade: true);
    }

    // 其它入口：非第一天且未安排 → 提示；否则正常打开
    public void onGatherClick() => TryOpen(UIID._gather);
    public void onDiagnoseClick() => TryOpen(UIID._clinic);
    public void onCauldronClick() => TryOpen(UIID._alchemy);
    public void onBackPackClick() => TryOpen(UIID._backpack);
    public void onTpClick() => TryOpen(UIID._wizardIsland);

    private void TryOpen(string key)
    {
        if (WorldManager.Instance.CurrentDay > 0 && !ChildManager.Instance.isDayTaskPlan)
        {
            IsRasingOver(false);
            return;
        }
        if(key == UIID._backpack)
        {
            _ = UIManager.Instance.SwitchPanelAsync(
             openKey: UIID._backpack,
             openData: new InventoryPanelData { Context = InventoryContext.View },
             closeKey: UIID._hud,
             duringBlack: null,
             fade: true
         );
            return;
        }
        Debug.Log($"[栈] 打开 {key} ");   // 临时
        _ = UIManager.Instance.SwitchPanelAsync(key, closeKey: UIID._mainmenu, fade: true);
    }

    // 休息 → 跳到次日 8:00
    public async void onSleep()
    {
        if (_sleeping) return;   // ★ 防连点
        _sleeping = true;

        await ScreenFader.Instance.FadeToBlackAsync();

        float day = Mathf.Floor(TimeManager.Instance.CurrentTotalSeconds / 86400f);
        TimeManager.Instance.AddTime((day + 1) * 86400f - TimeManager.Instance.CurrentTotalSeconds);
        RefreshDayGate();

        await ScreenFader.Instance.FadeFromBlackAsync();

        _sleeping = false;
    }

    public void onBackStart()
    {
        GameManager.Instance.ChangeState(GameManager.Instance.StartState);
    }
}