using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SusanController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image Suan;   // 提示框（默认隐藏）
    [SerializeField] private Sprite SusanN;
    [SerializeField] private Sprite SusanS;

    private void OnEnable()
    {
        Suan.sprite = SusanN;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Suan != null)
            Suan.sprite = SusanS;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Suan != null)
            Suan.sprite = SusanN;
    }
}
