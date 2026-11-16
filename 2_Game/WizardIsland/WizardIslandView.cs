using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WizardIslandView : BaseUIView
{
    #region Objects
    [Header("大地图小屋组件引用")]
    [SerializeField] private GameObject Parent;
    [SerializeField] private GameObject house_0;
    [SerializeField] private GameObject house_1;
    [SerializeField] private GameObject house_2;
    [SerializeField] private GameObject house_3;
    [SerializeField] private GameObject house_4;

    [Header("小屋大图")]
    [SerializeField] private GameObject BigHouse;
    [SerializeField] private GameObject house_0_big;
    [SerializeField] private GameObject house_1_big;
    [SerializeField] private GameObject house_2_big;
    [SerializeField] private GameObject house_3_big;
    [SerializeField] private GameObject house_4_big;

    [Header("小屋内")]
    [SerializeField] private List<WizardHouses> wizardHouses;
    #endregion

    #region configs
    private int currentHouseChoose = -1;

    [System.Serializable]
    public class WizardHouses
    {
        public int index;
        public string UIName;
    }
    #endregion

    #region life
    protected override void OnOpen()
    {
        base.OnOpen();
        clearAllCache();
    }

    protected override bool OnBack()
    {
        if (currentHouseChoose >= 0)
        {
            currentHouseChoose = -1;
            SetCurrentHouseActive();
            return true;
        }
        return false;
    }
    protected override void OnClose()
    {
        base.OnClose();
        clearAllCache();
    }

    protected override void OnRefresh()
    {
        base.OnRefresh();
        SetCurrentHouseActive();
    }

    #endregion

    #region logs
    public void Log(string message)
    {
        Debug.Log($"[WizardIsland] : {message}");
    }

    public void LogError(string message)
    {
        Debug.LogError($"[WizardIsland] : {message}");
    }

    public void LogWarning(string message)
    {
        Debug.LogWarning($"[WizardIsland] : {message}");
    }
    #endregion

    #region tools
    private void OpenBig()
    {
        Parent.SetActive(false);
    }
    private void SetAllBigNotActive()
    {
        house_0_big.SetActive(false);
        house_1_big.SetActive(false);
        house_2_big.SetActive(false);
        house_3_big.SetActive(false);
        house_4_big.SetActive(false);
        Parent.SetActive(true);
    }
    private void SetCurrentHouseActive()
    {
        OpenBig();
        switch (currentHouseChoose)
        {
            case 0:
                house_0_big.SetActive(true);
                break;
            case 1:
                house_1_big.SetActive(true);
                break;
            case 2:
                house_2_big.SetActive(true);
                break;
            case 3:
                house_3_big.SetActive(true);
                break;
            case 4:
                house_4_big.SetActive(true);
                break;
            default:
                clearAllCache();
                break;
        }
    }



    private void clearAllCache()
    {
        currentHouseChoose = -1;
        SetAllBigNotActive();
    }
    #endregion

    #region cores
    //点击大地图小屋控制
    public void OnClickHouse(GameObject currentHouse)
    {
        //1.检查该小屋的开关是否打开
        Toggle tog = currentHouse.GetComponentInChildren<Toggle>();
        if (tog.isOn == false)
        {
            //2.如果没有打开，先打开并等待下一次点击
            tog.isOn = true;
            return;
        }

        string[] id = currentHouse.name.Split("_");
        if (id.Length > 0)
            currentHouseChoose = int.Parse(id[1]);
        Refresh();
    }
    public void OnClickHouseIn()
    {
        EventBus.Instance.Publish(new RequestDialogue { StartID = 175 });
    }
    public void OnClickBuy()
    {
        _ = UIManager.Instance.SwitchPanelAsync(UIID._shop, new ShopPanelData
        {
            Type = ShopType.Herb
        },
        fade: true
        );
    }

    public void onClickInGarden()
    {
        if(currentHouseChoose > 0)
        {
            //进入花园场景
            //1.渲染ui
            _ = UIManager.Instance.SwitchPanelAsync(UIID._hud,duringBlack:() => { //2.实例化玩家到目的地
                PlayerManager.Instance.ActivatePlayerObject(Location._garden);
            },fade :true);
        }
    }
    #endregion
}
