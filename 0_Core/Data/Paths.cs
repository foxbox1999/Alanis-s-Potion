using System.IO;
using UnityEngine;

public static class Paths
{
    public static readonly string _settingPath = Application.persistentDataPath + "/settings.json";
    public static readonly string _playerAppearancePath = Path.Combine(Application.dataPath, "Resources", "Appearance");
}
public static class AudioPaths
{
    public const string BGM_DIR = "Audio/BGM";
    public const string SFX_DIR = "Audio/SFX";
    public static string BGM(string name) => $"{BGM_DIR}/{name}";
    public static string SFX(string name) => $"{SFX_DIR}/{name}";

    // BGM
    public static readonly string BGM_Start = BGM("开始界面");
    public static readonly string BGM_MainMenuDay = BGM("主页面白天");
    public static readonly string BGM_MainMenuNight = BGM("主页面夜晚");
    public static readonly string BGM_Town = BGM("小镇");
    public static readonly string BGM_WizardDay = BGM("巫师岛白天");
    public static readonly string BGM_WizardNight = BGM("巫师岛夜晚");

    // SFX
    public static readonly string SFX_Click = SFX("通用提示");
    public static readonly string SFX_StartBrew = SFX("开始炼制");
    public static readonly string SFX_Info = SFX("情报提示");
    public static readonly string SFX_ItemGet = SFX("物品获取提示");
    public static readonly string SFX_OpenMap = SFX("打开地图");
    public static readonly string SFX_Knock = SFX("敲门");
    public static readonly string SFX_Exchange = SFX("物品交换");
    public static readonly string SFX_BrewSuccess = SFX("药品炼制成功");
    public static readonly string SFX_BrewAdvanced = SFX("高级药品炼制成功");
}

/// <summary>存档文件的路径管理工具。</summary>
public static class SavePaths
{
    /// <summary>存档根目录（persistentDataPath/Saves）。</summary>
    public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    /// <summary>指定槽位的数据文件路径。</summary>
    /// <param name="slot">槽位索引。</param>
    public static string FilePath(int slot) => Path.Combine(SaveDirectory, $"slot_{slot}.dat");

    /// <summary>指定槽位的校验文件路径（存放 SHA256 摘要）。</summary>
    /// <param name="slot">槽位索引。</param>
    public static string ChecksumPath(int slot) => Path.Combine(SaveDirectory, $"slot_{slot}.sha256");

    /// <summary>确保存档目录存在，并返回目录路径。</summary>
    /// <returns>存档目录路径。</returns>
    public static string EnsureDirectory()
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
        return SaveDirectory;
    }
}

/// <summary>
/// Resources 相对路径统一管理（LoadAssetAsync / Resources.Load 用）。
/// 把散落在各业务脚本里的硬编码字符串集中到这里。
/// </summary>
public static class ResourcePaths
{
    // —— UI 预制件 ——
    /// <summary>UI 预制件路径（key 为面板名，如 "Start"）。</summary>
    public static string UIPrefab(string key) => $"Prefabs/UI/{key}";

    // —— 对话文本 ——
    /// <summary>剧情对话 CSV。</summary>
    public const string StoryLineCsv = "Txts/StoryLine";

    // —— 捏脸素材 ——
    /// <summary>捏脸部位立绘目录（最终合成用）。</summary>
    public const string FaceSculptingDir = "Images/Players/Face_Sculpting";

    /// <summary>捏脸素材选择目录（UI 展示用）。</summary>
    public const string FaceSculptingToChooseDir = "Images/Players/Face_Sculpting_ToChoose";

    /// <summary>发色素材目录。</summary>
    public const string HairColorDir = "Images/Players/BaseColor/HairColor";

    /// <summary>瞳色素材目录。</summary>
    public const string EyeColorDir = "Images/Players/BaseColor/PupilColor";

    /// <summary>某个捏脸部位（如 "BackHair_0_0"）。</summary>
    public static string FacePart(string partId) => $"{FaceSculptingDir}/{partId}";
}