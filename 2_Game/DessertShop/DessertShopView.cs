using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DessertShopView : BaseUIView
{
    public void OnSusan()
    {
        EventBus.Instance.Publish(new RequestDialogue { StartID = 168, NeedBlackFade = false });
    }

    public void OnBuy()
    {
        _ = UIManager.Instance.SwitchPanelAsync(
            UIID._shop,
            new ShopPanelData { Type = ShopType.Dessert },
            fade: true
        );
    }
}
