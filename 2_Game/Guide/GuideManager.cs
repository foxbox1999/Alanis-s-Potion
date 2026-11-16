using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 引导管理器：顺序播放引导步骤。
/// 引导期间全屏透明拦截层挡住所有 UI，点任意处推进，结束后其它 UI 恢复可点。
/// 引导步骤集中在这里配置，UI 预制件不碰剧情。
/// </summary>
public class GuideManager : MonoSingleton<GuideManager>
{
    [Header("预制件与画布")]
    [SerializeField] private GuideTip _tipPrefab;
    [SerializeField] private RectTransform _topCanvas;   // 最顶层 Canvas（拦截可靠）

    [Header("防连点")]
    [SerializeField] private float _nextCooldown = 0.3f;

    private Queue<GuideStep> _steps;
    private Action _onComplete;
    private GuideTip _current;
    private float _lastAdvanceTime = float.NegativeInfinity;

    public bool IsGuiding => _current != null || (_steps != null && _steps.Count > 0);

    /// <summary>开始一段引导；全部点完后回调 onComplete。</summary>
    public void StartGuide(List<GuideStep> steps, Action onComplete = null)
    {
        if (steps == null || steps.Count == 0) { onComplete?.Invoke(); return; }
        _steps = new Queue<GuideStep>(steps);
        _onComplete = onComplete;
        _lastAdvanceTime = float.NegativeInfinity;
        ShowNext();
    }

    /// <summary>推进下一步（拦截层点击触发）。</summary>
    public void Next()
    {
        if (Time.unscaledTime - _lastAdvanceTime < _nextCooldown) return;
        _lastAdvanceTime = Time.unscaledTime;
        ShowNext();
    }

    private void ShowNext()
    {
        if (_current != null) { Destroy(_current.gameObject); _current = null; }

        if (_steps.Count == 0)
        {
            _onComplete?.Invoke();
            _onComplete = null;
            return;
        }

        GuideStep step = _steps.Dequeue();
        if (step == null) return;
        _current = Instantiate(_tipPrefab, _topCanvas);
        _current.Setup(step, Next);
    }

    public void StopGuide()
    {
        if (_current != null) { Destroy(_current.gameObject); _current = null; }
        _steps?.Clear();
        _onComplete = null;
    }
}

/// <summary>
/// 一步引导的数据。
/// 由 GuideManager 顺序播放；每个界面引导几步、每步指向谁、说什么，都在这里配置。
/// </summary>
[Serializable]
public class GuideStep
{
    [Tooltip("要指向的目标 UI 元素（直接拖，不要用名字 Find，避免改名就崩）")]
    public string targetName;

    [Tooltip("提示文字")]
    public string tipText;

    [Tooltip("箭头方向（对应美术 5 张图：上 / 下左 / 下右 / 左 / 右）")]
    public GuideArrowDir arrow;
}