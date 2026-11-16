using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ResourcesLoader : IAssetLoader
{
    private Dictionary<string, UnityEngine.Object> _cache = new Dictionary<string, UnityEngine.Object>();

    public async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        if (_cache.TryGetValue(key, out var cached) && cached != null)
            return cached as T;
        // Resources.LoadAsync 本质是异步，但为了统一接口用 Task 包装
        var request = Resources.LoadAsync<T>(key);
        //请求未完成时，等待下一帧继续
        while (!request.isDone)
            await Task.Yield();

        if (request.asset != null)
        {
            _cache[key] = request.asset;
            return request.asset as T;
        }
        Debug.LogError($"[ResourcesLoader] 未找到资源: {key}");
        return null;
    }

    public async Task<T[]> LoadAllAssetAsync<T>(string key) where T : Object
    {
        var assets = Resources.LoadAll<T>(key);

        if (assets == null || assets.Length == 0)
        {
            Debug.LogWarning($"[ResourcesLoader] 文件夹为空或不存在: {key}");
            return new T[0];
        }

        // 缓存（可选）
        foreach (var asset in assets)
        {
            string cacheKey = $"{key}/{asset.name}";
            if (!_cache.ContainsKey(cacheKey))
                _cache[cacheKey] = asset;
        }

        // 保持接口异步语义
        await Task.Yield();
        return assets;
    }

    public Task<SceneInstance> LoadSceneAsync(string key, LoadSceneMode mode)
    {
        // Resources 不支持直接加载场景，需用 SceneManager，这里做兼容
        SceneManager.LoadScene(key, mode);
        return Task.FromResult(new SceneInstance());
    }

    public void ReleaseAsset(string key)
    {
        if (_cache.ContainsKey(key))
        {
            // Resources.UnloadAsset 只能卸载单个 asset，复杂情况建议用 Resources.UnloadUnusedAssets
            Resources.UnloadAsset(_cache[key]);
            _cache.Remove(key);
        }
    }

    public void ClearCache()
    {
        foreach (var item in _cache.Values) // 假设 cachedObjects 是 List<UnityEngine.Object>
        {
            if (item is GameObject || item is Component)
            {
                // 不能 UnloadAsset，跳过或 Destroy
                // 如果是实例化出来的，应由 PoolManager 负责销毁
                continue;
            }
            else
            {
                Resources.UnloadAsset(item);
            }
        }
        _cache.Clear();
        Resources.UnloadUnusedAssets();
    }
}