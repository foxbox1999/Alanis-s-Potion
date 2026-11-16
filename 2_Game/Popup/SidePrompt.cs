using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>侧边提示条：移动 SideImage 背景图 → 打开往左移一个宽度 → 停留 → 关闭往右移回 → 回收。</summary>
public class SidePrompt : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messages;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Image SideImage;

    [SerializeField] private float _slideInTime = 0.3f;
    [SerializeField] private float _displayTime = 4f;
    [SerializeField] private float _slideOutTime = 0.3f;

    private Coroutine _routine;
    private Vector2 _imageStartPos;   // 图片初始位置（打开前先归位）

    private void Awake()
    {
        if (SideImage != null) _imageStartPos = SideImage.rectTransform.anchoredPosition;
    }

    private float Width => SideImage != null ? SideImage.rectTransform.rect.width : 0f;

    public void Show(string message)
    {
        if (messages != null) messages.text = message;
        confirmButton?.onClick.RemoveAllListeners();
        confirmButton?.onClick.AddListener(Retract);

        if (SideImage != null) SideImage.rectTransform.anchoredPosition = _imageStartPos;   // 先归位

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Lifecycle());
    }

    private IEnumerator Lifecycle()
    {
        yield return Slide(-Width, _slideInTime);   // 打开：Image 往左移一个宽度
        yield return new WaitForSeconds(_displayTime);
        yield return Slide(Width, _slideOutTime);   // 关闭：Image 往右移回
        Recycle();
    }

    public void Retract()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(RetractRoutine());
    }

    private IEnumerator RetractRoutine()
    {
        yield return Slide(Width, _slideOutTime);
        Recycle();
    }

    /// <summary>移动的是 SideImage（子物体的 text/button 会跟着一起动）。</summary>
    private IEnumerator Slide(float deltaX, float duration)
    {
        if (SideImage == null) yield break;
        Vector2 start = SideImage.rectTransform.anchoredPosition;
        Vector2 target = start + new Vector2(deltaX, 0f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SideImage.rectTransform.anchoredPosition = Vector2.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        SideImage.rectTransform.anchoredPosition = target;
    }

    private void Recycle()
    {
        gameObject.SetActive(false);
        PoolManager.Instance.Return(gameObject);
    }
}