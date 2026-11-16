using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HudView : BaseUIView
{
    #region 引用
    [Header("金币显示")]
    [SerializeField] private TextMeshProUGUI goldCoins;
    [SerializeField] private TextMeshProUGUI silverCoins;

    [Header("时间显示")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI NeedDay;
    [Header("天气显示")]
    [SerializeField] private Image Weather;
    [SerializeField] private GameObject MapButton;
    [SerializeField] private GameObject PotionButton;
    [SerializeField] private GameObject UIControl;

    #endregion

    #region 配置
    private bool isUIHidden = false;
    // 缓存上一次显示的值，变了才刷新（-1 表示"还没刷过"，强制首帧刷新）
    private int _lastDay = -1;          //上次更新存的天数
    private int _lastMinute = -1;       //上次更新存的分钟
    private int _lastGold = -1;         //上次更新存的金币
    private int _lastSilver = -1;       //上次更新存的硬币
    private int _lastRentDays = -1;     //上次更新存的下次交租时间
    #endregion

    #region Life
    protected override void OnOpen()
    {
        base.OnOpen();
        Initialize();
        PlayBGMByDestination();
    }
    public void Initialize()
    {
        CheckPlot();
    }

    /// <summary>根据玩家最后所在区域播放 BGM。</summary>
    private void PlayBGMByDestination()
    {
        var loc = PlayerDataManager.Instance.LastLocationId;

        if (loc == LocationId.Garden)
        {
            // 花园 = 巫师岛
            AudioManager.Instance.PlayBGMByTime(AudioPaths.BGM_WizardDay, AudioPaths.BGM_WizardNight);
        }
        else
        {
            // 其它位置（StreetRight/StreetLeft/Church/Triumphal/Home）= 小镇
            AudioManager.Instance.PlayBGM(AudioPaths.BGM_Town);
        }
    }
    private void CheckPlot()
    {
        if(PlayerDataManager.Instance.PlotStep <= 10)
        {
            MapButton.SetActive(false);
            PotionButton.SetActive(false);
        }
        else
        {
            MapButton.SetActive(true);
            PotionButton.SetActive(true);
        }
    }
    #endregion

    #region Core
    /// <summary>
    /// 主页按钮
    /// </summary>
    public void onHomeButton()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
             openKey: UIID._mainmenu,
             closeKey: UIID._hud,
             duringBlack: null,
             fade: true
         );
    }
    /// <summary>
    /// 炼药按钮
    /// </summary>
    public void onCauldron()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
             openKey: UIID._alchemy,
             closeKey: UIID._hud,
             duringBlack: null,
             fade: true
         );
    }
    /// <summary>
    /// 地图按钮
    /// </summary>
    public void onMap()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
             openKey: UIID._map,
             closeKey: UIID._hud,
             duringBlack: null,
             fade: true
         );
    }
    public void onHideUI()
    {
        UIControl.SetActive(isUIHidden);
        isUIHidden = !isUIHidden;
    }
    /// <summary>
    /// 情报按钮
    /// </summary>
    public void onBackPack()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
              openKey: UIID._backpack,
              openData: new InventoryPanelData { Context = InventoryContext.View},
              closeKey: UIID._hud,
              duringBlack: null,
              fade: true
          );
    }
    /// <summary>
    /// 角色信息按钮
    /// </summary>
    public void onCHaracter()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
              openKey: UIID._intelligence,
              closeKey: UIID._hud,
              duringBlack: null,
              fade: true
          );
    }
    #endregion

    #region Tool
    private void Update()
    {
        RefreshTime();
        RefreshCoin();
        RefreshRent();
    }
    public static string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToString("yyyy年MM月dd日 HH:mm");
    }
    private void RefreshCoin()
    {
        //2.获取玩家当前的金币和银币数量
        int gold = PlayerDataManager.Instance.GoldCoins;
        int silver = PlayerDataManager.Instance.SilverCoins;
        //3.更新金币文本显示
        if (gold != _lastGold) { _lastGold = gold; goldCoins.text = gold.ToString(); }
        if (silver != _lastSilver) { _lastSilver = silver; silverCoins.text = silver.ToString(); }
    }
    private void RefreshTime()
    {
        var t = TimeManager.Instance.TimeData;   // 读缓存字段，不分配内存
        if (t.Day != _lastDay)
        {
            _lastDay = t.Day;
            dayText.text = $"{t.Year}年{t.Month}月{t.Day}日";
        }
        int minute = t.Hour * 60 + t.Minute;
        if (minute != _lastMinute)
        {
            _lastMinute = minute;
            timeText.text = $"{t.Hour:00}:{t.Minute:00}";
        }
    }
    private void RefreshRent()
    {
        int days = WorldManager.Instance.RentDaysLeft;
        if (days != _lastRentDays)
        {
            _lastRentDays = days;
            NeedDay.text = $"{days}";
        }
    }
    #endregion

    #region Logs
    public void Log(string message)
    {
        Debug.Log($"[HudView] : {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[HudView] : {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"[HudView] : {message}");
    }
    #endregion
}
