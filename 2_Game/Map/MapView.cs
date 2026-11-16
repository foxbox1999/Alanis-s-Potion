using UnityEngine;

/// <summary>地图界面 View：商业街/教堂可传送，其余未开放。</summary>
public class MapView : BaseUIView
{
    // 商业街（开放）→ 小屋
    public void onBusinessStreet()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._hud,
            openData: new UIPanelData { ClearStack = true },
            closeKey: UIID._map,
            duringBlack: () =>
            {
                PlayerManager.Instance.ActivatePlayerObject(Location._home);
                (GameManager.Instance.GameLoop as GameLoopState)?.EnterFreeRoam();
            },
            fade: true
        );
    }

    // 教堂（开放）→ 教堂
    public void onChurch()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._hud,
            openData: new UIPanelData { ClearStack = true },
            closeKey: UIID._map,
            duringBlack: () =>
            {
                PlayerManager.Instance.ActivatePlayerObject(Location._church);
                (GameManager.Instance.GameLoop as GameLoopState)?.EnterFreeRoam();
            },
            fade: true
        );
    }

    // 其余：未开放
    public void onHousing() => PopupManager.Instance.ShowSidePrompt("该区域暂未开放");
    public void onTownOut() => PopupManager.Instance.ShowSidePrompt("该区域暂未开放");
    public void onNormalHouse() => PopupManager.Instance.ShowSidePrompt("该区域暂未开放");
    public void onGarden(){
        _ = UIManager.Instance.SwitchPanelAsync(
            openKey: UIID._hud,
            openData: new UIPanelData { ClearStack = true },
            closeKey: UIID._map,
            duringBlack: () =>
            {
                PlayerManager.Instance.ActivatePlayerObject(Location._garden);
                (GameManager.Instance.GameLoop as GameLoopState)?.EnterFreeRoam();
            },
            fade: true
        );
    }
    public void onNobility() => PopupManager.Instance.ShowSidePrompt("该区域暂未开放");
    public void onGarrison() => PopupManager.Instance.ShowSidePrompt("该区域暂未开放");
}