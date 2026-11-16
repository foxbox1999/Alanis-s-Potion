using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestState : IGameState
{
    /**
     * 游戏启动，加载初始界面
     *      显示Logo、检查存档、加载最低配置
     */
    public TestState(GameManager _gameManager) : base(_gameManager)
    {
    }
    public override void Enter()
    {
        base.Enter();
        switch (GameManager.Instance.testAim)
        {
            case "Town":
                //1.激活玩家并设置玩家的位置
                PlayerManager.Instance.ActivatePlayerObject(Location._triumphal);
                //PlayerManager.Instance.ActivatePlayerObject(new Vector3(105.82f,-2f,0));
                //2.打开小镇ui
                UIManager.Instance.OpenPanel(UIID._hud);
                break;
            default:
                if (SaveManager.Instance.HasSave(0))
                    SaveManager.Instance.Load(0);
                else
                    SaveManager.Instance.NewGame(0);
                InventoryManager.Instance.Add("Herb_Almond", 1);
                UIManager.Instance.OpenPanel(UIID._dessertShop);
                break;
        }
    }
    public override void Update()
    {
        base.Update();
    }
    public override void Exit()
    {
        base.Exit();
    }
}
