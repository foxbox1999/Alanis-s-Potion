using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 看病界面 View：只负责显示 + 接收点击，业务逻辑全在 ClinicManager。
/// 流程：选病人 → 望（点身上可见症状）/ 问（点对话框）发现症状 → 勾选症状筛选药 → 选中药 → 治疗。
/// </summary>
public class ClinicView : BaseUIView
{
    #region 引用
    [Header("药水列表")]
    [SerializeField] private Transform PotionContent;
    [SerializeField] private GameObject PotionPrefab;

    [Header("症状列表")]
    [SerializeField] private Transform SymptomContent;
    [SerializeField] private GameObject SymptomPrefab;
    [SerializeField] private Image CharacterImage;

    [Header("对话框")]
    [SerializeField] private TextMeshProUGUI CharacterName;
    [SerializeField] private TextMeshProUGUI Narration;
    [SerializeField] private Button TreatmentButton;

    [Header("面板与按钮")]
    [SerializeField] private GameObject DiagnosePanel;
    [SerializeField] private GameObject ChoosePanel;
    [SerializeField] private Button NextPatient;
    [SerializeField] private GameObject MadPic;
    [SerializeField] private GameObject HappyPic;

    [Header("其他")]
    [SerializeField] private Button Treat;
    #endregion

    #region 运行时（缓存，避免反复分配）
    // 药水格子缓存：id → item，刷新时复用，不反复 Instantiate/Destroy
    private readonly Dictionary<string, ItemCell> _potionItems = new();
    private string _selectedPotionId;

    private readonly List<string> _potionOrder = new();   // 默认顺序（实例化顺序）
    #endregion

    #region 生命周期
    protected override void OnOpen()
    {
        base.OnOpen();
        ClinicManager.Instance.EnsureDailyPatients();   // 进诊所当天首次生成病人
        OpenChoosePanel();
        SwitchToWaitingMode();
        if(PlayerDataManager.Instance.PlotStep == 9)
        {
            Treat.interactable = false;
        }
    }
    protected override bool OnBack()
    {
        if (DiagnosePanel.activeSelf) { OpenChoosePanel(); return true; }
        return false;
    }
    #endregion

    #region 流程
    /// <summary>点击「下一位病人」。</summary>
    public void onNextPatient()
    {
        var patients = ClinicManager.Instance.GetWaitingPatients();
        Debug.Log(patients.Count);
        if (patients.Count == 0)
        {
            CharacterImage.enabled = false;
            CharacterName.text = "";
            Narration.text = "当前无病人需要看诊";
            return;
        }

        ClinicManager.Instance.StartExamination(patients[0].npcId);
        ShowPatient();
    }

