using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>按钮点击音效：挂在 Button 上，默认播「通用提示」，可对单个按钮指定不同音效。</summary>
public class ButtonSFX : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string _sfxKey = AudioPaths.SFX_Click;

    public void OnPointerClick(PointerEventData eventData)
    {
        var btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;
        AudioManager.Instance.PlaySFX(_sfxKey);
    }
    
}