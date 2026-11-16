using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家数据管理器：负责玩家档案数据（ID、名字、剧情进度、货币、捏脸外观、存档位置）的
/// 运行时持有与持久化（实现 ISaveable）。
/// 与 PlayerManager（玩家实体/表现层）区分：本类只管"数据"，不碰 GameObject / 预制件 / 相机。
/// </summary>
/// 什么时候需要将捏脸的结果生成 Sprite？
///     1.开始新游戏时：捏脸确定后，生成 Sprite 作为玩家外观资源，存入本地，然后将 Sprite 赋值给PlayerAppearance以及characterspriteConfig 捏脸
///     2.读档时：从存档中读取捏脸数据，生成 Sprite 作为玩家外观资源，存入本地，然后将 Sprite 赋值给PlayerAppearance以及characterspriteConfig 捏脸
///     3.当捏脸数据发生变化后：生成 Sprite 作为玩家外观资源，然后将 Sprite 赋值给PlayerAppearance以及characterspriteConfig 捏脸(和1算是同一个流程)
public class PlayerDataManager : MonoSingleton<PlayerDataManager>, ISaveable
{
    #region 外观部位常量
    public const string PART_BACK_HAIR = "BackHair";    // 后发
    public const string PART_EYES = "Eyes";            // 眼睛
    public const string PART_EYEBROWS = "Eyebrows";    // 眉毛
    public const string PART_FRONT_HAIR = "FrontHair"; // 前发
    public const string PART_MOUTH = "Mouth";          // 嘴巴
    public const string PlayerAppearanceName = "PlayerAppearance"; // 玩家外观资源名
    public const string ChibiPlayerAppearanceName = "ChibiAppearance"; // 小人外观资源名
    #endregion

    #region 存档契约
    /// <summary>存档模块唯一键。</summary>
    public string SaveKey => "player";

    /// <summary>存档格式版本。</summary>
    public int SaveVersion => 1;
    #endregion

    #region 运行时数据
    private string _playerID = "";
    private string _playerName = "艾拉妮丝";
    private int _plotStep;
    private int _goldCoins = 10;
    private int _silverCoins = 100;
    private string _lastCheckpointPosition = "(0.00, 0.00, 0.00)";

    /// <summary>捏脸外观：部位名 → "部位_样式_颜色"（如 "BackHair_0_0"）。</summary>
    private readonly Dictionary<string, string> _appearance = new Dictionary<string, string>
    {
        { PART_BACK_HAIR, "BackHair_0_0" },
        { PART_EYES, "Eyes_0_0" },
        { PART_EYEBROWS, "Eyebrows_0_0" },
        { PART_FRONT_HAIR, "FrontHair_0_0" },
        { PART_MOUTH, "Mouth_0_0" }
    };

    public Sprite PlayerAppearance { get; private set; }
    public Sprite ChibiPlayerAppearance { get; private set; }

    public IDisposable FaceOver;
    #endregion

