using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 引导提示框：按方向切 5 张图 + 名字查找目标 + 全屏透明点击拦截。
/// 预制件层级：
/// GuideTip（根，全屏拉伸）
/// ├── TipGroup（框本体，挂 Image）
/// │    └── TipText（文字）
/// └── ClickCatcher（全屏透明 Image+Button，最后一个子节点 = 最上层）
/// </summary>
public class GuideTip : MonoBehaviour
{
    [Header("框")]
    [SerializeField] private RectTransform _tipGroup;

    [Header("文字")]
    [SerializeField] private TextMeshProUGUI _tipText;

    [Header("5 张美术图")]
    [SerializeField] private Sprite _spriteUp;
    [SerializeField] private Sprite _spriteDownL;
    [SerializeField] private Sprite _spriteDownR;
    [SerializeField] private Sprite _spriteLeft;
    [SerializeField] private Sprite _spriteRight;

    [Header("全屏拦截")]
    [SerializeField] private Button _clickCatcher;

    //[Header("定位")]
    //[SerializeField] private float _gap = 20f;

    private Image _boxImage;
    private Action _onClick;

    private void Awake() => _boxImage = _tipGroup.GetComponent<Image>();

    public void Setup(GuideStep step, Action onClick)
    {
        _onClick = onClick;

        // 1. 文字
        _tipText.text = step.tipText;

        // 2. 切图 + 还原尺寸
        _boxImage.sprite = GetSprite(step.arrow);
        _boxImage.SetNativeSize();

        // 3. 拦截层点击 → 下一步
        _clickCatcher.onClick.RemoveAllListeners();
        _clickCatcher.onClick.AddListener(() => _onClick?.Invoke());

        // 4. 定位
        PositionAt(step);
    }

    private Sprite GetSprite(GuideArrowDir dir) => dir switch
    {
        GuideArrowDir.Up => _spriteUp,
        GuideArrowDir.DownL => _spriteDownL,
        GuideArrowDir.DownR => _spriteDownR,
        GuideArrowDir.Left => _spriteLeft,
        GuideArrowDir.Right => _spriteRight,
        _ => _spriteUp
    };

    private void PositionAt(GuideStep step)
    {
        if (string.IsNullOrEmpty(step.targetName)) return;

        // 名字查找目标
        var target = GameObject.Find(step.targetName)?.GetComponent<RectTransform>();
        if (target == null)
        {
            Debug.LogWarning($"[GuideTip] 找不到引导目标: {step.targetName}");
            return;
        }

        // 目标屏幕位置 → 父节点本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_tipGroup.parent, target.position, null, out Vector2 targetLocal);

        Vector2 targetSize = target.sizeDelta;
        // 按框尺寸算偏移
        Vector2 size = _tipGroup.sizeDelta;
        _tipGroup.localPosition = targetLocal + GetOffset(step.arrow, size, targetSize);
    }

    private Vector2 GetOffset(GuideArrowDir dir, Vector2 size, Vector2 targetSize)
    {
        float halfW = size.x * 0.5f; //图片宽度的一半
        float halfH = size.y * 0.5f; //图片高度的一半
        return dir switch
        {
            GuideArrowDir.Up => new Vector2(0f, -halfH - (targetSize.y / 2)), //在目标的定位基础上，向下平移半高
            GuideArrowDir.DownL => new Vector2(halfW - 470, halfH + (targetSize.y / 2)), //在目标的基础上，向上平移半高，向右平移半宽
            GuideArrowDir.DownR => new Vector2(-halfW + 470, halfH + (targetSize.y / 2)),   //在目标的基础上，向上平移半高，应该还要加半高目标的大小，向左平移半宽
            GuideArrowDir.Left => new Vector2(halfW + (targetSize.x / 2) - 50, 266f), //向右半目标 右是+
            GuideArrowDir.Right => new Vector2(-halfW - (targetSize.x / 2), 197f), //向左半目标 左是-
            _ => Vector2.zero
        };
    }
}