using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StartView : BaseUIView
{
    /**
     * 
     * 主要功能： 
     *     1.入场动画实现 √
     *     2.面板切换控制 √
     *     3.各个按钮的功能实现 
     *     4.各个存档槽位的正确显示
     *     5.各个存档槽位的按钮功能设置
     *     6.提供删除某个存档的方法
     *     
     * 
     * 
     */
    #region 组件引用
    [Header("入场动画相关")]
    [SerializeField] private GameObject[] BackGrounds;
    [SerializeField] private GameObject Bollte;
    [SerializeField] private GameObject Title;
    [SerializeField] private GameObject ButtonArea;
    [SerializeField] private GameObject SaveFieldPanel;
    [SerializeField] private TextMeshProUGUI SaveTitleText;
    [SerializeField] private List<SaveFieldObject> SaveFields;

    #endregion

    [Header("动画配置")]
    [SerializeField] private float BaseSpeed;
    [SerializeField] private float FadeSpeed;

    #region 配置 
    [System.Serializable]
    public class SaveFieldObject
    {
        public GameObject SaveField;
        public TextMeshProUGUI GameDays;
        public TextMeshProUGUI PlayTime;
        public TextMeshProUGUI Location;
        public Button FieldButton;
    }

    private bool isOverWriteModel = false;
    #endregion

    #region 初始化 
    public override void Open(UIPanelData data = null)
    {
        SetAllGameObjectFalse();
        base.Open(data);
        _panelKey = "StartView";
    }

    protected override void OnOpen()
    {
        StartCoroutine(StartInterfaceEntranceAnimationControl());
        updateFieldPanel();
    }
    #endregion

    #region 开场动画控制
    private void SetAllGameObjectFalse()
    {
        SetButtonAreaOn();
        SetUIAlpha(ButtonArea, 0, false, false);
        SetUIAlpha(Bollte, 0, false, false);
        SetUIAlpha(Title, 0, false, false);
        SaveFieldPanel.SetActive(false);
        foreach (var item in BackGrounds)
        {
            SetUIAlpha(item, 0, false, false);
        }
    }
    private IEnumerator StartInterfaceEntranceAnimationControl()
    {
        WaitForSeconds wait = new WaitForSeconds(BaseSpeed);

        yield return new WaitForSeconds(0.3f);
        AudioManager.Instance.PlayBGM(AudioPaths.BGM_Start);
        for (int i = 0; i < BackGrounds.Length; i++)
        {
            yield return StartCoroutine(FadeInBackGround(BackGrounds[i], wait, FadeSpeed));

        }
        yield return StartCoroutine(FadeInBackGround(Bollte, wait, FadeSpeed));

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(FadeInBackGround(Title, wait, FadeSpeed));
        yield return StartCoroutine(FadeInBackGround(ButtonArea, wait, FadeSpeed));
    }



    #endregion

    #region 开始游戏按钮
    public void OnStartNewGameButton()
    {
        //1.检查存档栏位是否已满
        int EmptyFieldIndex = GetFirstEmptyField();

        //2.如果满了提示玩家存档存档位置已满，弹出选择框，选择是，让玩家选择一个存档覆盖，选择否，返回初始界面
        if (EmptyFieldIndex == -1)
        {
            PopupManager.Instance.ShowConfirm("存档已满", "存档位置已满，无法创建新存档。是否选择一个存档栏位进行覆盖？",
    onConfirm: () =>
    {
        // 选择覆盖，进入覆盖模式
        isOverWriteModel = true;
        //1.显示存档栏位面板
        SetSaveFieldPanelOn();
    },
    onCancel: () => { /* 取消 */ });
            return;
        }
        //3.如果没有满，直接开始新游戏，并将第一个空的存档栏位作为新游戏的存档栏位
        SaveManager.Instance?.NewGame(EmptyFieldIndex);
    }

    private int GetFirstEmptyField()
    {
        for (int i = 0; i < 3; i++)
        {
            bool hasSave = SaveManager.Instance?.HasSave(i) ?? false;
            if (!hasSave)
                return i;
        }
        return -1;
    }
    #endregion

    #region 继续游戏按钮
    public void OnContinueButton()
    {
        SetSaveFieldPanelOn();
    }

    #endregion

    #region 设置按钮
    public void OnSettingButton()
    {
        UIManager.Instance.OpenPanel(UIID._setting);
    }
    #endregion

    #region 退出游戏按钮
    public void OnExitGameButton()
    {
#if UNITY_EDITOR
        // 编辑器里 Application.Quit() 无效，用这个停止运行
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 打包后的游戏（Windows/其他平台）真正退出
        Application.Quit();
#endif
    }
    #endregion

    #region 存档栏位控制
    public void updateFieldPanel()
    {
        //1.遍历三个栏位
        for (int i = 0; i < 3; i++)
        {
            //2.获取对应栏位需要的信息
            SaveHeader currentInfo = SaveManager.Instance?.PeekHeader(i);
            //3.将获取到的信息展示出来
            SaveFields[i].PlayTime.text = "";
            SaveFields[i].Location.text = "";
            SaveFields[i].GameDays.text = "";
            SaveFields[i].FieldButton.onClick.RemoveAllListeners();
            int currentIndex = i;
            SaveFields[i].FieldButton.onClick.AddListener(() => OnFieldButtonClick(currentIndex));
            if (currentInfo == null)
            {
                SetUIAlpha(SaveFields[i].SaveField, 0.5f, false, false);
                continue;
            }
            else
            {
                ResetUIAlpha(SaveFields[i].SaveField);
            }
            if (currentInfo.gameTotalSeconds > 0)
            {
                Debug.Log($"第{i}个存档的游戏总时间为{currentInfo.gameTotalSeconds}秒");
                int minutes = (int)(currentInfo.gameTotalSeconds / (60*TimeManager.Instance.GameTimeScale));
                int hours = minutes / 60;
                SaveFields[i].PlayTime.text = $"{hours}时{minutes % 60}分";
            }
            //SaveFields[i].PlayTime.text = currentInfo.PlayTime;
            // 位置显示（六种情况）
            if (currentInfo != null)
            {
                SaveFields[i].Location.text = GetLocationText(currentInfo.plotStep, currentInfo.gameDayIndex, currentInfo.locationId, currentInfo.sessionState);
            }
            if (currentInfo.gameDayIndex > -1)
                SaveFields[i].GameDays.text = currentInfo.gameDayIndex + "天";
        }
    }
    private void OnFieldButtonClick(int i)
    {

        if (!isOverWriteModel)
        {
            LogWarning("继续游戏");
            //1.存档管理器加载对应的存档数据
            if (SaveManager.Instance.Load(i))
            {
                ContinueGame();   // ★ 读档成功后走继续流程
            }

        }
        else
        {
            LogWarning("开始覆写");
            SaveManager.Instance?.NewGame(i);
            isOverWriteModel = false;
        }
    }

    /// <summary>继续游戏：按 PlotStep 恢复剧情；剧情已过则按上次会话状态恢复。</summary>
    private void ContinueGame()
    {
       
        // 先切到游戏循环状态（Start → GameLoop）
        GameManager.Instance.ChangeState(GameManager.Instance.GameLoop);

        int plotStep = PlayerDataManager.Instance.PlotStep;
        Debug.Log($"[StartView] 继续游戏：PlotStep={plotStep}, LastSessionState={PlayerDataManager.Instance.LastSessionState}");
        if (plotStep < 11)
        {
            // 还在引导剧情中：交给 PlotDirector 按 PlotStep 恢复（捏脸/剧情/场景）
            PlotDirector.Instance.ResumeFromSave();
            UIManager.Instance.ClosePanel(UIID._start);
            return;
        }

        // 剧情已过：按上次退出时的会话状态恢复
        if (PlayerDataManager.Instance.LastSessionState == SessionState.FreeRoam)
        {
            // 上次在可移动场景：放玩家 + 开 HUD
            LocationId loc = PlayerDataManager.Instance.LastLocationId;
            _ = UIManager.Instance.SwitchPanelAsync(
                openKey: UIID._hud,
                openData: new UIPanelData { ClearStack = true },
                closeKey: UIID._start,
                duringBlack: () =>
                {
                    PlayerManager.Instance.ActivatePlayerObject(Location.GetPosition(loc));
                    (GameManager.Instance.GameLoop as GameLoopState)?.EnterFreeRoam();
                },
                fade: true
            );
        }
        else
        {
            // 上次在主菜单：直接开主菜单
            _ = UIManager.Instance.SwitchPanelAsync(
                openKey: UIID._mainmenu,
                openData: new UIPanelData { ClearStack = true },
                closeKey: null,
                fade: true
            );
        }
    }

    /// <summary>存档位置显示文本（六种情况）。</summary>
    private string GetLocationText(int plotStep, int day, int locationId, int sessionState)
    {
        if (plotStep < 8) return "新手教程";
        if (day >= 5) return "敬请期待";
        if (sessionState == (int)SessionState.MainMenu) return "小屋内";   // ★ 主菜单
        return ((LocationId)locationId) switch
        {
            LocationId.Garden => "花园",
            LocationId.Church => "教堂",
            LocationId.Home => "小屋内",
            _ => "小镇"
        };
    }
    #endregion

    #region 界面切换
    public void OnBackButton()
    {
        SetButtonAreaOn();
    }

    private void SetButtonAreaOn()
    {
        ButtonArea.SetActive(true);
        isOverWriteModel = false;
        SaveFieldPanel.SetActive(false);
    }
    private void SetSaveFieldPanelOn()
    {
        ButtonArea.SetActive(false);
        SaveFieldPanel.SetActive(true);
        SetUIAlpha(SaveFieldPanel, 0, false, false);
        StartCoroutine(FadeInBackGround(SaveFieldPanel, new WaitForSeconds(BaseSpeed), 0.07f));
    }
    #endregion

    #region 其他

    #endregion

    #region 工具类
    /// </summary>
    /// <param name="target">目标 UI 对象（需在 Canvas 下）</param>
    /// <param name="alpha">透明度 (0=全透, 1=不透)</param>
    /// <param name="affectRaycasts">是否同时控制射线检测（可选）</param>
    /// <param name="affectInteractable">是否同时控制交互状态（可选）</param>
    public static void SetUIAlpha(GameObject target, float alpha,
        bool? affectRaycasts = null, bool? affectInteractable = null)
    {
        if (target == null)
        {
            Debug.LogWarning("[UIAlphaHelper] 目标对象为空");
            return;
        }

        // 获取或添加 CanvasGroup（Unity 会自动处理子树渲染）
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        // 安全设置 alpha（clamp 防止异常值）
        cg.alpha = Mathf.Clamp01(alpha);

        // 可选：同步控制交互状态（常用于淡出时禁用点击）
        if (affectRaycasts.HasValue) cg.blocksRaycasts = affectRaycasts.Value;
        if (affectInteractable.HasValue) cg.interactable = affectInteractable.Value;

        // 调试提示（开发阶段可保留）
    }

    /// <summary>
    /// 重置透明度为完全不透明，并恢复交互（常用清理操作）
    /// </summary>
    public static void ResetUIAlpha(GameObject target)
    {
        if (target == null) return;

        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }

    private IEnumerator FadeInBackGround(GameObject item, WaitForSeconds wait, float FadeInSpeed)
    {
        float Alpha = 0;
        while (Alpha < 1)
        {
            Alpha += FadeInSpeed;
            SetUIAlpha(item, Alpha, false, false);
            if (Alpha >= 1)
            {
                SetUIAlpha(item, 1, true, true);
                break;
            }
            yield return wait;
        }
    }
    private IEnumerator FadeOutObject(GameObject item, WaitForSeconds wait, float FadeInSpeed)
    {
        float Alpha = 1;
        while (Alpha > 0)
        {
            Alpha -= FadeInSpeed;
            SetUIAlpha(item, Alpha, false, false);
            if (Alpha <= 0)
            {
                SetUIAlpha(item, 0, true, true);
                break;
            }
            yield return wait;
        }
    }
    #endregion

    #region 生命周期
    public void RemoveUI()
    {

    }
    public void UpdataUI()
    {

    }
    public bool onBackButton()
    {
        return false;
    }
    #endregion

    #region 日志输出
    public void Log(string message)
    {
        Debug.Log($"[StartView] : {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[StartView] : {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"[StartView] : {message}");
    }

    #endregion


}
