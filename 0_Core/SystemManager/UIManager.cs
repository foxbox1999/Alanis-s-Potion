using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 管理器（门面模式 + 栈管理 + 对象池 + 异步加载 + 统一转场）
/// 三种界面：特殊根界面(Start/MainMenu/HUD) / 普通纯UI / 弹窗(不入栈、永不黑屏、只靠内部关闭)
/// 栈 = 导航历史，只有栈顶真正打开；切换关旧开新，返回重开上一个。
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    #region 配置
    [Header("Canvas 根节点")]
    [SerializeField] private Canvas _screenCanvas;
    [SerializeField] private Canvas _topCanvas;

    [Header("配置")]
    [SerializeField] private bool _debugMode = true;

    private Dictionary<string, BaseUIView> _activePanels = new Dictionary<string, BaseUIView>();
    private Stack<string> _panelStack = new Stack<string>();
    private List<string> _persistentPanels = new List<string>();

    private IEventBus _eventBus;
    private Dictionary<string, IDisposable> _subs = new Dictionary<string, IDisposable>();

    private Image _clickShield;
    private Coroutine _shieldCoroutine;
    private bool _transitioning;
    private readonly Dictionary<string, bool> _fadePrefCache = new();
    private static readonly HashSet<string> RootKeys = new() { UIID._start, UIID._mainmenu, UIID._hud };
    public bool IsTransitioning => _transitioning;
    #endregion

    #region 生命周期
    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
        CreateClickShield();
    }

    private void SubscribeEvents()
    {
        _eventBus = EventBus.Instance;
        SafeSubscribe<DialogueUIOpenRequestEvent>("DialogueUIOpenRequest", OnDialogueUIOpenRequest);
    }

    private void CreateClickShield()
    {
        if (_topCanvas == null) return;
        var go = new GameObject("[ClickShield]", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_topCanvas.transform, false);
        go.transform.SetAsLastSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;
        _clickShield = img;
        go.SetActive(false);
    }
    #endregion

    #region 事件响应
    private async void OnDialogueUIOpenRequest(DialogueUIOpenRequestEvent @event)
    {
        if (@event.NeedBlackFade) await ScreenFader.Instance.FadeToBlackAsync();

        DialogueView view = await OpenPanelAsync<DialogueView>(UIID._dialogue,
            new UIPanelData { CustomData = @event.StartID });

        _eventBus.Publish(new DialogueUIReadyEvent { View = view });

        if (@event.NeedBlackFade) await ScreenFader.Instance.FadeFromBlackAsync();
    }
    #endregion

    #region 对外接口
    // ===== 统一转场 =====
    /// <summary>黑屏 → 关旧 → 处理数据 → 开新 → 亮屏。所有界面切换都走这里。</summary>
    public async Task SwitchPanelAsync(
        string openKey,
        UIPanelData openData = null,
        string closeKey = null,
        Action duringBlack = null,
        bool? fade = null)
    {
        bool doFade = fade ?? (GetFadePref(openKey) || GetFadePref(closeKey));
        EnterTransition();
        try
        {
            if (doFade) await ScreenFader.Instance.FadeToBlackAsync();
            if (!string.IsNullOrEmpty(closeKey)) ClosePanel(closeKey);
            duringBlack?.Invoke();
            if (!string.IsNullOrEmpty(openKey)) await OpenPanelAsync<BaseUIView>(openKey, openData);
            UpdateStack(openKey);
        }
        finally
        {
            if (doFade) await ScreenFader.Instance.FadeFromBlackAsync();
            ExitTransition();
        }
    }

    private void UpdateStack(string openKey)
    {
        if (string.IsNullOrEmpty(openKey)) return;

        if (RootKeys.Contains(openKey))
        {
            // 根界面：关掉其它非弹窗面板（防残留），再清栈放栈底
            CloseAllNonPopupExcept(openKey);
            _panelStack.Clear();
            _panelStack.Push(openKey);
        }
        else if (_activePanels.TryGetValue(openKey, out var v) && !v.IsPopup)
        {
            _panelStack.Push(openKey);
        }
    }

    private void CloseAllNonPopupExcept(string keepKey)
    {
        var keys = new List<string>(_activePanels.Keys);
        foreach (var k in keys)
            if (k != keepKey && !_activePanels[k].IsPopup && !_activePanels[k].IsPersistent)
                ClosePanel(k);
    }

    /// <summary>
    /// 打开面板（无黑屏，纯开；栈由 SwitchPanelAsync 管）同步开
    /// </summary>
    /// <typeparam name="T">打开面板的面板view</typeparam>
    /// <param name="key">打开ui的名字</param>
    /// <param name="data">需要传递的数据</param>
    /// <returns></returns>
    public async Task<T> OpenPanelAsync<T>(string key, UIPanelData data = null) where T : BaseUIView
    {

        if (string.IsNullOrEmpty(key)) { LogError("面板Key不能为空"); return null; }

        if (_activePanels.TryGetValue(key, out var existingPanel))
        {
            if (existingPanel.IsSingleton) { BringToTop(key); existingPanel.Refresh(); return existingPanel as T; }
            else ClosePanel(key);
        }

        if (data != null && data.ClearStack) ClearStack();

        GameObject prefab = await AssetManager.Instance.LoadAssetAsync<GameObject>(ResourcePaths.UIPrefab(key));
        if (prefab == null) { LogError($"[UI] 未找到预制体: UI/{key}"); return null; }

        GameObject instance = PoolManager.Instance.Get(prefab);
        if (instance == null) { LogError($"从对象池获取实例失败: {key}"); return null; }

        if (instance.transform.parent != null) instance.transform.SetParent(null, false);

        T view = instance.GetComponent<T>();
        if (view == null) { LogError($"预制体缺少 {typeof(T).Name} 组件: {key}"); PoolManager.Instance.Return(instance); return null; }

        _fadePrefCache[key] = view.UseBlackFade;

        instance.transform.SetParent(GetCanvasParent(view.Layer), false);
        instance.transform.localScale = Vector3.one;
        _activePanels[key] = view;

        view.Open(data);

        if (view.IsPersistent) _persistentPanels.Add(key);
        return view;
    }
    /// <summary>
    /// 异步纯开
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    public async void OpenPanel(string key, UIPanelData data = null)
    {
        await OpenPanelAsync<BaseUIView>(key, data);
    }

    // ===== 关闭面板（无黑屏，同步；不碰栈） =====
    public void ClosePanel(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!_activePanels.TryGetValue(key, out var view)) return;
        _activePanels.Remove(key);
        if (view == null) return;

        if (view.IsPersistent) _persistentPanels.Remove(key);
        if (view.IsPopup) BlockInputFor(0.15f);

        view.Close();
        if (view.gameObject != null) PoolManager.Instance.Return(view.gameObject);
    }

    // ===== 返回（弹栈 + 关当前 + 重开上一个） =====
    public async void GoBack()
    {
        if (_panelStack.Count <= 1) { Log("已是根面板，无法返回"); return; }

        string currentKey = _panelStack.Pop();
        string previousKey = _panelStack.Peek();

        await SwitchPanelAsync(
            openKey: previousKey,
            closeKey: currentKey,
            duringBlack: null,
            fade: GetFadePref(currentKey) || GetFadePref(previousKey)
        );
    }

    // ===== 清栈 / 全关（都跳过弹窗） =====
    public void ClearStack()
    {
        List<string> keys = new List<string>();
        foreach (var kvp in _activePanels)
            if (!kvp.Value.IsPersistent && !kvp.Value.IsPopup)
                keys.Add(kvp.Key);
        foreach (var key in keys) ClosePanel(key);
        _panelStack.Clear();
    }

    public void CloseAll()
    {
        List<string> keys = new List<string>(_activePanels.Keys);
        foreach (var key in keys)
            if (!_activePanels[key].IsPersistent && !_activePanels[key].IsPopup)
                ClosePanel(key);
        _panelStack.Clear();
    }

    // ===== 查询 =====
    public T GetPanel<T>(string key) where T : BaseUIView
        => _activePanels.TryGetValue(key, out var v) ? v as T : null;

    public bool IsPanelOpen(string key) => _activePanels.ContainsKey(key);

    public void RefreshPanel(string key)
    {
        if (_activePanels.TryGetValue(key, out var view)) view.Refresh();
    }
    #endregion

    #region 私有核心
    private void EnterTransition()
    {
        _transitioning = true;
        GameManager.Instance?.EnterBusy();
        if (_clickShield != null) _clickShield.gameObject.SetActive(true);
    }

    private void ExitTransition()
    {
        _transitioning = false;
        GameManager.Instance?.ExitBusy();
        if (_clickShield != null) _clickShield.gameObject.SetActive(false);
    }

    private bool GetFadePref(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (_fadePrefCache.TryGetValue(key, out var v)) return v;
        if (_activePanels.TryGetValue(key, out var view))
        {
            _fadePrefCache[key] = view.UseBlackFade;
            return view.UseBlackFade;
        }
        return false;
    }

    public void BlockInputFor(float seconds)
    {
        if (_clickShield == null) return;
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
        _shieldCoroutine = StartCoroutine(ShieldRoutine(seconds));
    }

    private IEnumerator ShieldRoutine(float seconds)
    {
        _clickShield.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        _clickShield.gameObject.SetActive(false);
        _shieldCoroutine = null;
    }

    private Transform GetCanvasParent(UILayer layer)
    {
        return layer switch
        {
            UILayer.TopLayer => _topCanvas.transform,
            _ => _screenCanvas.transform
        };
    }

    private void BringToTop(string key)
    {
        if (_activePanels.TryGetValue(key, out var view))
            view.transform.SetAsLastSibling();
    }
    #endregion

    #region Tools
    private void SafeSubscribe<T>(string key, Action<T> action) where T : EventBase
    {
        if (_eventBus == null) { Log("事件总线初始化未成功，订阅失败"); return; }
        if (_subs == null) _subs = new Dictionary<string, IDisposable>();
        if (_subs.TryGetValue(key, out var oldSub)) oldSub?.Dispose();
        _subs[key] = _eventBus.Subscribe(action);
    }
    #endregion

    #region 事件监听
    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        CloseAll();
    }
    #endregion

    #region 日志
    private void Log(string message) { if (_debugMode) Debug.Log($"[UIManager] {message}"); }
    private void LogError(string message) { Debug.LogError($"[UIManager] {message}"); }
    #endregion

    #region 清理
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        foreach (var sub in _subs.Values) sub?.Dispose();
        _subs.Clear();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _activePanels.Clear();
        _persistentPanels.Clear();
        _panelStack.Clear();
    }
    #endregion
}