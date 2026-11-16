using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>炼药界面（魔法书）：选药方 → 看配方 → 开始炼制 → 动画。字段名沿用旧预制体，勿改名。</summary>
public class AlchemyView : BaseUIView
{
    [Header("描述框")]
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI TreatableDiseases;
    [SerializeField] private TextMeshProUGUI description2;
    [SerializeField] private TextMeshProUGUI TreatableDiseases2;

    [Header("炼制时间")]
    [SerializeField] private TextMeshProUGUI CauldronTime;

    [Header("强化提示框")]
    [SerializeField] private Image NormalPotion;
    [SerializeField] private Image ReinforcedPotion;

    [Header("平移控制")]
    [SerializeField] private RectTransform BookPanel;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float bookOffsetX = 520f;   // 书右移距离（约520）

    [Header("药品选择界面")]
    [Tooltip("上一页按钮")]
    [SerializeField] private Button PreviousPageButton;
    [Tooltip("下一页按钮")]
    [SerializeField] private Button NextPageButton;
    [SerializeField] private List<GameObject> Potions;

    [Header("药品详情界面")]
    [SerializeField] private GameObject ShowedPotion;
    [SerializeField] private TextMeshProUGUI NeededTime;
    [SerializeField] private List<GameObject> Materials;
    [Tooltip("开始炼制按钮")]
    [SerializeField] private Button StartCauldronButton;
    [SerializeField] private GameObject ReinforcedPanel;

    [Header("书本切换")]
    [SerializeField] private Image CurrentBook;
    [SerializeField] private GameObject ChoosePotionPanel;
    [SerializeField] private GameObject PotionInfoPanel;

    [Header("书本图片")]
    [SerializeField] private List<Sprite> BookSprites;

    [Header("炼药动画")]
    [SerializeField] private Animator SpoonAnimator;
    [SerializeField] private Animator VapourAnimator;

    private const int PageSize = 6;
    private readonly List<Potion> _allPotions = new();
    private int _currentPage;
    private string _selectedPotionId;
    private int _lastSecond = -1;

    private Vector2 _bookClosedPos;   // 原位（占屏幕大半）
    private Vector2 _bookOpenPos;     // 右移520后的位置
    private Coroutine _bookMoveRoutine;
    private bool _isBrewing;          // 书在打开位（炼制中/已完成待确认）
    private bool _finishedPopupShown; // 完成弹窗是否已弹出
    private void Awake()
    {
        // 只记一次「关书位」。之后从对象池复用，anchoredPosition 可能已被移动过，不能重记。
        _bookClosedPos = BookPanel.anchoredPosition;
        _bookOpenPos = _bookClosedPos + new Vector2(bookOffsetX, 0f);
    }
    protected override void OnOpen()
    {
        base.OnOpen();
        BindButtons();
        LoadPotions();
        ShowPotionSelectionPanel();

        // 重进：按任务状态直接把书摆到位（不播平移动画）
        var job = AlchemyManager.Instance.CurrentJob;
        _finishedPopupShown = false;
        if (job == null)
        {
            // 情况1：没在炼药 → 书在原位
            _isBrewing = false;
            BookPanel.anchoredPosition = _bookClosedPos;
            SetBrewingUI(false);
        }
        else if (job.IsDone)
        {
            // 情况3：已炼好 → 书在打开位 + 弹窗
            _isBrewing = true;
            BookPanel.anchoredPosition = _bookOpenPos;
            SetBrewingUI(true);
            ShowFinishedPopup();
        }
        else
        {
            // 情况2：炼制中 → 书在打开位 + 动画 + 正确倒计时
            _isBrewing = true;
            BookPanel.anchoredPosition = _bookOpenPos;
            SetBrewingUI(true);
            UpdateCountdown(job);
        }

        if (PlayerDataManager.Instance.PlotStep == 10)
        {
            AlchemyTeach();
        }
    }

