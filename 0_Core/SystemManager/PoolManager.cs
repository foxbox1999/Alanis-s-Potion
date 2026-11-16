using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池管理器（按 prefab 分池）。
/// Get：池里有实例则复用，没有则 Instantiate 并打上来源标记。
/// Return：回收实例（SetActive(false) 挂回池根下）。
/// 非本池产出的实例（例如直接 Instantiate 出来的）会直接 Destroy，避免泄漏。
/// </summary>
public class PoolManager : MonoSingleton<PoolManager>
{
    private readonly Dictionary<GameObject, Pool> _pools = new Dictionary<GameObject, Pool>();
    private Transform _root;

    protected override void Awake()
    {
        base.Awake();
        var rootGo = new GameObject("[PoolManager-Root]");
        rootGo.transform.SetParent(transform, false);
        _root = rootGo.transform;
    }

    /// <summary>获取一个实例（池中有则复用，无则新建）。</summary>
    /// <param name="prefab">预制体。</param>
    /// <param name="parent">可选父节点；不传则挂到池根下，由调用方稍后再挂。</param>
    public GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[PoolManager] Get 的 prefab 为 null。");
            return null;
        }

        var pool = GetOrCreatePool(prefab);
        GameObject instance = pool.Get();
        if (instance == null)
            instance = Create(prefab);

        instance.transform.SetParent(parent != null ? parent : _root, false);
        instance.SetActive(true);
        return instance;
    }

    /// <summary>回收实例到对应池。</summary>
    /// <param name="instance">实例对象。</param>
    public void Return(GameObject instance)
    {
        if (instance == null) return;

        var marker = instance.GetComponent<PooledObject>();
        if (marker == null)
        {
            // 不是池产出的实例，无法定位来源，直接销毁避免泄漏
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(_root, false);
        GetOrCreatePool(marker.Prefab).Return(instance);
    }

    /// <summary>预热：预创建 count 个实例入池。</summary>
    /// <param name="prefab">预制体。</param>
    /// <param name="count">预热数量。</param>
    public void Warm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        var pool = GetOrCreatePool(prefab);
        for (int i = 0; i < count; i++)
            pool.Return(Create(prefab));
    }

    /// <summary>清空某个 prefab 的池。</summary>
    /// <param name="prefab">预制体。</param>
    public void ClearPool(GameObject prefab)
    {
        if (prefab != null && _pools.TryGetValue(prefab, out var pool))
        {
            pool.Clear();
            _pools.Remove(prefab);
        }
    }

    /// <summary>清空所有池。</summary>
    public void ClearAll()
    {
        foreach (var pool in _pools.Values) pool.Clear();
        _pools.Clear();
    }

    protected override void OnDestroy()
    {
        ClearAll();
        base.OnDestroy();
    }

    /// <summary>取或建某个 prefab 的池。</summary>
    private Pool GetOrCreatePool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var pool))
        {
            pool = new Pool();
            _pools[prefab] = pool;
        }
        return pool;
    }

    /// <summary>新建实例并打上来源标记。</summary>
    private GameObject Create(GameObject prefab)
    {
        var instance = Instantiate(prefab);
        var marker = instance.GetComponent<PooledObject>();
        if (marker == null) marker = instance.AddComponent<PooledObject>();
        marker.Prefab = prefab;
        instance.SetActive(false);
        instance.transform.SetParent(_root, false);
        return instance;
    }

    /// <summary>单个 prefab 的池。</summary>
    private class Pool
    {
        private readonly Queue<GameObject> _queue = new Queue<GameObject>();

        public GameObject Get() => _queue.Count > 0 ? _queue.Dequeue() : null;
        public void Return(GameObject instance) => _queue.Enqueue(instance);
        public void Clear() { while (_queue.Count > 0) Destroy(_queue.Dequeue()); }
    }
}