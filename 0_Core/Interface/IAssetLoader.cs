using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

public interface IAssetLoader
{
    /// <summary> 异步加载资源（泛型） </summary>
    Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object;

    /// <summary>
    /// 异步加载key下的所有同类型资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<T[]> LoadAllAssetAsync<T>(string key) where T : Object;
    /// <summary> 异步加载场景 </summary>
    Task<SceneInstance> LoadSceneAsync(string key, UnityEngine.SceneManagement.LoadSceneMode mode);

    /// <summary> 释放资源（用于 Addressables 或手动缓存） </summary>
    void ReleaseAsset(string key);

    /// <summary> 清空所有缓存（场景切换时调用） </summary>
    void ClearCache();
}
