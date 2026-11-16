using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>药水格子：显示图标 + 数量，点击选中。</summary>
public class ItemCell : MonoBehaviour
{
    [Serializable]
    public class VisualStyle
    {
        public Color color = Color.white;   // 状态颜色
        public float scale = 1f;            // 缩放倍数（选中放大用）
    }
    [SerializeField] private Image _icon;
    [SerializeField] protected TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _countText;
    [Header("视觉状态（Inspector 里直接改颜色/缩放）")]
    [SerializeField] private VisualStyle _defaultStyle = new VisualStyle { color = Color.white, scale = 1f };
    [SerializeField] private VisualStyle _matchedStyle = new VisualStyle { color = Color.green, scale = 1f };
    [SerializeField] private VisualStyle _selectedStyle = new VisualStyle { color = Color.blue, scale = 1.1f };
    private Button _button;
    public string ItemId { get; private set; }

    private void Awake() => _button = GetComponent<Button>();

    public void Setup(string itemId, Sprite icon, string count, Action<string> onClick, string name = null)
    {
        ItemId = itemId;
        if (_icon != null) _icon.sprite = icon;
        if (_nameText) _nameText.text = name;
        SetCount(count);

        if (_button != null)   // ★ 采集格子没有 Button，跳过
        {
            _button.onClick.RemoveAllListeners();
            if (onClick != null)
                _button.onClick.AddListener(() => onClick?.Invoke(ItemId));
        }

    }

    public void SetCount(string count) { if (_countText != null) _countText.text = count; }

    /// <summary>三种视觉状态：默认 / 匹配(绿) / 选中(蓝+放大)。</summary>
    public void SetVisual(ItemVisual visual)
    {
        VisualStyle style = visual switch
        {
            ItemVisual.Selected => _selectedStyle,
            ItemVisual.Matched => _matchedStyle,
            _ => _defaultStyle
        };
        Image back =  GetComponent<Image>();
        if (back) back.color = style.color;
        transform.localScale = Vector3.one * style.scale;
    }

    /// <summary>高亮当前选中的药。</summary>
    public void SetSelected(bool selected)
    {
        if (_icon != null) _icon.color = selected ? Color.blue : Color.white;
    }
}