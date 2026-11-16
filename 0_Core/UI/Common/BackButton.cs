using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BackButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;   // 禁用态不触发
        GetComponentInParent<BaseUIView>()?.GoBack();
    }
}
