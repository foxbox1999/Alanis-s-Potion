using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("移动配置")]
    public float moveSpeed = 5f;

    [Header("移动组件")]
    public Rigidbody2D rb;
    public Animator[] anims;

    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerState IdleState { get; private set; }
    public PlayerState RightState { get; private set; }
    public PlayerState LeftState { get; private set; }

    private void Awake()
    {
        stateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, stateMachine, "Idle");
        RightState = new PlayerRightState(this, stateMachine, "Right");
        LeftState = new PlayerLeftState(this, stateMachine, "Left");
    }

    private void OnEnable()
    {
        stateMachine.Initialize(IdleState);
    }
    private void Update()
    {
        stateMachine.currentState?.Update();
    }

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.velocity = new Vector2(_xVelocity, _yVelocity);
    }

    private void OnDisable()
    {
        CloseAllState();
    }

    private void CloseAllState()
    {
        stateMachine.CloseStateMachine();
    }
    public void SetPlayMoveState(bool _playMove)
    {
        if (_playMove)
        {
            stateMachine.Initialize(IdleState);
        }
        else
        {
            SetVelocity(0, 0);
            stateMachine.CloseStateMachine();
        }
    }
}
