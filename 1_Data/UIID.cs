
/// <summary>
/// 表示用于存储和管理与用户界面相关的键值对的通用字典。
/// </summary>
/// <remarks>类本身不包含成员，具体行为由实现决定；可用于在组件间共享界面配置或本地化资源。 </remarks>
public static class UIID
{
    #region 主UI

    /// <summary>开始界面</summary>
    public readonly static string _start = "Start";
    /// <summary>捏脸界面</summary>
    public readonly static string _characterCreation = "CharacterCreation";
    /// <summary>小镇抬头显示</summary>
    public readonly static string _hud = "HUD";
    /// <summary>地图ui</summary>
    public readonly static string _map = "Map";
    /// <summary>巫师岛</summary>
    public readonly static string _wizardIsland = "WizardIsland";
    /// <summary>背包ui</summary>
    public readonly static string _backpack = "Backpack";
    /// <summary>主菜单</summary>
    public readonly static string _mainmenu = "MainMenu";
    /// <summary>养子</summary>
    public readonly static string _raising = "Raising";
    /// <summary>看病</summary>
    public readonly static string _clinic = "Diagnose";
    /// <summary>采集</summary>
    public readonly static string _gather = "Gather";
    /// <summary>炼药</summary>
    public readonly static string _alchemy = "Alchemy";
    /// <summary>商店</summary>
    public readonly static string _shop = "Shop";
    /// <summary>甜品店</summary>
    public readonly static string _dessertShop = "DessertShop";
    /// <summary>情报</summary>
    public readonly static string _intelligence = "Intelligence";

    #endregion
    /// <summary>对话框界面</summary>
    public readonly static string _dialogue = "Dialogue";

    #region 弹窗
    /// <summary>选择弹窗</summary>
    public readonly static string _confirmPopup = "ConfirmPopup";
    /// <summary>通知弹窗</summary>
    public readonly static string _infoPopup = "InfoPopup";
    /// <summary>侧边提示</summary>
    public readonly static string _sidePrompt = "SidePrompt";
    /// <summary>获得物品弹窗</summary>
    public readonly static string _itemPopup = "ItemPopup";
    #endregion

    #region Setting
    /// <summary>初始设置</summary>
    public readonly static string _setting = "Setting";
    /// <summary>游戏内设置</summary>
    public readonly static string _gameSetting = "GameSetting";
    #endregion
}