    #region Life

    
    protected override void Start()
    {
        base.Start();
        SaveManager.Instance?.RegisterModule(this);   // 自注册为存档模块
        FaceOver = EventBus.Instance.Subscribe<OnAvatarCustomizationOver>(OnAvatarCustomizationOver,-10);
    }
    private void OnAvatarCustomizationOver(OnAvatarCustomizationOver e)
    {
        SetAllPartIdentifiers(e._parts);   // 1. 保存当前外貌方案
        SetAppearance();                  // 2. 合成图片
    }
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        FaceOver?.Dispose();
    }

    #endregion

    #region 基础信息
    /// <summary>玩家唯一 ID。</summary>
    public string playerID { get => _playerID; set => _playerID = value ?? ""; }

    /// <summary>玩家名（空值回退默认）。</summary>
    public string PlayerName { get => _playerName; }
    #endregion

    #region 剧情进度
    /// <summary>当前剧情步骤。</summary>
    public int PlotStep => _plotStep;

    /// <summary>设置剧情步骤（不会小于 0）。</summary>
    public void SetPlotStep(int step) => _plotStep = Mathf.Max(0, step);

    /// <summary>剧情步骤 +1。</summary>
    public void AdvancePlot() => _plotStep++;
    #endregion

    #region 货币
    public int GoldCoins => _goldCoins;
    public int SilverCoins => _silverCoins;

    /// <summary>增加金币（不会为负）。</summary>
    public void AddGold(int delta) => _goldCoins = Mathf.Max(0, _goldCoins + delta);

    /// <summary>增加银币（不会为负）。</summary>
    public void AddSilver(int delta) => _silverCoins = Mathf.Max(0, _silverCoins + delta);

    public bool HasEnoughGold(int amount) => _goldCoins >= amount;
    public bool HasEnoughSilver(int amount) => _silverCoins >= amount;

    /// <summary>尝试花费金币；余额足够则扣款并返回 true。</summary>
    public bool TrySpendGold(int amount)
    {
        if (!HasEnoughGold(amount)) return false;
        _goldCoins -= amount;
        return true;
    }

    /// <summary>尝试花费银币；余额足够则扣款并返回 true。</summary>
    public bool TrySpendSilver(int amount)
    {
        if (!HasEnoughSilver(amount)) return false;
        _silverCoins -= amount;
        return true;
    }
    #endregion

    #region 存档位置
    public string LastCheckpointPosition { get => _lastCheckpointPosition; set => _lastCheckpointPosition = value ?? "(0.00, 0.00, 0.00)"; }

    /// <summary>用世界坐标设置存档点。</summary>
    public void SetCheckpoint(Vector3 position) => _lastCheckpointPosition = $"({position.x:F2}, {position.y:F2}, {position.z:F2})";

    private LocationId _lastLocationId = LocationId.Home;
    private SessionState _lastSessionState = SessionState.FreeRoam;

    public LocationId LastLocationId => _lastLocationId;
    public SessionState LastSessionState => _lastSessionState;

    /// <summary>记录最后所在区域（玩家移动/放置时调用）。</summary>
    public void SetLastLocation(LocationId id) => _lastLocationId = id;

    /// <summary>记录最后会话状态（进可移动场景/主菜单时调用）。</summary>
    public void SetLastSessionState(SessionState s) => _lastSessionState = s;
    #endregion

    #region 捏脸外观
    /// <summary>获取某部位外观标识（如 "BackHair_0_0"）。</summary>
    public string GetAppearance(string partKey) => _appearance.TryGetValue(partKey, out var v) ? v : null;

    /// <summary>设置某部位外观标识。</summary>
    public void SetAppearance(string partKey, string value)
    {
        if (string.IsNullOrEmpty(partKey) || string.IsNullOrEmpty(value)) return;
        _appearance[partKey] = value;
    }

    public void SetAppearance()
    {
        // 释放旧立绘的纹理，避免反复合成（读档/改捏脸）时 GPU 纹理泄漏
        if (PlayerAppearance != null && PlayerAppearance.texture != null)
            Destroy(PlayerAppearance.texture);

        List<Sprite> parts = new List<Sprite>();
        foreach (var key in _appearance.Keys)
        {
            var value = _appearance[key];
            Sprite part = Resources.Load<Sprite>($"{ResourcePaths.FaceSculptingDir}/{value}");
            parts.Add(part);
        }
        Sprite nose = Resources.Load<Sprite>(ResourcePaths.FacePart("Nose_0_0"));
        PlayerAppearance = SpriteCombiner.CombineSprites(parts, PlayerAppearanceName);
    }
    /// <summary>一次性设置全部外观（UIFaceCustomizer.SaveFaceDate 用）。</summary>
    public void SetAllPartIdentifiers(Dictionary<string, string> parts)
    {
        if (parts == null) return;
        foreach (var kvp in parts)
            SetAppearance(kvp.Key, kvp.Value);
    }

    /// <summary>获取全部外观标识（UIFaceCustomizer 初始化用）。</summary>
    public Dictionary<string, string> GetAllPartIdentifiers() => new Dictionary<string, string>(_appearance);

    /// <summary>恢复默认捏脸。</summary>
    public void ResetAppearance()
    {
        _appearance[PART_BACK_HAIR] = "BackHair_0_0";
        _appearance[PART_EYES] = "Eyes_0_0";
        _appearance[PART_EYEBROWS] = "Eyebrows_0_0";
        _appearance[PART_FRONT_HAIR] = "FrontHair_0_0";
        _appearance[PART_MOUTH] = "Mouth_0_0";
    }
    #endregion

    #region 存档（ISaveable）
    /// <summary>序列化为 PlayerSaveData 的 JSON。</summary>
    public string SerializeState()
    {
        // 保存时根据根 UI 决定：主菜单 or HUD/可移动场景
        if (UIManager.Instance != null && UIManager.Instance.IsPanelOpen(UIID._mainmenu))
            _lastSessionState = SessionState.MainMenu;
        else
            _lastSessionState = SessionState.FreeRoam;

        string json = JsonUtility.ToJson(new PlayerSaveData
        {
            playerID = _playerID,
            playerName = _playerName,
            plotStep = _plotStep,
            lastCheckpointPosition = _lastCheckpointPosition,
            locationId = (int)_lastLocationId,
            sessionState = (int)_lastSessionState,
            goldCoins = _goldCoins,
            silverCoins = _silverCoins,
            backHair = GetAppearance(PART_BACK_HAIR),
            eyes = GetAppearance(PART_EYES),
            eyebrows = GetAppearance(PART_EYEBROWS),
            frontHair = GetAppearance(PART_FRONT_HAIR),
            mouth = GetAppearance(PART_MOUTH)
        });
        return JsonUtility.ToJson(json);
    }
    

    /// <summary>从 PlayerSaveData 的 JSON 恢复。</summary>
    public void DeserializeState(string json, int version)
    {
        if (string.IsNullOrEmpty(json)) { ResetState(); return; }
        var dto = JsonUtility.FromJson<PlayerSaveData>(json);
        if (dto == null) return;

        _playerID = dto.playerID;
        _playerName = dto.playerName;
        _plotStep = dto.plotStep;
        _lastCheckpointPosition = dto.lastCheckpointPosition;
        _lastLocationId = (LocationId)dto.locationId;
        _lastSessionState = (SessionState)dto.sessionState;
        _goldCoins = dto.goldCoins;
        _silverCoins = dto.silverCoins;
        _appearance[PART_BACK_HAIR] = dto.backHair;
        _appearance[PART_EYES] = dto.eyes;
        _appearance[PART_EYEBROWS] = dto.eyebrows;
        _appearance[PART_FRONT_HAIR] = dto.frontHair;
        _appearance[PART_MOUTH] = dto.mouth;

        SetAppearance();
    }

    /// <summary>恢复默认玩家数据。</summary>
    public void ResetState()
    {
        _playerID = "";
        _playerName = "艾拉妮丝";
        _plotStep = 0;
        _lastCheckpointPosition = "(0.00, 0.00, 0.00)";
        _lastLocationId = LocationId.Home;
        _lastSessionState = SessionState.FreeRoam;
        _goldCoins = 10;
        _silverCoins = 100;
        ResetAppearance();
    }
    #endregion
}