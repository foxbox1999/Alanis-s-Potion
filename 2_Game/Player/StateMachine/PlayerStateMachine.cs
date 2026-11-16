using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine
{
    public PlayerState currentState { get; private set; }

    public void Initialize(PlayerState _Startstate)
    {
        currentState = _Startstate;
        currentState.Enter();
    }

    public void ChangeState(PlayerState _newState)
    {
        if (currentState != _newState)
        {
            currentState.Exit();
            currentState = _newState;
            currentState.Enter();
        }
    }

    public void CloseStateMachine()
    {
        if (currentState != null)
        {
            currentState.Exit();
            currentState = null;
        }
    }
}
