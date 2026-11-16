using System;
using UnityEngine;

/// <summary>
/// UI面板基类（所有面板的根）
/// 职责：只负责展示和控件绑定，不处理业务逻辑
/// </summary>
public abstract class BaseUIView : MonoBehaviour
{
    [Header("面板配置")]
    [Tooltip("ui打开的key，预制件名字应该和这个一致")]
    [SerializeField] protected string _panelKey;          // 唯一标识
    [SerializeField] protected UILayer _layer = UILayer.ScreenUI;  // 所属层级
    [Tooltip("是否是单列，同一时间只能存在一个")]
    [SerializeField] protected bool _isSingleton = true;  // 是否单例（同一时间只能存在一个）
    [SerializeField] protected bool _isPopup = false;     // 是否为弹窗（不压入栈）

    [Tooltip("是否场景切换时保留")]
    [SerializeField] protected bool _isPersistent = false; // 是否场景切换时保留

    [Header("转场配置")]
    [SerializeField] protected bool _useBlackFade = true;   // 【新增】是否用黑屏转场（弹窗自动忽略）

    // ===== 公共属性 =====
    public string PanelKey => _panelKey;
    public UILayer Layer => _layer;
    public bool IsSingleton => _isSingleton;
    public bool IsPopup => _isPopup;
    public bool IsPersistent => _isPersistent;
    public bool IsOpen { get; private set; }
    public UIPanelData Data { get; protected set; }

    // 【新增】是否用黑屏转场：弹窗永不黑屏，其它看 _useBlackFade 配置
    public bool UseBlackFade => _useBlackFade && !IsPopup;

    // ===== 事件回调 =====
    public Action OnPanelOpened;
    public Action OnPanelClosed;
    public Action OnPanelRefreshed;

    //=============运行时数据==================
    protected virtual bool OnBack() => false;


    #region 生命周期
    /// <summary>
    /// 打开面板（由 UIManager 调用）
    /// </summary>
    public virtual void Open(UIPanelData data = null)
    {
        if (IsOpen) return;

        Data = data ?? new UIPanelData();
        Data.OnUIOpen?.Invoke();
        OnOpen();
        gameObject.SetActive(true);
        OnPanelOpened?.Invoke();
        IsOpen = true;
    }

    /// <summary>
    /// 关闭面板（由 UIManager 调用）
    /// </summary>
    public virtual void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        OnClose();
        OnPanelClosed?.Invoke();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 刷新面板数据（外部数据变化时调用）
    /// </summary>
    public virtual void Refresh()
    {
        if (!IsOpen) return;
        OnRefresh();
        OnPanelRefreshed?.Invoke();
    }

    // ===== 子类可重写 =====
    protected virtual void OnOpen() { }
    protected virtual void OnClose() { }
    protected virtual void OnRefresh() { }
    #endregion

    #region 工具方法
    /// <summary>
    /// 在子物体中查找组件
    /// </summary>
    protected T GetComponentInChild<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    /// <summary>
    /// 获取按钮（常用快捷方式）
    /// </summary>
    protected UnityEngine.UI.Button GetButton(string childName)
        => GetComponentInChild<UnityEngine.UI.Button>(childName);

    /// <summary>
    /// 获取文本（常用快捷方式）
    /// </summary>
    protected UnityEngine.UI.Text GetText(string childName)
        => GetComponentInChild<UnityEngine.UI.Text>(childName);

    /// <summary>
    /// 获取图片（常用快捷方式）
    /// </summary>
    protected UnityEngine.UI.Image GetImage(string childName)
        => GetComponentInChild<UnityEngine.UI.Image>(childName);

    /// <summary>
    /// 关闭自己（供按钮绑定调用）
    /// </summary>
    public void CloseSelf()
    {
        UIManager.Instance.ClosePanel(_panelKey);
    }

    /// <summary>
    /// 返回上一级（供返回按钮绑定）
    /// </summary>
    public void GoBack()
    {
        if (OnBack()) return;
        Debug.Log("UIManagerback");
        UIManager.Instance.GoBack();
    }
    #endregion
}