    /// <summary>
    /// 显示当前病人：立绘、名字，并在病人身上放可见症状点（望）。
    /// </summary>
    private void ShowPatient()
    {
        var patient = ClinicManager.Instance.CurrentPatient;
        Debug.Log(patient.npcId);
        if (patient == null) return;

        // 立绘：named 用 NPC 立绘，普通用 Man_X 立绘（由 ClinicManager 决定）
        Sprite portrait = ClinicManager.Instance.GetPatientPortrait(patient);
        Debug.Log(patient.isNamed);
        CharacterImage.sprite = portrait;
        CharacterImage.enabled = portrait != null;

        // 名字：named 用 NPC 名，普通病人显示「病人」
        CharacterName.text = patient.isNamed
            ? (DatabaseManager.Instance.GetNpc(patient.npcId)?.name ?? "")
            : "病人";

        Narration.text = "点击此处询问症状";
        NextPatient.gameObject.SetActive(false);        // 【新增】看诊时隐藏「下一位」
        TreatmentButton.gameObject.SetActive(true);     // 显示「治疗」

        BuildBodySpots(patient);   // 望：在病人身上放可见症状点
        RefreshSymptoms();         // 清空已发现症状列表
        RefreshPotions();          // 刷新药水列表
        var btn = Narration.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnNarrationClick);
        }
        _selectedPotionId = null;
    }

    /// <summary>
    /// 望：给病人身上的「可见症状」创建可点击点。
    /// named 病人用专属立绘（Symptom_{npcId}_{symptom}），普通病人用通用立绘（Symptom.sprite）。
    /// </summary>
    private void BuildBodySpots(Patient patient)
    {
        ClearContent(CharacterImage.transform);

        Disease disease = ClinicManager.Instance.CurrentDisease;
        if (disease == null) return;

        foreach (string sid in disease.symptoms)
        {
            Symptom symptom = DatabaseManager.Instance.GetSymptom(sid);
            if (symptom == null || symptom.displayType != SymptomDisplayType.Visual) continue;

            // 症状立绘：named 用专属图，普通用通用图
            Sprite symptomSprite = ClinicManager.Instance.GetSymptomSprite(patient, sid);
            if (symptomSprite == null) continue;

            GameObject spot = new GameObject(sid, typeof(RectTransform), typeof(Image), typeof(Button));
            spot.transform.SetParent(CharacterImage.transform, false);

            Image img = spot.GetComponent<Image>();
            img.sprite = symptomSprite;
            img.raycastTarget = true;

            RectTransform rt = spot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            string symptomId = sid;                    // 闭包捕获
            Transform spotTransform = spot.transform;  // 闭包捕获 Transform
            spot.GetComponent<Button>().onClick.AddListener(() =>
            {
                ClinicManager.Instance.DiscoverSymptom(symptomId);
                spotTransform.SetAsFirstSibling();     // 【新增】点完置底，让出其它症状
                RefreshSymptoms();
                RefreshPotionVisual();
            });
        }
    }


    /// <summary>问：点击对话框，揭示一个口述症状。</summary>
    public void OnNarrationClick()
    {
        string sid = ClinicManager.Instance.Ask();
        if (string.IsNullOrEmpty(sid)) return;
        // 病人说出症状描述（点一下说一条，说完了自动循环）
        var symptom = DatabaseManager.Instance.GetSymptom(sid);
        if (symptom != null)
            Narration.text = symptom.description;

        RefreshSymptoms();
        RefreshPotionVisual();
    }

    /// <summary>刷新已发现症状列表（Toggle 列表）。</summary>
    private void RefreshSymptoms()
    {
        ClearContent(SymptomContent);
        foreach (var sid in ClinicManager.Instance.GetDiscoveredSymptoms())
        {
            var symptom = DatabaseManager.Instance.GetSymptom(sid);
            if (symptom == null) continue;

            GameObject obj = Instantiate(SymptomPrefab, SymptomContent);
            var item = obj.GetComponent<SymptomItem>() ?? obj.AddComponent<SymptomItem>();
            item.Setup(sid, symptom.name, OnSymptomToggle);
        }
    }

    private void OnSymptomToggle(string symptomId, bool isOn)
    {
        ClinicManager.Instance.SelectSymptom(symptomId, isOn);
        RefreshPotionVisual();
    }
    #endregion

    #region 药水
    private void RefreshPotions()
    {
        ClearContent(PotionContent);
        _potionItems.Clear();
        _potionOrder.Clear();

        //foreach (var potion in DatabaseManager.Instance.AllPotions)
        //{
        //    int count = InventoryManager.Instance.GetCount(potion.id);
        //    if (count <= 0) continue;

        //    GameObject obj = Instantiate(PotionPrefab, PotionContent);
        //    var item = obj.GetComponent<ItemCell>() ?? obj.AddComponent<ItemCell>();
        //    item.Setup(potion.id, potion.icon, count, OnPotionClick);
        //    _potionItems[potion.id] = item;
        //}
        foreach (var potion in DatabaseManager.Instance.AllPotions)
        {
            int count = InventoryManager.Instance.GetCount(potion.id);  // 0 也显示

            GameObject obj = Instantiate(PotionPrefab, PotionContent);
            var item = obj.GetComponent<ItemCell>() ?? obj.AddComponent<ItemCell>();
            item.Setup(potion.id, potion.icon, count.ToString(), OnPotionClick);
            _potionItems[potion.id] = item;
            _potionOrder.Add(potion.id);   // 记录默认顺序
        }
        RefreshPotionVisual();
    }
    /// <summary>应用三种视觉状态 + 匹配的移到最前面（其余保持默认顺序）。</summary>
    private void RefreshPotionVisual()
    {
        var matched = new HashSet<string>();
        foreach (var p in ClinicManager.Instance.GetMatchedPotions()) matched.Add(p.id);
        bool hasSelection = ClinicManager.Instance.GetSelectedSymptoms().Count > 0;

        // 1. 视觉状态：选中 > 匹配 > 默认
        foreach (var kv in _potionItems)
        {
            if (kv.Key == _selectedPotionId)
                kv.Value.SetVisual(ItemVisual.Selected);
            else if (hasSelection && matched.Contains(kv.Key))
                kv.Value.SetVisual(ItemVisual.Matched);
            else
                kv.Value.SetVisual(ItemVisual.Default);
        }

        // 2. 匹配的移到最前面（保持默认顺序），其余按默认顺序排后面
        int idx = 0;
        foreach (var pid in _potionOrder)
            if (hasSelection && matched.Contains(pid))
                _potionItems[pid].transform.SetSiblingIndex(idx++);
        foreach (var pid in _potionOrder)
            if (!(hasSelection && matched.Contains(pid)))
                _potionItems[pid].transform.SetSiblingIndex(idx++);
    }
    private void OnPotionClick(string potionId)
    {
        // 再点一次当前选中的 → 取消选择
        if (_selectedPotionId == potionId)
        {
            _selectedPotionId = null;
            Narration.text = "点击此处询问症状";
        }
        else
        {
            _selectedPotionId = potionId;
            var potion = DatabaseManager.Instance.GetPotion(potionId);
            Narration.text = potion != null ? $"是要使用 {potion.name} 治疗吗？" : "";
        }
        RefreshPotionVisual();   // 重新应用颜色/放大/顺序
    }

    #endregion

    #region 治疗
    public void onTreatmentClicked()
    {
        if (string.IsNullOrEmpty(_selectedPotionId))
        {
            PopupManager.Instance.ShowConfirm("请选择药水", "您需要选择一个药水为病人进行治疗", null);
            return;
        }

        PopupManager.Instance.ShowConfirm("确认治疗", "确定使用这个药水吗？", onConfirmTreat);
    }

    private void onConfirmTreat()
    {
        TreatResult result = ClinicManager.Instance.Treat(_selectedPotionId);

        switch (result)
        {
            case TreatResult.Success:
                Narration.text = "我好了！谢谢医生！";
                HappyPic.SetActive(true);
                MadPic.SetActive(false);
                break;

            case TreatResult.NoMedicine:
                Narration.text = "抱歉，现在药品不够了，明天来取药吧";
                HappyPic.SetActive(false);
                MadPic.SetActive(false);
                break;

            case TreatResult.WrongMedicine:
                Narration.text = "庸医啊！赔钱！";
                HappyPic.SetActive(false);
                MadPic.SetActive(true);
                break;
            case TreatResult.Error:
                //1.此时一定是选择了某个药品，以及有病人才能进入，要是还是Error，只能是系统错误，所有debug就行
                Debug.LogError($"数据出错请检查,当前药品id：{_selectedPotionId}");
                Narration.text = "";
                break;
        }

        TreatmentButton.interactable = false;

        // 三种情况点对话框都回到等待下一位
        var btn = Narration.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(SwitchToWaitingMode);
        }
    }
    #endregion

    #region 面板控制
    private void OpenDiagnosePanel() { DiagnosePanel.SetActive(true); ChoosePanel.SetActive(false); NextPatient.gameObject.SetActive(true); }
    private void OpenChoosePanel() { DiagnosePanel.SetActive(false); ChoosePanel.SetActive(true); }

    private void SwitchToWaitingMode()
    {
        NextPatient.gameObject.SetActive(true);
        TreatmentButton.gameObject.SetActive(false);
        TreatmentButton.interactable = true;
        MadPic.SetActive(false);
        HappyPic.SetActive(false);

        CharacterImage.enabled = false;
        CharacterName.text = "";
        Narration.text = "请点击下一位问诊病人";
        var btn = Narration.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
        }
        ClearContent(SymptomContent);
        ClearContent(CharacterImage.transform);
        ClearContent(PotionContent);
        _potionItems.Clear();
        _selectedPotionId = null;
    }


    public void onDiagnose() => OpenDiagnosePanel();

    /// <summary>
    /// 点击休息立牌
    /// </summary>
    public void OnMainMenu()
    {
        //切换至主菜单
        _ = UIManager.Instance.SwitchPanelAsync(
        openKey: UIID._mainmenu,
        openData: new UIPanelData { ClearStack = true },
        closeKey: UIID._clinic,
        duringBlack: null,
        fade: true
    );
    }
    public void OnTown()
    {
        //回到可移动场景中的主角小屋门口，并可以正常行动
        _ = UIManager.Instance.SwitchPanelAsync(
       openKey: UIID._hud,
       closeKey: UIID._clinic,
       duringBlack: () =>
       {
           PlayerManager.Instance.ActivatePlayerObject(Location._home);
           PlayerManager.Instance.ThawPlayer();
       },
       fade: true
   );
        PlayerManager.Instance.ActivatePlayerObject(Location._home);   // 小屋门口位置
    }
    #endregion

    #region 工具
    private void ClearContent(Transform content)
    {
        // 内存安全：只销毁子物体，列表引用在上层 Clear
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }
    #endregion
}