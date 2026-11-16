using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BootState : IGameState
{
    /**
     * 游戏启动，加载初始界面
     *      显示Logo、检查存档、加载最低配置
     */
    public BootState(GameManager _gameManager) : base(_gameManager)
    {
    }
    public override void Enter()
    {
        base.Enter();
        //显示Logo
    }
    public override void Update()
    {
        base.Update();
        //检查存档、加载最低配置

        //切换到MainMenu状态
        _manager.ChangeState(_manager.StartState);
    }
    public override void Exit()
    {
        base.Exit();
    }
}
