using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausedState : IGameState
{
    /**
    * 按ESC打开暂停菜单
    *      唯一真正暂停时间的地方（炼药计时在此处停止）
    */
    public PausedState(GameManager _gameManager) : base(_gameManager)
    {
    }
    public override void Enter()
    {
        base.Enter();

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
