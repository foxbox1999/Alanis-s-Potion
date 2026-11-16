using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>主菜单按钮：点击清空面板栈并打开主菜单（StartState）。</summary>
public class MainMenuButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var btn = GetComponent<Button>();
        if (btn != null && !btn.interactable) return;
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._mainmenu,
            openData: new UIPanelData { ClearStack = true },
            closeKey: null,
            duringBlack: null,
            fade: true
        );
    }
}