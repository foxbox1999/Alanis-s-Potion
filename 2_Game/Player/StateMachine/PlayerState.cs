using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerState 
{
    protected PlayerStateMachine stateMachine; //状态机
    protected Player player; //玩家
    private string animBoolName; //动画状态名称
    protected Rigidbody2D rb;
    protected float xInput => InputManager.Instance.MoveVector.x;
    protected float yInput => InputManager.Instance.MoveVector.y;

    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;

    }

    public virtual void Enter() {
        foreach(var anim in player.anims)
        {
            anim.SetBool(animBoolName, true);
        }
        rb = player.rb;
    }

    public virtual void Update()
    {
        
    }

    public virtual void Exit()
    {
        foreach (var anim in player.anims)
        {
            anim.SetBool(animBoolName, false);
        }
    }
}
