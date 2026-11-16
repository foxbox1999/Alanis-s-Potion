using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 剧情流程导演：按顺序推进
/// 点开始 → 第一段剧情 → 捏脸 → 第二段剧情 → 进镇 → 第三段剧情(买店铺) → 回药店 → 采集教程 → 炼药教程 → 看病教程 → 正常游玩
/// </summary>
public class PlotDirector : MonoSingleton<PlotDirector>
{
    #region 基础配置
    // —— 剧情对话起始 ID（对应 StoryLine.csv）——
    private const int DIALOGUE_INTRO = 0;   // 第一段：引入剧情（星历1509年…）
    private const int DIALOGUE_AFTER_FACE = 7;   // 第二段：捏脸后（肖，你看我这样）
    private const int DIALOGUE_TOWN = 14;  // 第三段：进镇买店铺
    private const int DIALOGUE_HOMEDOOR = 177; //新手指引结束后
    // —— 场景名（改成你项目里的实际场景名）——
    private const string HOME_SCENE = "Home";
    private const string TOWN_SCENE = "Town";
    [SerializeField] private List<PlotBackground> backs = new List<PlotBackground>();
    private const int DIALOGUE_DAY5 = 148/* 填 CSV 里第五天剧情的 StartID */;
    private bool _endingPlayed;
    private IDisposable _newDaySub;
    [Serializable]
    public class PlotBackground
    {
        public int plot;
        public Sprite backSprite;
    }
    #endregion

    #region 运行时数据
    private IDisposable FaceOver;
    #endregion

    #region Life
    protected override void Start()
    {
        base.Start();
        FaceOver = EventBus.Instance.Subscribe<OnAvatarCustomizationOver>(OnAvatarCustomizationOver, 10);
        _newDaySub = EventBus.Instance.Subscribe<NewDayEvent>(e => CheckDay5Ending(), 10);
    }

