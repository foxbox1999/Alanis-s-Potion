using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 纯提示框视图层
/// </summary>
public class InfoPopup : PopupViewBase
{
    [SerializeField] private TextMeshProUGUI messages;
    [SerializeField] private Button confirmButton;

    protected override void Bind()
    {
        if (Popup == null) return;

        messages?.SetText(Popup.message);
        confirmButton?.onClick.RemoveAllListeners();
        confirmButton?.onClick.AddListener(ConfirmAndClose);
    }
}
