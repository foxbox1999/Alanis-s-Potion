using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>采集界面 View：展示 + 点击，业务逻辑走 GatherManager / DatabaseManager。</summary>
public class GatherView : BaseUIView
{
    [Header("地区按钮")]
    [SerializeField] private Button PetranMountain;
    [SerializeField] private Button TheslamWilds;
    [SerializeField] private Button HissingForest;
    [SerializeField] private Button RareMoundThicket;
    [SerializeField] private Button BackGround;

    [Header("信息面板")]
    [SerializeField] private GameObject ShowHarvestPanel;
    [SerializeField] private Image ZoneImage;
    [SerializeField] private TextMeshProUGUI ZoneName;
    [SerializeField] private Transform HarvestsContent;
    [SerializeField] private GameObject HarvestPrefab;
    [SerializeField] private Button ConfirmButton;

    [Header("采集动画")]
    [SerializeField] private GameObject GatherImage;   // Image.prefab 覆盖层（从旧 GatherController.GatherImage 移过来）

    [Header("配置")]
    [SerializeField] private float _fadeDuration = 0.25f;

    private string _currentZoneId;
    private Coroutine _fadeRoutine;

    protected override void OnOpen()
    {
        base.OnOpen();
        BindButtons();
        ShowHarvestPanel.SetActive(false);
        GatherImage.SetActive(false);
        if(PlayerDataManager.Instance.PlotStep == 9)
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
    }

    /// <summary>
    /// 构建按钮绑定
    /// </summary>
    private void BindButtons()
    {
        PetranMountain.onClick.RemoveAllListeners();
        TheslamWilds.onClick.RemoveAllListeners();
        HissingForest.onClick.RemoveAllListeners();
        RareMoundThicket.onClick.RemoveAllListeners();

        PetranMountain.onClick.AddListener(() => OnZoneClick("Zone_Petran_Mountain"));
        TheslamWilds.onClick.AddListener(() => OnZoneClick("Zone_Theslam_Wilds"));
        HissingForest.onClick.AddListener(() => OnZoneClick("Zone_Hissing_Forest"));
        RareMoundThicket.onClick.AddListener(() => OnZoneClick("Zone_Rare_Mound_Thicket"));

        BackGround.onClick.RemoveAllListeners();
        BackGround.onClick.AddListener(OnCancelSelect);

        ConfirmButton.onClick.RemoveAllListeners();
        ConfirmButton.onClick.AddListener(OnConfirmGather);
    }

    private void OnZoneClick(string zoneId)
    {
        //1.将当前选择地区置为目标
        _currentZoneId = zoneId;
        //2.更新对应地区的面板具体信息
        UpdatePanelInfo(zoneId);
        //3.淡入面板
        FadePanel(ShowHarvestPanel, 0f, 1f);
    }

    private void UpdatePanelInfo(string zoneId)
    {
        Zone zone = DatabaseManager.Instance.GetZone(zoneId);
        if (zone != null) { ZoneImage.sprite = zone.icon; ZoneName.text = zone.name; }

        for (int i = HarvestsContent.childCount - 1; i >= 0; i--)
            Destroy(HarvestsContent.GetChild(i).gameObject);

        foreach (var herb in DatabaseManager.Instance.GetHerbsByZone(zoneId))
            BuildHerbCell(herb);
    }

    private void BuildHerbCell(Herb herb)
    {
        GameObject obj = Instantiate(HarvestPrefab, HarvestsContent);
        var item = obj.GetComponent<ItemCell>();
        item.Setup(herb.id, herb.icon, $"{herb.minQuantity}~{herb.maxQuantity}", null);
        obj.SetActive(true);
    }

    private void OnCancelSelect()
    {
        _currentZoneId = null;
        FadePanel(ShowHarvestPanel, 1f, 0f);
    }

    private void OnConfirmGather()
    {
        if (string.IsNullOrEmpty(_currentZoneId)) return;
        FadePanel(ShowHarvestPanel, 1f, 0f);
        GatherManager.Instance.StartGather(_currentZoneId);
        GatherImage.SetActive(true);   // 播帧动画，动画结束回调 onAnimOver
    }

    private void FadePanel(GameObject panel, float from, float to)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(group, from, to));
    }

    private IEnumerator FadeRoutine(CanvasGroup group, float from, float to)
    {
        if (to > 0f)
        {
            group.gameObject.SetActive(true);
            group.alpha = from;
            group.blocksRaycasts = true;
            group.interactable = true;
        }
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / _fadeDuration));
            yield return null;
        }
        group.alpha = to;
        if (to <= 0f)
        {
            group.gameObject.SetActive(false);
            group.blocksRaycasts = false;
            group.interactable = false;
        }
        _fadeRoutine = null;
    }

    public T GetComponentInStrictChildren<T>(GameObject go) where T : Component
    {
        T[] components = go.GetComponentsInChildren<T>();
        foreach (T comp in components) if (comp.gameObject != go) return comp;
        return null;
    }
}