    // PlotDirector 里订阅并响应
    private void OnAvatarCustomizationOver(OnAvatarCustomizationOver e)
    {
        // 只在「捏脸」这一步（PlotStep==1）才推进剧情；以后玩家重新捏脸（非剧情）不会误推
        if (PlayerDataManager.Instance.PlotStep == 1)
        {
            PlayerDataManager.Instance.SetPlotStep(2);
            UIManager.Instance.ClosePanel(UIID._characterCreation);
            Advance();
        }
    }
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        FaceOver?.Dispose();
    }
    #endregion

    /// <summary>
    /// 真表示PlotDirector空闲，假表示在使用
    /// </summary>
    private bool _flowRunning = true;

    /// <summary>推进一步。对话结束 / 捏脸确认 / 场景加载完 / 教程完成时调用。</summary>
    /**
     * 剧情流程：
     *      0：播放第一段剧情并开启1
     *      1：打开捏脸面板
     *      2：播放第二段剧情并开启3
     *      3：加载镇场景（黑屏）并开启4
     *      4：播放第三段剧情并开启5
     *      5：移动玩家到药店门口并打开6 
     *      6：打开HUD，播放自言自语并开启6
     *      7：播放自言自语并开启8
     *      8：自由活动并送礼给玩家，将进度推进到9
     *      9：由主菜单界面触发，播放完整采集教程并由采集页面推进到10
     *      10：由主菜单触发，播放炼药教程并由采集页面推进进度到11
     *      11：由时间触发，播放第五天剧情
     */
    public void Advance()
    {
        if (!_flowRunning) return;
        //Debug.Log($"当前进度位于{PlayerDataManager.Instance.PlotStep}");
        switch (PlayerDataManager.Instance.PlotStep)
        {
            case 0: PlayDialogue(DIALOGUE_INTRO); break;  // 第一段剧情
            case 1: OpenFaceCustomizer(); break;  // 捏脸
            case 2: PlayDialogue(DIALOGUE_AFTER_FACE); break;  // 第二段剧情
            case 3: _ = LoadSceneAsync(TOWN_SCENE); break;    // 进镇（黑屏）
            case 4: PlayDialogue(DIALOGUE_TOWN); break;       // 第三段剧情（买店铺）
            case 5: _ = LoadSceneAsync(HOME_SCENE); break;    // 回药店（黑屏→HUD→对话177→FreePlayer）
            case 6: PlayTutorial(TutorialKind.Hud); break;  // HUD教程
            case 7: PlayDialogue(DIALOGUE_HOMEDOOR); break;  // 播放自言自语
            case 8: FreePlayer(); break;  // 此时应该是自由活动
            case 10: PlayTutorial(TutorialKind.Alchemy); break;  // 炼药教程
            case 11: StartCoroutine(PlayDay5Ending()); break;   // 第五天结局
            default: return;   // 越界保护
        }
    }

    #region 第五天结局
    private void CheckDay5Ending()
    {
        if (WorldManager.Instance.CurrentDay < 5) return;              // 还没到第五天
        if(PlayerDataManager.Instance.PlotStep < 11) return;   // 还没到第五天剧情
        StartCoroutine(PlayDay5Ending());
    }

    /// <summary>第五天结局：剧情 → 结局动画 → 存档 + 清会话 + 回 Start。</summary>
    private IEnumerator PlayDay5Ending()
    {
        // 1. 第五天剧情（播完回调）
        bool dialogueDone = false;
        EventBus.Instance.Publish(new RequestDialogue
        {
            StartID = DIALOGUE_DAY5,
            OnDialogueComplete = () => dialogueDone = true
        });
        yield return new WaitUntil(() => dialogueDone);

        // 2. 结局动画（占位，换成实际动画播完回调）
        //yield return PlayEndingAnimation();

        // 3. 存档（此时 PlotStep == 11）+ 清会话 + 自动回 Start
        SaveManager.Instance.Save();
        SaveManager.Instance.EndSession(false);
        GameManager.Instance.ChangeState(GameManager.Instance.StartState);
    }

    /// <summary>结局动画：替换成实际动画播完的回调。</summary>
    private IEnumerator PlayEndingAnimation()
    {
        yield return new WaitForSeconds(3f);
    }
    #endregion

    /// <summary>读档后恢复剧情：按当前 PlotStep 推进到对应步骤。</summary>
    public void ResumeFromSave()
    {
        _flowRunning = true;   // 确保流程可推进
        Advance();
    }
    #region 各步骤实现
    private void FreePlayer()
    {
        if(PlayerDataManager.Instance.PlotStep != 8)
        {
            return;
        }
        if (PlayerDataManager.Instance.PlotStep == 8)
            PlayerManager.Instance.ThawPlayer();
        GiveStartingItems();
        PlayerDataManager.Instance.SetPlotStep(9);
    }

    #region 开局赠礼
    /// <summary>新手礼包：每种药品 1 份、每种药材 5 份，并弹获得物品窗口。</summary>
    private void GiveStartingItems()
    {
        var obtained = new List<ItemStackData>();
        foreach (var item in DatabaseManager.Instance.AllItems)
        {
            if (item.category == ItemCategory.Potion)
            {
                InventoryManager.Instance.Add(item.id, 1);
                obtained.Add(new ItemStackData { itemId = item.id, count = 1 });
            }
            else if (item.category == ItemCategory.Herb)
            {
                InventoryManager.Instance.Add(item.id, 5);
                obtained.Add(new ItemStackData { itemId = item.id, count = 5 });
            }
        }
        if (obtained.Count > 0)
            PopupManager.Instance.ShowItem(obtained);
        //    EventBus.Instance.Publish(new ItemsObtainedEvent { Items = obtained });
    }
    #endregion

    /// <summary>播一段剧情，播完自动 Advance。</summary>
    private void PlayDialogue(int startId)
    {
        Sprite back = null;
        if (startId == 0 || startId == 7)
            back = backs.Find(i => i.plot == 0).backSprite;
        bool needblack = true;
        if (startId == 177 || startId == DIALOGUE_TOWN)
            needblack = false;
        EventBus.Instance.Publish(new RequestDialogue
        {
            StartID = startId,
            OnDialogueComplete = () =>
            {
                PlayerDataManager.Instance.SetPlotStep(PlayerDataManager.Instance.PlotStep + 1);
                Advance();
            },
            BackGround = back,
            NeedBlackFade = needblack
        });
    }

    /// <summary>打开捏脸面板；确认后由 UIFaceCustomizer 回调 Advance。</summary>
    private void OpenFaceCustomizer()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._characterCreation,
            openData: new UIPanelData { ClearStack = true },
            closeKey: null,
            duringBlack: null,
            fade: true
        );
    }
    private void checkHUDON()
    {
        if (!UIManager.Instance.IsPanelOpen(UIID._hud))
        {
            _ = UIManager.Instance.SwitchPanelAsync(UIID._hud,new UIPanelData { ClearStack = true},duringBlack:() => {
                PlayerManager.Instance.ActivatePlayerObject(Location._triumphal);
                PlayerManager.Instance.FreezePlayer();
            });
        }
    }

    private async Task LoadSceneAsync(string sceneName)
    {
        bool isHome = sceneName == HOME_SCENE;

        await ScreenFader.Instance.FadeToBlackAsync();

        if (isHome)
        {
            // 黑屏期间：设玩家位置 + 开 HUD
            PlayerManager.Instance.MovePlayerTo(Location._home);
            UIManager.Instance.OpenPanel(UIID._hud,new UIPanelData { ClearStack = true });
            TimeManager.Instance.Resume();
        }
        else
        {
            // 进镇：激活玩家 + 定位 + 冻结
            PlayerManager.Instance.ActivatePlayerObject(Location._triumphal);
            PlayerManager.Instance.FreezePlayer();
        }

        await ScreenFader.Instance.FadeFromBlackAsync();

        PlayerDataManager.Instance.SetPlotStep(PlayerDataManager.Instance.PlotStep + 1);
        Debug.Log($"场景加载完毕，当前进度位于{PlayerDataManager.Instance.PlotStep}");
        Advance();
    }

    /// <summary>播放一个教程，完成后自动 Advance（完成判定见下方说明）。</summary>
    private void PlayTutorial(TutorialKind kind)
    {
        switch (kind)
        {
            case TutorialKind.Hud:
                HudSteps();
                break;
            case TutorialKind.Alchemy:
                AlchemySteps();
                break;
        }
    }

    private void HudSteps()
    {
        checkHUDON();
        GuideManager.Instance.StartGuide(new List<GuideStep>
        {
            new() { targetName = "EMotion", tipText = "这里可以查看已经解锁的角色信息", arrow = GuideArrowDir.DownL },

            new() { targetName = "NeedDays", tipText = "这里可以看到还有多久就要交租啦", arrow = GuideArrowDir.Left }
      }, () =>
      {
          PlayerDataManager.Instance.SetPlotStep(7);   // HUD教程完 → 对话177
          Advance();
      });
    }

    private void GatherSteps()
    {
        //1.打开采集界面
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._gather,
            openData: new UIPanelData
            {
                OnUIOpen = () =>
                {
                    GuideManager.Instance.StartGuide(new List<GuideStep>
                    {
                        new() { targetName = "PetranMountain", tipText = "地图上有这种提示标的就是可以采集的地方。", arrow = GuideArrowDir.Up },
                        new() { targetName = "PetranMountain", tipText = "每个采集点都有独属于自己的药材，可以点击查看。", arrow = GuideArrowDir.Up }
                    }, () =>
                    {
                        PlayerDataManager.Instance.SetPlotStep(10);
                    });
                }
            },
            closeKey: UIID._mainmenu,
            duringBlack: null,
            fade: true
        );
    }



    private void AlchemySteps()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
       openKey: UIID._mainmenu,
       openData: new UIPanelData { ClearStack = true },
       closeKey: null,
       duringBlack: null,
       fade: false
   );
        GuideManager.Instance.StartGuide(new List<GuideStep>
    {
        new() { targetName = "Cauldron", tipText = "采集完了，就可以炼药了。", arrow = GuideArrowDir.DownR },
    }, () =>
    {
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._alchemy,
            openData: null,
            closeKey: UIID._mainmenu,
            duringBlack: null,
            fade: true
        );
    });
    }
    #endregion

    /// <summary>第一次进小屋：锁定其它按钮，只留「休息中」，引导点击休息。</summary>
    // 在「进入小屋按钮」点击后调用（此时已在看病界面）
    public void StartHomeGuide()
    {
        if (PlayerDataManager.Instance.PlotStep != 9) return;

        // 第一段：指向休息中 ToMainMenu
        GuideManager.Instance.StartGuide(new List<GuideStep>
    {
        new() { targetName = "ToMainMenu", tipText = "点击此处回到后屋内", arrow = GuideArrowDir.DownL },
    }, () =>
    {
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._mainmenu,
            openData: new UIPanelData { ClearStack = true },
            closeKey: UIID._clinic,
            duringBlack: null,
            fade: true
        );
    });
    }

    public void OnHomeGuideDone()
    {
        // 玩家点了 ToMainMenu → 进主菜单 → 第二段引导：采集 → 炼药
        GuideManager.Instance.StartGuide(new List<GuideStep>
    {
        new() { targetName = "GatherHerbs",  tipText = "点击这里，去往采集", arrow = GuideArrowDir.Right },
    }, () =>
    {
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._gather,
            closeKey: UIID._mainmenu,
            duringBlack: null,
            fade: true
        );
    });
    }
}

/// <summary>教程类型。</summary>
public enum TutorialKind { Gather, Alchemy, Clinic, Hud }