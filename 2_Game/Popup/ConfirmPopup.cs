using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 选择弹窗：确认 + 取消。
/// </summary>
public class ConfirmPopupView : PopupViewBase
{
    [SerializeField] private TextMeshProUGUI Title;
    [SerializeField] private TextMeshProUGUI messages;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    protected override void Bind()
    {
        if (Popup == null) return;
        Title?.SetText(Popup.title);
        messages?.SetText(Popup.message);
        confirmButton?.onClick.RemoveAllListeners();
        confirmButton?.onClick.AddListener(ConfirmAndClose);
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelAndClose);
        }
    }

}
