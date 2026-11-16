using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntelligenceViews : BaseUIView
{
    [Header("标签选择")]
    [SerializeField] private Button Witcher;
    [SerializeField] private Button Monster;
    [SerializeField] private Button Church;
    [SerializeField] private Button Villager;

    [Header("背景图片")]
    [SerializeField] private Image BackgroundImage;

    [Header("infoPanel")]
    [SerializeField] private GameObject InfoPanel;
    [SerializeField] private Sprite InfoBackGround;
    [SerializeField] private Image CharacterImage;
    [SerializeField] private TextMeshProUGUI characetrName;
    [SerializeField] private TextMeshProUGUI interest;
    [SerializeField] private TextMeshProUGUI personality;
    [SerializeField] private TextMeshProUGUI description;

    [Header("选择面板")]
    [SerializeField] private GameObject SelectionPanel;
    [SerializeField] private Sprite SelectionBackGround;
    [SerializeField] private GameObject CharacterPrefab;
    [SerializeField] private Image SuspectedValue;
    [SerializeField] private Image Favorability;
    [SerializeField] private Transform CharacterContent;

    [Header("换页")]
    [SerializeField] private Button PreviousPage;
    [SerializeField] private Button NextPage;

    [Header("sprite素材")]
    [SerializeField] private Sprite NoValue;
    [SerializeField] private List<valuesprites> values;

    [System.Serializable]
    public class valuesprites { public int value; public Sprite sprite; }

    private int currentIndex = 0;
    private FactionType currentFaction = FactionType.Witch;
    private List<Npc> currentIntelligences = new();

    protected override void OnOpen()
    {
        base.OnOpen();
        closeAllMotion();
        openSelectionPanel();
        onWitcherButton();
    }

    protected override bool OnBack()
    {
        if (InfoPanel.activeSelf)
        {
            openSelectionPanel();
            return true;   // 详情页 → 先回选择页
        }
        return false;      // 交给栈返回
    }

    // ===== 面板切换 =====
    private void openInfoPanel() { InfoPanel.SetActive(true); SelectionPanel.SetActive(false); BackgroundImage.sprite = InfoBackGround; }
    private void openSelectionPanel() { InfoPanel.SetActive(false); SelectionPanel.SetActive(true); BackgroundImage.sprite = SelectionBackGround; }

    private void closeAllMotion()
    {
        Favorability.sprite = NoValue;
        SuspectedValue.sprite = NoValue;
    }

    private void OpenFavorability() { Favorability.sprite = values.Find(x => x.value == 0).sprite; SuspectedValue.sprite = NoValue; }
    private void OpenSuspected() { SuspectedValue.sprite = values.Find(x => x.value == 0).sprite; Favorability.sprite = NoValue; }

    // ===== 核心 =====
    private void updatePanel()
    {
        openSelectionPanel();
        ClearSelectionPanel();

        // 只显示已认识的 NPC
        currentIntelligences = SocialManager.Instance.GetMetNpcs(currentFaction);

        foreach (var npc in currentIntelligences)
        {
            GameObject characterButton = Instantiate(CharacterPrefab, CharacterContent);
            characterButton.name = npc.id;
            ToolMethods.GetComponentInStrictChildren<Image>(characterButton).sprite = npc.sprite;   // ★ QSprite → sprite
            characterButton.GetComponentInChildren<Toggle>().group = CharacterContent.GetComponent<ToggleGroup>();
            Button button = characterButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onCharacterPointFirst(button));
        }

        if (currentIntelligences.Count > 0)
            updateCharacterMotion(currentIntelligences[0]);
    }

    private void ClearSelectionPanel()
    {
        foreach (Transform child in CharacterContent) Destroy(child.gameObject);
    }

    private void onCharacterPointFirst(Button button)
    {
        Toggle tog = button.GetComponentInChildren<Toggle>();
        if (tog.isOn == false)
        {
            updateCharacterMotion(currentIntelligences[button.transform.GetSiblingIndex()]);
            tog.isOn = true;
            return;
        }
        int index = button.transform.GetSiblingIndex();
        openInfoPanel();
        updateCharacter(currentIntelligences[index]);
    }

    private void updateCharacter(Npc npc)
    {
        CharacterImage.sprite = npc.sprite;
        characetrName.text = npc.name;
        interest.text = npc.preference;
        personality.text = npc.personality;
        description.text = npc.description;
    }

    public void updateCharacterMotion(Npc npc)
    {
        int value = 0;
        switch (currentFaction)
        {
            case FactionType.Witch:
                OpenFavorability();
                value = SocialManager.Instance.GetFavor(npc.id);
                int showValue = (int)(value / 10);
                int currentShowValue = showValue * 10;
                changeFavorabilityPic(currentShowValue);
                break;
            case FactionType.Church:
                value = SocialManager.Instance.GetSuspicion(npc.id);
                int ChurchSus = (int)(value / 10) * 10;   // ★ 取整到 10 的倍数，和好感度一致
                changeSuspicionPic(ChurchSus);
                break;
            case FactionType.Townsfolk:
                OpenSuspected();
                value = SocialManager.Instance.GetSuspicion(npc.id);
                int TownSus = (int)(value / 10) * 10;   // ★ 取整到 10 的倍数，和好感度一致
                changeSuspicionPic(TownSus);
                break;
        }
    }

    private void changeSuspicionPic(int value)
    {
        var match = values.Find(x => x.value == value);
        if (match == null)
        {
            Debug.LogWarning($"[Intelligence] 找不到怀疑度档位 {value}，用第一个档位代替");
            match = values.Count > 0 ? values[0] : null;
        }
        if (match != null) SuspectedValue.sprite = match.sprite;
    }
    private void changeFavorabilityPic(int value) => Favorability.sprite = values.Find(x => x.value == value).sprite;

    // ===== 翻页 =====
    public void onPreviousPage()
    {
        if (currentIndex > 0) { currentIndex--; updateCharacter(currentIntelligences[currentIndex]); }
    }
    public void onNextPage()
    {
        if (currentIndex < currentIntelligences.Count - 1) { currentIndex++; updateCharacter(currentIntelligences[currentIndex]); }
    }

    // ===== 标签 =====
    public void onWitcherButton() { currentFaction = FactionType.Witch; OpenFavorability(); currentIndex = 0; updatePanel(); }
    public void onChurchButton() { currentFaction = FactionType.Church; OpenSuspected(); currentIndex = 0; updatePanel(); }
    public void onVillagerButton() { currentFaction = FactionType.Townsfolk; OpenSuspected(); currentIndex = 0; updatePanel(); }

    public void onMonsterButton()
    {
        PopupManager.Instance.ShowSidePrompt("该内容还在绝赞开发中，敬请期待");
    }
}