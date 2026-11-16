using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class AssetManager : MonoSingleton<AssetManager>
{
    private IAssetLoader _loader;

    protected override void Awake()
    {
        base.Awake();
        // 根据宏或配置选择底层实现
        //#if UNITY_EDITOR || USE_RESOURCES
        _loader = new ResourcesLoader();
        //#else
        //        _loader = new AddressableLoader();
        //        Debug.Log("[AssetManager] 使用 AddressableLoader");
        //#endif
    }

    // ===== 对外公开接口 =====
    /// <summary>
    /// 异步加载资源（泛型）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[AssetManager] key 为空");
            return null;
        }

        var asset = await _loader.LoadAssetAsync<T>(key);
        if (asset != null)
        {
            // 发布加载完成事件（可选）
            //EventBus.Instance.Publish(new AssetLoadedEvent(key, asset));
        }
        return asset;
    }

    /// <summary>
    /// 异步批量加载多个同类型资源。
    /// 并发加载，全部完成后返回结果列表（顺序与传入的 keys 一致）。
    /// 加载失败的资源在返回列表中对应位置为 null。
    /// </summary>
    /// <typeparam name="T">资源类型，如 GameObject、AudioClip、Sprite 等</typeparam>
    /// <param name="keys">资源路径集合</param>
    /// <returns>资源列表，顺序与 keys 一致；失败项为 null</returns>
    public async Task<T[]> LoadAllAssetsAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[AssetManager] key 为空");
            return null;
        }
        var asset = await _loader.LoadAllAssetAsync<T>(key);
        if (asset != null)
        {
            // 发布加载完成事件（可选）
            //EventBus.Instance.Publish(new AssetLoadedEvent(key, asset));
            return asset;
        }
        return null;
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="key"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public async Task<SceneInstance> LoadSceneAsync(string key, LoadSceneMode mode = LoadSceneMode.Single)
    {
        return await _loader.LoadSceneAsync(key, mode);
    }

    /// <summary>
    /// 释放资源（用于 Addressables 或手动缓存）
    /// </summary>
    /// <param name="key"></param>
    public void ReleaseAsset(string key)
    {
        _loader.ReleaseAsset(key);
    }

    /// <summary>
    /// 清空所有缓存（场景切换时调用）
    /// </summary>
    public void ClearCache()
    {
        _loader.ClearCache();
    }

    // ===== 生命周期管理 =====
    protected override void OnDestroy()
    {
        ClearCache();
    }

}
