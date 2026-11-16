using System;

/// <summary>玩家本体存档：基础信息、剧情进度、货币、捏脸外貌。</summary>
[Serializable]
public class PlayerSaveData
{
    /// <summary>玩家唯一 ID。注意：原模型是 long，JsonUtility 不支持 long，故用 string 存储。</summary>
    public string playerID = "";

    /// <summary>玩家名。</summary>
    public string playerName = "艾拉妮丝";

    /// <summary>剧情进度（PlotStep），由 PlotDirector 驱动。</summary>
    public int plotStep;

    /// <summary>最后所在区域（LocationId 枚举值）。</summary>
    public int locationId = 0;

    /// <summary>最后会话状态：0=可移动场景(HUD) 1=主菜单。</summary>
    public int sessionState = 0;
    /// <summary>上次存档位置（字符串形式，如 "(58.27, -51.16, 0.00)"）。</summary>
    public string lastCheckpointPosition = "(0.00, 0.00, 0.00)";

    /// <summary>金币。</summary>
    public int goldCoins = 10;

    /// <summary>银币。</summary>
    public int silverCoins = 100;

    /// <summary>后发（形如 "BackHair_样式_颜色"）。</summary>
    public string backHair = "BackHair_0_0";

    /// <summary>眼睛（形如 "Eyes_样式_颜色"）。</summary>
    public string eyes = "Eyes_0_0";

    /// <summary>眉毛（形如 "Eyebrows_样式_颜色"）。</summary>
    public string eyebrows = "Eyebrows_0_0";

    /// <summary>前发（形如 "FrontHair_样式_颜色"）。</summary>
    public string frontHair = "FrontHair_0_0";

    /// <summary>嘴巴（形如 "Mouth_样式_颜色"）。</summary>
    public string mouth = "Mouth_0_0";
}