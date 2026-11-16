using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingState : IGameState
{
    /**
     * 切换场景或读取存档
     *      显示加载条，阻塞所有输入
     */
    public LoadingState(GameManager _gameManager) : base(_gameManager)
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
