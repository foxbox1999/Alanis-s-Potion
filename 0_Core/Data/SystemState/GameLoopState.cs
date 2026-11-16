using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoopState : IGameState
{
    /**
     * 玩家进入家园/地图场景
     *      核心容器状态，内部包含子状态3
     *      子状态名称	玩家移动	UI点击交互	后台计时（炼药）	适用场景
     *      FreeRoamSubState	✅ 启用	✅ 启用（HUD按钮）	✅ 正常流逝	玩家在场景中散步，偶尔点采集按钮
     *      ullUISubState	❌ 禁用	✅ 启用（全屏面板）	✅ 正常流逝	打开采集、看病、炼药、养成等全屏大界面
     *      DialogSubState	❌ 禁用	⚠️ 仅限对话选项	✅ 正常流逝（除非剧情特别要求）	剧情对话弹出，覆盖全屏
     */
    /// <summary> 
    /// 玩家进入家园/地图场景
    /// </summary>
    /// <remarks>
    /// 核心容器状态，内部包含子状态3
    /// FreeRoamSubState	✅ 启用	✅ 启用（HUD按钮）	✅ 正常流逝	玩家在场景中散步，偶尔点采集按钮
    /// FullUISubState	❌ 禁用	✅ 启用（全屏面板）	✅ 正常流逝	打开采集、看病、炼药、养成等全屏大界面
    /// DialogSubState	❌ 禁用	⚠️ 仅限对话选项	✅ 正常流逝（除非剧情特别要求）	剧情对话弹出，覆盖全屏
    /// </remarks>
    /// 

    public SubState Current { get; private set; }
    private SubState _beforeDialog;

    public GameLoopState(GameManager _gameManager) : base(_gameManager)
    {
    }
    public override void Enter()
    {
        base.Enter();
        //TimeManager.Instance?.Resume();
    }
    public override void Update()
    {
        base.Update();
    }
    public override void Exit()
    {
        base.Exit();
    }
    #region 子状态
    /// <summary>
    /// 进入游戏场景界面 启用UI点击交互 启用（HUD按钮）	正常流逝	玩家在场景中散步，偶尔点采集按钮
    /// </summary>
    public void EnterFreeRoam()
    {
        Current = SubState.FreeRoam;
        PlayerManager.Instance?.ThawPlayer();
        _ = UIManager.Instance?.SwitchPanelAsync(UIID._hud, closeKey: null, fade: true);
        PlayerDataManager.Instance?.SetLastSessionState(SessionState.FreeRoam);
        EventBus.Instance.Publish(new SubStateChangedEvent { SubState = SubState.FreeRoam });
    }
    /// <summary>
    /// 进入全屏ui界面 启用（全屏面板）	正常流逝	打开采集、看病、炼药、养成等全屏大界面
    /// </summary>
    public void EnterFullUI()
    {
        Current = SubState.FullUI;
        PlayerManager.Instance?.FreezePlayer();
        _ = UIManager.Instance?.SwitchPanelAsync(openKey: null, closeKey: UIID._hud, fade: true);
        EventBus.Instance.Publish(new SubStateChangedEvent { SubState = SubState.FullUI });
    }
    /// <summary>
    /// 进入对话状态 UI点击交互仅限对话选项 后台计时（炼药）正常流逝（除非剧情特别要求）	剧情对话弹出，覆盖全屏
    /// </summary>
    public void EnterDialog()
    {
        _beforeDialog = Current;
        Current = SubState.Dialog;
        PlayerManager.Instance?.FreezePlayer();
        _ = UIManager.Instance?.SwitchPanelAsync(openKey: null, closeKey: "Hud", fade: true);
        EventBus.Instance.Publish(new SubStateChangedEvent { SubState = SubState.Dialog });
    }
    /// <summary>
    /// 退出对话状态
    /// </summary>
    public void ExitDialog()
    {
        Current = _beforeDialog;
        //返回到上一个状态
        EventBus.Instance.Publish(new SubStateChangedEvent { SubState = Current });
        if (_beforeDialog == SubState.FreeRoam)
        {
            PlayerManager.Instance?.ThawPlayer();
            _ = UIManager.Instance?.SwitchPanelAsync(UIID._hud, closeKey: null, fade: true);
        }
    }
    #endregion
}
