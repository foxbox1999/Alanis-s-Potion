using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static CharacterSpriteConfig;

public class DialogueView : BaseUIView
{
    #region Config
    [Header("UI组件")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform leftCharacterContainer;
    [SerializeField] private Transform rightCharacterContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Transform optionContainer;
    [SerializeField] private SpriteCharacterDisplay spriteDisplayPrefab;
    [SerializeField] private Image backgroundImage;
    private DialogueManager controller;
    private CharacterSpriteConfig spriteConfig;
    private string protagonistId = "艾拉妮丝";
    private float typewriterSpeed = 0.02f;

    private ICharacterDisplay leftDisplay, rightDisplay;
    private CharacterSpriteConfig.CharacterData leftData, rightData;
    private string leftExpression, rightExpression;
    private Coroutine typewriterCoroutine;
    private string currentFullText;
    private bool isTyping;
    #endregion

    #region Life
    public void Initialize(DialogueManager ctrl, CharacterSpriteConfig config)
    {
        controller = ctrl;
        spriteConfig = config;
        spriteConfig.InitAllCharacters();
        protagonistId = "艾拉妮丝";
    }

    public void Initialize()
    {

    }

    public void UpdataUI()
    {

    }

    public void RemoveUI()
    {
        leftDisplay?.Hide();
        rightDisplay?.Hide();
    }

    public bool onBackButton()
    {
        return false;
    }

    public void onSkip()
    {
        EventBus.Instance.Publish(new RequestEndDialogue());
    }
    #endregion

    #region Tools
    void SetImageAlpha(Image targetImage, float alpha)
    {
        Color color = targetImage.color;
        color.a = alpha;          // alpha 范围 0（完全透明） 到 1（完全不透明）
        targetImage.color = color;
    }

    private void ClearOptions()
    {
        foreach (Transform child in optionContainer) Destroy(child.gameObject);
    }

    //打字机协程
    private IEnumerator TypewriterEffect(string fullText)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        isTyping = false;
    }

    //继续按钮事件
    public void OnContinuePressed()
    {
        if (isTyping)
        {
            StopCoroutine(typewriterCoroutine);
            dialogueText.text = currentFullText;
            isTyping = false;
        }
        else controller.OnContinue();
    }
    #endregion

    #region Core
    public void ShowDialogue(DialogueRow row, Sprite BackGround = null)
    {
        ClearOptions();
        nameText.text = row.characterId;
        currentFullText = row.content;
        if (BackGround == null)
        {
            SetImageAlpha(backgroundImage, 0f);
        }
        else
        {
            SetImageAlpha(backgroundImage, 1f);
        }
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterEffect(currentFullText));

        bool isProtagonist = (row.characterId == protagonistId);
        //Sprite characterSprite = null;
        var charData = spriteConfig.GetCharacter(row.characterId);

        // 特殊情况：主角的 "nielian" 表情 → 使用动态合成的捏脸立绘
        if (isProtagonist && row.expression == "nielian")
        {
            if (charData.expressions.Exists(e => e.expressionName == "nielian") && charData.expressions.Find(e => e.expressionName == "nielian").sprite != null)
            {
                // 如果已经存在捏脸数据，直接使用
                row.expression = "nielian";
            }
            else
            {
                Debug.LogWarning($"请求失败,失败原因：{charData.expressions.Exists(e => e.expressionName == "nielian")},{charData.expressions.Find(e => e.expressionName == "nielian").sprite != null},{PlayerDataManager.Instance.PlayerAppearance == null}");
                // 如果不存在捏脸数据，回退到主角的默认表情
                row.expression = "normal";
            }
        }

        if (isProtagonist)
        {
            ShowCharacter(ref leftDisplay, leftCharacterContainer, charData, row.expression,
                         ref leftData, ref leftExpression);
            rightDisplay?.Hide();
        }
        else
        {
            ShowCharacter(ref rightDisplay, rightCharacterContainer, charData, row.expression,
                         ref rightData, ref rightExpression);
            leftDisplay?.Hide();
        }
    }
    public void ShowOptions(List<DialogueRow> options)
    {
        dialogueText.text = "";
        ClearOptions();
        foreach (var opt in options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionContainer);
            var btn = btnObj.GetComponent<Button>();
            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            label.text = opt.content;
            int index = options.IndexOf(opt);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => controller.OnOptionSelected(index));
        }
    }

    public void RefreshCharacterDisplay(string characterId, string expression)
    {
        bool isProtagonist = (characterId == protagonistId);
        var charData = spriteConfig.GetCharacter(characterId);
        if (isProtagonist && leftData?.characterId == characterId)
            ShowCharacter(ref leftDisplay, leftCharacterContainer, charData, expression, ref leftData, ref leftExpression);
        else if (!isProtagonist && rightData?.characterId == characterId)
            ShowCharacter(ref rightDisplay, rightCharacterContainer, charData, expression, ref rightData, ref rightExpression);
    }

    private void ShowCharacter(ref ICharacterDisplay display, Transform container,
        CharacterSpriteConfig.CharacterData data, string expression,
        ref CharacterSpriteConfig.CharacterData cachedData, ref string cachedExpression)
    {
        if (data == null) { display?.Hide(); return; }
        if (cachedData != data || display == null)
        {
            display?.Hide();
            display = Instantiate(spriteDisplayPrefab, container);
            cachedData = data;
        }
        display.Show(container, data, expression);
        cachedExpression = expression;
    }

    #endregion

    #region Logs
    public void Log(string message)
    {
        Debug.Log($"[DialogueView]: {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[DialogueView]: {message}");
    }
    public void LogWarning(string message)
    {
        Debug.LogWarning($"[DialogueView]: {message}");
    }
    #endregion

}
