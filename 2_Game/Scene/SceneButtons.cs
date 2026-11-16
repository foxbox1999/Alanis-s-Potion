using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneButtons : MonoBehaviour
{
    public void onMainHome()
    {
        _ = UIManager.Instance.SwitchPanelAsync(UIID._clinic, closeKey: UIID._hud, fade: true);
    }
    public void onDessert()
    {
        _ = UIManager.Instance.SwitchPanelAsync(UIID._dessertShop, closeKey: UIID._hud, fade: true);
    }

    public void onClickInGarden()
    {
        //1.黑屏渲染ui
        _ = UIManager.Instance.SwitchPanelAsync(UIID._wizardIsland,duringBlack: () =>
        {
            //2.卸载玩家对象
            PlayerManager.Instance.UninstallPlayerObject();
        },closeKey: UIID._hud, fade: true);
    }
    public void onClickInStreetRight()
    {
        //1.渲染ui
        _ = UIManager.Instance.SwitchPanelAsync(UIID._hud, duringBlack: () =>
        { //2.实例化玩家到目的地
            PlayerManager.Instance.ActivatePlayerObject(Location._church);
        }, fade: true);
    }
    public void onClickChurch()
    {
        //1.渲染ui
        _ = UIManager.Instance.SwitchPanelAsync(UIID._hud, duringBlack: () =>
        { //2.实例化玩家到目的地
            PlayerManager.Instance.ActivatePlayerObject(Location._streetRight);
        }, fade: true);
    }
}
