using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 显示开始游戏界面、设置界面  不加载游戏场景，只渲染UI
/// </summary>
public class StartState : IGameState
{
    /// <summary>
    /// 显示开始游戏界面、设置界面  不加载游戏场景，只渲染UI
    /// </summary>
    public StartState(GameManager _gameManager) : base(_gameManager)
    {
    }
    public override void Enter()
    {
        base.Enter();
        TimeManager.Instance?.Pause();
        //1.打开Start界面
        _ = UIManager.Instance.SwitchPanelAsync(
            UIID._start,
            duringBlack: () =>
            {
                //2.如果当前SaveManager有激活的存档，将该存挡保存并重置至初始状态
                if (SaveManager.Instance.CurrentSlot != -1)
                    SaveManager.Instance.EndSession(true);
            },
            fade: true
            );
    }
    public override void Update()
    {
        base.Update();
    }
    public override void Exit()
    {
        base.Exit();
    }
}
