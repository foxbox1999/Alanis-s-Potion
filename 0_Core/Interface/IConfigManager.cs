using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConfigManager 
{
    Settings CurrentSettings { get; }
    /// <summary>
    /// 加载用户设置（内部会发布 SettingsChangedEvent，AudioManager 会自动响应）
    /// </summary>
    void LoadOrCreateUserSettings();

    /// <summary>
    /// 设置主音量（文件层面）
    /// </summary>
    /// <param name="value"></param>
    void SetMasterVolume(float value);

    /// <summary>
    /// 设置音乐音量（文件层面）
    /// </summary>
    /// <param name="value"></param>
    void SetBGMVolume(float value);

    /// <summary>
    /// 设置音效音量（文件层面）
    /// </summary>
    /// <param name="value"></param>
    void SetSFXVolume(float value);

    /// <summary>
    /// 设置按键绑定，前面是待绑定的功能，后面是按键
    /// </summary>
    /// <param name="actionName"></param>
    /// <param name="newKeyCode"></param>
    void SetKeyBind(string actionName, string newKeyCode);
}
