using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingButton : MonoBehaviour
{
    // 打开游戏内设置（弹窗式：不关页面、不黑屏、在顶层）
    public void onSettingButton()
    {
        UIManager.Instance.OpenPanel(UIID._gameSetting);   // 直接用 OpenPanel，不 SwitchPanelAsync
    }
}
