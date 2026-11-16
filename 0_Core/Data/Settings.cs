using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Settings 
{
    //音量设置
    public float masterVolume = 1.0f;   //主音量
    public float bgmVolume = 1.0f;      //音乐音量
    public float sfxVolume = 1.0f;      //音效音量

    //分辨率和全屏控制
    public bool isFullScreen = true;    //全屏模式
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;

    // 按键映射：使用 Serializable 字典或 List 包装
    public List<KeyBindPair> keyBinds = new List<KeyBindPair>();
}

[System.Serializable]
public class KeyBindPair
{
    public string actionName; // 如 "Interact", "MoveUp"
    public string keyCode;    // 存储字符串便于跨平台
}