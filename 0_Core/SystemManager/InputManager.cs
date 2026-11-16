using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoSingleton<InputManager>
{
    private PlayerControls _controls;
    /// <summary>
    /// 获取玩家的移动向量
    /// </summary>
    public Vector2 MoveVector { get; private set; }
    // 3. 事件（用于处理跳跃、交互等一次性操作）
    /// <summary>
    /// 当玩家按下交互键时触发的事件
    /// </summary>
    public event Action OnInteractPerformed;
    protected override void Awake()
    {
        base.Awake();
        _controls = new PlayerControls();
        _controls.Player.Enable();

        _controls.Player.Interact.performed += ctx => OnInteractPerformed?.Invoke();
    }
    private void Update()
    {
        MoveVector = _controls.Player.Move.ReadValue<Vector2>();
    }
    /// <summary>
    /// 切换到 Player 模式（比如在场景中移动时）
    /// </summary>
    public void EnablePlayerActionMap()
    {
        _controls.UI.Disable();
        _controls.Player.Enable();
    }
    /// <summary>
    /// 切换到 UI 模式（比如打开背包、对话时）
    /// </summary>
    public void EnableUIActionMap()
    {
        _controls.Player.Disable();  // 禁用移动
        _controls.UI.Enable();       // 启用 UI 导航（上下左右选按钮）
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 释放资源
        _controls?.Dispose();
    }
    /// <summary>
    /// 注册交互事件
    /// </summary>
    /// <param name="action"></param>
    public void RegisterInteract(Action action)
    {
        OnInteractPerformed += action;
    }
    /// <summary>
    /// 注销交互事件
    /// </summary>
    /// <param name="action"></param>
    public void UnregisterInteract(Action action)
    {
        OnInteractPerformed -= action;
    }
}
