using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IGameState
{
    protected GameManager _manager;
    public IGameState(GameManager _gameManager)
    {
        _manager = _gameManager;
    }
    public virtual void Enter()
    {
        //// 隐藏全屏遮罩（如果有）
        //UIManager.Instance.HideBlockMask();
        // 恢复时间（如果之前因剧情暂停）
        Time.timeScale = 1f;
    }
    public virtual void Update() { }
    public virtual void Exit() {
        // 隐藏全屏遮罩（如果有）
        //UIManager.Instance.LoadBlockMask();
    }
}
