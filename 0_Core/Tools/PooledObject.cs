using UnityEngine;

/// <summary>标记对象池实例的来源 prefab（运行时内部使用，不序列化）。</summary>
public class PooledObject : MonoBehaviour
{
    /// <summary>该实例来自哪个 prefab。</summary>
    public GameObject Prefab { get; set; }
}