    public void AlchemyTeach()
    {
        GuideManager.Instance.StartGuide(new List<GuideStep>
                {
                    new() { targetName = "FirstPotion", tipText = "刚刚采集到的药材和之前剩余的药材，可以炼制一个退烧药剂。。", arrow = GuideArrowDir.Up },
                }, () =>
                {
                    OnPotionClick("Potion_Fever_Cure");
                    GuideManager.Instance.StartGuide(new List<GuideStep>
                {
                    new() { targetName = "MainDescription", tipText = "这里可以查看药效", arrow = GuideArrowDir.Up },
                    new() { targetName = "Material_0", tipText = "这里可以查看药材", arrow = GuideArrowDir.Up },
                    new() { targetName = "Material_0", tipText = "这里还可以查看需求数量，以及药材的位置", arrow = GuideArrowDir.Up },
                    new() { targetName = "StartRefining", tipText = "确认无误就可以开始炼制药剂了。", arrow = GuideArrowDir.DownR }
                }, () => {
                    PlayerDataManager.Instance.SetPlotStep(11);
                });
                });
        PlayerDataManager.Instance.SetPlotStep(11);
    }

    protected override void OnRefresh()
    {
        base.OnRefresh();
        RefreshCauldron();
        RefreshInfoPanel();
    }

    protected override bool OnBack()
    {
        if (PotionInfoPanel.activeSelf)
        {
            ShowPotionSelectionPanel();
            _selectedPotionId = null;
            return true;
        }
        return false;
    }

    private void BindButtons()
    {
        PreviousPageButton.onClick.RemoveAllListeners();
        PreviousPageButton.onClick.AddListener(OnPreviousPageButtonClicked);
        NextPageButton.onClick.RemoveAllListeners();
        NextPageButton.onClick.AddListener(OnNextPageButtonClicked);
        StartCauldronButton.onClick.RemoveAllListeners();
        StartCauldronButton.onClick.AddListener(OnStartCauldronButtonClicked);
    }

    private void LoadPotions()
    {
        _allPotions.Clear();
        foreach (var p in DatabaseManager.Instance.AllPotions) _allPotions.Add(p);
        _currentPage = 0;
        UpdatePotionList();
    }

    private void UpdatePotionList()
    {
        int start = _currentPage * PageSize;
        for (int i = 0; i < Potions.Count; i++)
        {
            GameObject btn = Potions[i];
            int index = start + i;
            if (index >= _allPotions.Count) { btn.SetActive(false); continue; }

            Potion potion = _allPotions[index];
            btn.SetActive(true);
            btn.GetComponent<Image>().sprite = potion.icon;
            btn.GetComponentInChildren<TextMeshProUGUI>().text = potion.name;
            Transform qty = btn.transform.Find("QuantityText");
            if (qty != null) qty.GetComponent<TextMeshProUGUI>().text = InventoryManager.Instance.GetCount(potion.id).ToString();

            string id = potion.id;
            btn.GetComponent<Button>().onClick.RemoveAllListeners();
            btn.GetComponent<Button>().onClick.AddListener(() => OnPotionClick(id));
        }
    }

    private void OnPotionClick(string potionId)
    {
        _selectedPotionId = potionId;
        ShowPotionRecipe(potionId);
        ShowPotionRecipePanel();
    }

    private void OnNextPageButtonClicked()
    {
        if ((_currentPage + 1) * PageSize < _allPotions.Count) { _currentPage++; UpdatePotionList(); }
    }

    private void OnPreviousPageButtonClicked()
    {
        if (_currentPage > 0) { _currentPage--; UpdatePotionList(); }
    }

    private void ShowPotionRecipe(string potionId)
    {
        var potion = DatabaseManager.Instance.GetPotion(potionId);
        if (potion == null) return;

        ShowedPotion.GetComponent<Image>().sprite = potion.icon;
        ShowedPotion.GetComponentInChildren<TextMeshProUGUI>().text = potion.name;
        description2.text = potion.description;
        TreatableDiseases2.text = DatabaseManager.Instance.GetDisease(potion.diseaseId)?.name ?? "";
        NeededTime.text = $"{potion.recipe.timeRequired}s";

        for (int i = 0; i < Materials.Count; i++)
        {
            GameObject m = Materials[i];
            if (i < potion.recipe.ingredients.Count)
            {
                var ing = potion.recipe.ingredients[i];
                var item = DatabaseManager.Instance.GetItem(ing.itemId);
                m.SetActive(true);
                m.GetComponent<Image>().sprite = item != null ? item.icon : null;
                m.GetComponentInChildren<TextMeshProUGUI>().text = item != null ? item.name : ing.itemId;

                Transform need = m.transform.Find("MaterialNeededQuantity");
                if (need != null) need.GetComponent<TextMeshProUGUI>().text = ing.quantity.ToString();
                Transform have = m.transform.Find("MaterialInventoryQuantity");
                if (have != null) have.GetComponent<TextMeshProUGUI>().text = InventoryManager.Instance.GetCount(ing.itemId).ToString();
            }
            else m.SetActive(false);
        }

        UpdateEnhancedHint(potion);
    }

