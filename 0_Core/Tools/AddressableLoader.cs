using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class AddressableLoader : IAssetLoader
{
    // 存储所有正在使用的操作句柄，用于释放
    private Dictionary<string, AsyncOperationHandle> _handleMap = new Dictionary<string, AsyncOperationHandle>();

    public async Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 存储句柄以便后续释放
                _handleMap[key] = handle;
                return handle.Result;
            }
            else
            {
                Debug.LogError($"[AddressableLoader] 加载失败: {key}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AddressableLoader] 异常: {ex}");
            return null;
        }
    }

    public async Task<SceneInstance> LoadSceneAsync(string key, LoadSceneMode mode)
    {
        var handle = Addressables.LoadSceneAsync(key, mode);
        await handle.Task;
        return handle.Result; // SceneInstance
    }

    public void ReleaseAsset(string key)
    {
        if (_handleMap.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            _handleMap.Remove(key);
        }
    }

    public void ClearCache()
    {
        foreach (var kvp in _handleMap)
        {
            Addressables.Release(kvp.Value);
        }
        _handleMap.Clear();
        Debug.Log("[AddressableLoader] 已释放所有资源句柄");
    }

    public Task<T[]> LoadAllAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        return null;
    }
}