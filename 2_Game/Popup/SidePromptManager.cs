using UnityEngine;

/// <summary>侧边提示管理器：多个 toast 同时弹，各自滑入/停留/缩回回收池。</summary>
public class SidePromptManager : MonoSingleton<SidePromptManager>
{
    [SerializeField] private GameObject _prefab;       // SidePrompt 预制件
    [SerializeField] private RectTransform _parent;    // 顶层 Canvas

    public void Show(string message)
    {
        if (_prefab == null || _parent == null) return;
        GameObject instance = PoolManager.Instance.Get(_prefab);
        instance.transform.SetParent(_parent, false);
        instance.SetActive(true);
        instance.GetComponent<SidePrompt>().Show(message);
    }
}