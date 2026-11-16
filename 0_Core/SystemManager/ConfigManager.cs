using System.IO;
using UnityEngine;

public class ConfigManager : MonoSingleton<ConfigManager>, IConfigManager
{
    /**
     * 管理静态数据（游戏参数、物品表、对话树等）和运行时设置（用户自定义按键、音量、图形选项）。
     */
    #region 配置
    public Settings CurrentSettings { get; private set; }
    #endregion

    #region 生命周期
    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    #region 动态设置持久化 (序列化/反序列化)
    /// <summary>
    /// 加载或创建用户设置,全局设置
    /// </summary>
    public void LoadOrCreateUserSettings()
    {
        if (File.Exists(Paths._settingPath))
        {
            bool needCreateDefault = false;
            try
            {
                string json = File.ReadAllText(Paths._settingPath);
                CurrentSettings = JsonUtility.FromJson<Settings>(json);

                // 如果反序列化返回 null，说明 JSON 格式有误（比如空文件或完全不匹配）
                if (CurrentSettings == null)
                {
                    needCreateDefault = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"加载设置文件失败: {ex.Message}");
                needCreateDefault = true;
            }
            // 如果标记为需要重建，则删除原文件
            if (needCreateDefault)
            {
                try
                {
                    File.Delete(Paths._settingPath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"删除损坏的设置文件失败: {ex.Message}");
                }
            }
        }
        else
        {
            CurrentSettings = new Settings(); // 默认值
            SaveSettings(); // 立即创建文件
        }
#if UNITY_STANDALONE_WIN
        ApplyScreenSettings();
#endif
        ApplyAllSettings();
    }
    /// <summary>设置全屏标志（文件层面）。</summary>
    public void SetFullScreen(bool full) => CurrentSettings.isFullScreen = full;

    /// <summary>
    /// 立即保存当前设置到文件中，通常在应用退出时调用
    /// </summary>
    public void SaveSettings()
    {
        if (CurrentSettings == null) return;
        string json = JsonUtility.ToJson(CurrentSettings, prettyPrint: true);
        File.WriteAllText(Paths._settingPath, json);
    }
    #endregion

    #region 应用设置
    /// <summary>
    /// 应用分辨率设置
    /// </summary>
    private void ApplyScreenSettings()
    {
        if (CurrentSettings == null) return;

        // 应用分辨率
        Screen.SetResolution(
            CurrentSettings.resolutionWidth,
            CurrentSettings.resolutionHeight,
            CurrentSettings.isFullScreen
        );
    }

    /// <summary>
    /// 应用所有设置到游戏中，并广播事件通知其他系统，修改设置后需要调用此方法
    /// </summary>
    private void ApplyAllSettings()
    {
        if (CurrentSettings == null) return;
        //全局音量设置广播
        EventBus.Instance.Publish(new SettingsChangedEvent(CurrentSettings));
    }

    /// <summary>
    /// 应用当前设置到游戏中（包括分辨率、音量等），并广播事件通知其他系统，修改设置后需要调用此方法
    /// </summary>
    public void ApplySettings()
    {
#if UNITY_STANDALONE_WIN
        ApplyScreenSettings();
#endif
        ApplyAllSettings();
    }
    #endregion

    #region 修改设置与事件通知 (观察者模式)
    /// <summary>
    /// 设置主音量（文件层面）
    /// </summary>
    /// <param name="value"></param>
    public void SetMasterVolume(float value)
    {
        CurrentSettings.masterVolume = Mathf.Clamp01(value);
        Debug.Log($"[ConfigManager] 设置主音量为: {CurrentSettings.masterVolume}");
    }
 
    /// <summary>
    /// 设置音乐音量（文件层面）
    /// </summary>
    /// <param name="value"></param>
    public void SetBGMVolume(float value)
    {
        CurrentSettings.bgmVolume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// 设置音效音量（文件层面）
    /// </summary>
    /// <param name="value"></param>
    public void SetSFXVolume(float value)
    {
        CurrentSettings.sfxVolume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// 设置按键绑定，前面是待绑定的功能，后面是按键
    /// </summary>
    /// <param name="actionName"></param>
    /// <param name="newKeyCode"></param>
    public void SetKeyBind(string actionName, string newKeyCode)
    {

    }
    #endregion
}

