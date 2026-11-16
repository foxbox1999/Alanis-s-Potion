using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可复用「上下箭头」滚动控制器：放在任意 ScrollRect 层级下，自动向上查找最近的 ScrollRect 并平滑滚动。
/// 用法：挂在一个包含「上/下」两个 Button 的节点上（如 Btn_Up / Btn_Down），无需关心它在 Content 内外。
/// </summary>
public class ScrollRectArrows : MonoBehaviour
{
    [Header("箭头按钮")]
    [SerializeField] private Button _upButton;
    [SerializeField] private Button _downButton;

    [Header("目标（留空则自动向上查找最近的 ScrollRect）")]
    [SerializeField] private ScrollRect _scrollRect;

    [Header("参数")]
    [SerializeField] private float _step = 0.3f;     // 每次滚动比例（0~1，正=向上）
    [SerializeField] private float _duration = 0.2f; // 平滑时长

    private Coroutine _routine;

    private void Awake()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponentInParent<ScrollRect>();

        if (_upButton != null)
        {
            _upButton.onClick.RemoveAllListeners();
            _upButton.onClick.AddListener(() => ScrollBy(_step));
        }
        if (_downButton != null)
        {
            _downButton.onClick.RemoveAllListeners();
            _downButton.onClick.AddListener(() => ScrollBy(-_step));
        }
    }

    public void ScrollBy(float delta)
    {
        if (_scrollRect == null) return;
        float target = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + delta);
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(SmoothScroll(target));
    }

    private IEnumerator SmoothScroll(float target)
    {
        float start = _scrollRect.verticalNormalizedPosition;
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);
            float s = t * t * (3f - 2f * t);                       // SmoothStep 缓动
            _scrollRect.verticalNormalizedPosition = Mathf.LerpUnclamped(start, target, s);
            yield return null;
        }
        _scrollRect.verticalNormalizedPosition = target;
        _routine = null;
    }
}