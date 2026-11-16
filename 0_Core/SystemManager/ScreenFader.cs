using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 【新增】全局黑屏转场器。
/// 全屏黑色 Image + CanvasGroup，负责「画面 → 黑屏」和「黑屏 → 画面」。
/// 独立 Canvas、sortingOrder 最高，不归 UIManager 面板栈管理（避免切 UI 时遗漏）。
/// </summary>
public class ScreenFader : MonoSingleton<ScreenFader>
{
    [Tooltip("全屏黑 Image + CanvasGroup，挂在一个独立 Canvas 上，sortingOrder 设为最高。")]
    [SerializeField] private CanvasGroup _black;
    [SerializeField] private float _duration = 0.3f;

    public bool IsFading { get; private set; }

    /// <summary>画面淡出到黑屏（黑幕 alpha 0 → 1）。</summary>
    public async Task FadeToBlackAsync()
    {
        IsFading = true;
        _black.gameObject.SetActive(true);
        _black.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _black.alpha = Mathf.Lerp(0f, 1f, elapsed / _duration);
            await Task.Yield();
        }
        _black.alpha = 1f;
        IsFading = false;
    }

    /// <summary>黑屏淡出到画面（黑幕 alpha 1 → 0）。</summary>
    public async Task FadeFromBlackAsync()
    {
        IsFading = true;
        _black.alpha = 1f;
        _black.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _black.alpha = Mathf.Lerp(1f, 0f, elapsed / _duration);
            await Task.Yield();
        }
        _black.alpha = 0f;
        _black.gameObject.SetActive(false);
        IsFading = false;
    }
}