    private void UpdateEnhancedHint(Potion basePotion)
    {
        Potion upgrade = null;
        foreach (var p in DatabaseManager.Instance.AllPotions)
            foreach (var ing in p.recipe.ingredients)
                if (ing.itemId == basePotion.id) { upgrade = p; break; }

        bool has = upgrade != null;
        ReinforcedPanel.SetActive(has);
        NormalPotion.enabled = has;
        ReinforcedPotion.enabled = has;
        if (has) { NormalPotion.sprite = basePotion.icon; ReinforcedPotion.sprite = upgrade.icon; }
    }

    private void OnStartCauldronButtonClicked()
    {
        if (string.IsNullOrEmpty(_selectedPotionId)) return;
        if (AlchemyManager.Instance.StartBrew(_selectedPotionId))
        {
            // 情况2：开始炼制 → 书右移520（动画），之后按钮一直不可点
            _isBrewing = true;
            SetBrewingUI(true);
            MovePanel(_bookClosedPos, _bookOpenPos, duration);
            RefreshCauldron();
            RefreshInfoPanel();
        }
        else PopupManager.Instance.ShowSidePrompt("材料不足或已在炼制中");
    }

    private void Update()
    {
        if (!_isBrewing) return;
        RefreshCauldron();
    }

    private void RefreshCauldron()
    {
        var job = AlchemyManager.Instance.CurrentJob;
        if (job == null) return;

        if (job.IsDone)
        {
            if (!_finishedPopupShown)
            {
                _finishedPopupShown = true;
                ShowFinishedPopup();
            }
            return;
        }

        UpdateCountdown(job);
    }

    /// <summary>设置炼制相关的 UI（动画/倒计时/按钮）。</summary>
    private void SetBrewingUI(bool brewing)
    {
        if (SpoonAnimator != null) SpoonAnimator.enabled = brewing;
        if (VapourAnimator != null) VapourAnimator.enabled = brewing;
        CauldronTime.enabled = brewing;
        StartCauldronButton.interactable = !brewing;
    }

    /// <summary>炼制完成：弹窗，等确认后再领取并恢复书。</summary>
    private void ShowFinishedPopup()
    {
        var result = AlchemyManager.Instance.PeekResult();
        if (result == null || result.Count == 0)
        {
            AlchemyManager.Instance.TryClaim();
            _isBrewing = false;
            SetBrewingUI(false);
            MovePanel(_bookOpenPos, _bookClosedPos, duration);
            return;
        }

        PopupManager.Instance.ShowItem(result, onConfirm: () =>
        {
            AlchemyManager.Instance.TryClaim();      // 确认后才真正领取
            _isBrewing = false;
            SetBrewingUI(false);
            MovePanel(_bookOpenPos, _bookClosedPos, duration);   // 书恢复到原位
            _finishedPopupShown = false;
        });
    }

    private void RefreshInfoPanel()
    {
        if (_selectedPotionId != null)
            ShowPotionRecipe(_selectedPotionId);
    }

    private void UpdateCountdown(AlchemyManager.AlchemyJob job)
    {
        int sec = Mathf.CeilToInt(job.remaining);   // ★ remaining 现在是真实秒
        if (sec == _lastSecond) return;
        _lastSecond = sec;
        CauldronTime.text = $"{sec / 60:D2} : {sec % 60:D2}";
    }

    private void ShowPotionSelectionPanel()
    {
        CurrentBook.sprite = BookSprites[0];
        ChoosePotionPanel.SetActive(true);
        PotionInfoPanel.SetActive(false);
        ReinforcedPanel.SetActive(false);
    }

    private void ShowPotionRecipePanel()
    {
        CurrentBook.sprite = BookSprites[1];
        ChoosePotionPanel.SetActive(false);
        PotionInfoPanel.SetActive(true);
    }

    private void MovePanel(Vector2 from, Vector2 to, float dur)
    {
        if (_bookMoveRoutine != null) StopCoroutine(_bookMoveRoutine);
        _bookMoveRoutine = StartCoroutine(MoveXCoroutine(from, to, dur));
    }

    private IEnumerator MoveXCoroutine(Vector2 from, Vector2 to, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            BookPanel.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / dur));
            yield return null;
        }
        BookPanel.anchoredPosition = to;
        _bookMoveRoutine = null;
    }
}