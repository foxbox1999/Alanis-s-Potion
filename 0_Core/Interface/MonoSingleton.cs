using UnityEngine;

/// <summary>
/// 泛型单例基类（支持场景切换销毁/保留） Awake()：赋值单列对象并将当前 GameObject 脱离场景销毁链
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    // --- 静态保护变量 ---
    private static T _instance;
    private static bool _isQuitting = false; // 防止退出游戏时反复报错

    // --- 公共属性 ---
    public static T Instance
    {
        get
        {
            // 1. 实例已建立直接返回（退出存档时对象还活着，能正常访问）
            if (_instance != null)
                return _instance;

            // 1. 退出保护：防止在 Editor 停止运行时产生 MissingReference 错误风暴
            if (_isQuitting)
            {
                Debug.LogWarning($"[{typeof(T).Name}] 游戏正在退出，拒绝访问实例。");
                return null;
            }

            // 2. 懒加载（Lazy Initialization）：如果丢失则自动在场景中查找
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
            }
            return _instance;
        }
    }

    // --- 子类可重写的开关（默认开启跨场景保留）---


    // --- 核心生命周期 ---
    /// <summary>
    /// Awake 方法在 MonoBehaviour 被实例化时调用，用于初始化单例实例并处理跨场景保留逻辑，也用于不受其他脚本访问的保护逻辑
    ///       订阅事件、初始化数据等操作也可以在 Awake 中进行
    /// </summary>
    protected virtual void Awake()
    {
        // 单例双重校验（防止场景中有重复的 Manager 预制体）
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[{typeof(T).Name}] 重复实例被销毁。");
            DestroyImmediate(this.gameObject);
            return;
        }
        // 赋值并处理跨场景
        if (_instance == this) return;
        _instance = this as T;
    }
    protected virtual void Start()
    {
        // 可选：在 Start 中进行一些初始化操作
    }
    // 当场景切换或应用退出时触发
    protected virtual void OnDestroy()
    {
        // 重要：当实例被销毁时，必须清空静态引用，否则其他脚本访问 Instance 会返回已销毁的对象
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // 应用退出时的全局标记
    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}