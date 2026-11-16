using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueManager : MonoSingleton<DialogueManager>
{
    /// <summary>
    /// 对话控制器（导演）
    /// 职责：加载CSV、驱动流程、处理选项、执行效果指令
    /// </summary>

    #region Config
    private IEventBus _eventBus;
    private Dictionary<string, IDisposable> _subs = new Dictionary<string, IDisposable>();

    private Dictionary<int, DialogueRow> rowMap;
    private DialogueRow currentRow;
    private List<DialogueRow> currentOptions;
    private bool isWaitingForChoice;

    private UnityAction currentAction = null;
    // 临时存储：等待 UI 就绪期间的起始 ID
    private int pendingStartId;
    private Sprite pendingBackGround;
    private List<UnityAction> pendingActions;
    private string _giftNpcName;
    private int _giftNormalNextId;
    private int _farewellNextId = -1;   // 当前选项组里「回见」选项的 nextId
    private DialogueView currentView; // 运行时由 UIManager 设置
    [Header("CharacterScriptableObjects")]
    [SerializeField] private CharacterSpriteConfig spriteConfig;
    #endregion

    #region Life
    protected override void Start()
    {
        base.Start();
        _eventBus = EventBus.Instance;
        SafeSubscribe<SaveLoadedEvent>("SaveLoaded", OnSaveLoad);
        SafeSubscribe<DialogueUIReadyEvent>("DialogueUIReady", OnDialogueUIReady);
        SafeSubscribe<RequestDialogue>("RequestDialogue", OnRequestDialogue);
        SafeSubscribe<RequestEndDialogue>("RequestEndDialogue", OnRequestEndDialogue);
        SafeSubscribe<OnAvatarCustomizationOver>("FaceOver", OnAvatarCustomizationOver);
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        foreach (var sub in _subs.Values)
        {
            sub?.Dispose();
        }
        _subs.Clear();
    }
    #endregion

    #region 事件处理
    private void OnSaveLoad(SaveLoadedEvent @e)
    {
        if (e.Success)
        {
            spriteConfig.ChangeNielian();
        }
    }
    private void OnRequestDialogue(RequestDialogue @e)
    {
        currentAction = e.OnDialogueComplete;
        RequestPlayDialogue(e.StartID, e.actions, e.BackGround, e.OnDialogueComplete, e.NeedBlackFade);
        Debug.Log($"请求开始对话，起始ID: {e.StartID}");
    }
    private void OnRequestEndDialogue(RequestEndDialogue @e)
    {
        // 跳过：从当前行开始，按顺序应用所有剩余对白行的副作用（认识/好感/货币/随机物品/切换）
        // 遇到选项行(&)就停——因为选项决定后续走向，无法确定该触发哪条
        if (rowMap != null && currentRow != null)
        {
            int id = currentRow.id;
            while (rowMap.TryGetValue(id, out var row))
            {
                if (row.flag == "&") break;   // 遇到选项就停
                if (!string.IsNullOrEmpty(row.effect) && row.effect.StartsWith("认识"))
                {
                    ApplyEffect(row.effect, row.target);   // 认识/好感/银币/金币/随机物品等都会走这里
                }
                id++;
                if(id > rowMap.Count+pendingStartId) break;   // 防止死循环
            }
        }

        GoToId(-1);
    }
    private void OnDialogueUIReady(DialogueUIReadyEvent @e)
    {
        if (e.View == null)
        {
            LogError("对话 View 为空，无法初始化");
            return;
        }
        currentView = e.View; //因为是引用类型的数据，所以这里直接赋值后，外部对currentView的修改会反映到UIManager持有的实例上
        currentView.Initialize(this, spriteConfig); // 注入依赖

        GoToId(pendingStartId);
    }

    private void OnAvatarCustomizationOver(OnAvatarCustomizationOver e)
    {
        updataNielian();
    }
    #endregion

    #region Core
    public void updataNielian()
    {
        spriteConfig.ChangeNielian();
    }
    private void RequestPlayDialogue(int startId = 0, List<UnityAction> actions = null, Sprite BackGround = null, UnityAction OnDialogueComplete = null, bool NeedBlackFade = false)
    {
        // 剧情 CSV 实际路径：Resources/Txts/StoryLine.csv
        TextAsset csvFile = Resources.Load<TextAsset>(ResourcePaths.StoryLineCsv);

        if (csvFile == null)
        {
            Debug.LogError($"对话文件不存在: {ResourcePaths.StoryLineCsv}");
            return;
        }

        if (actions != null)
            pendingActions = actions;

        List<DialogueRow> rows = CsvParser.ParseDialogueCsv(csvFile.text);
        BuildRowMap(rows);
        // 缓存起始ID，等UI就绪后开始
        pendingStartId = startId;
        pendingBackGround = BackGround;
        //请求 UIManager 显示对话UI
        _eventBus.Publish(new DialogueUIOpenRequestEvent
        {
            StartID = startId,
            NeedBlackFade = NeedBlackFade
        });
        PlayerManager.Instance.FreezePlayer();
    }
    private void GoToId(int id)
    {
        //找不到id直接结束对话
        if (!rowMap.TryGetValue(id, out currentRow))
        {
            EndDialogue();
            return;
        }
        //rowMap没有End行，所以如果当前行是End行，直接结束对话
        switch (currentRow.flag)
        {
            case "#": //对白行
                currentView.ShowDialogue(currentRow, pendingBackGround);
                isWaitingForChoice = false;
                currentOptions = null;
                break;
            case "&": //选项行
                CollectAndShowOptions(currentRow);
                break;
        }
    }
    private void EndDialogue()
    {
        // —— 清空所有对话状态，避免下次读到脏数据 ——
        rowMap = null;
        currentRow = null;
        currentOptions = null;
        isWaitingForChoice = false;

        pendingActions = null;      // ★ 关键：否则下次选项会误触发上一次的 Action
        pendingStartId = 0;
        pendingBackGround = null;

        // 送礼上下文
        _giftNpcName = null;
        _giftNormalNextId = -1;
        _farewellNextId = -1;

        // 完成回调（先触发再清）
        currentAction?.Invoke();
        currentAction = null;

        currentView = null;
        UIManager.Instance?.ClosePanel("Dialogue");   // 关闭和打开用同一个开关
        PlayerManager.Instance.ThawPlayer();
    }
    private void CollectAndShowOptions(DialogueRow firstOptionRow)
    {
        currentOptions = new List<DialogueRow>();
        int id = firstOptionRow.id;
        //这里将所有的选项存储起来
        while (rowMap.TryGetValue(id, out DialogueRow row) && row.flag == "&")
        {
            currentOptions.Add(row);
            if (row.content == "回见")          // 精确匹配（选项文案固定是这俩字）
                _farewellNextId = row.nextId;
            id++;
        }
        currentView.ShowOptions(currentOptions);
        isWaitingForChoice = true; //等待玩家输入置为false
    }

    //============================玩家输入=================================
    //这部分不应该在view层里面吗
    public void OnContinue()
    {
        if (isWaitingForChoice) return;
        bool proceed = true;
        if (currentRow.effect != null)
            proceed = ApplyEffect(currentRow.effect, currentRow.target);
        if (!proceed) return;                       // 送礼接管了流程，等背包回调
        //当于结束的两个条件相:1.rowMap.TryGetValue(nextId, out currentRow)返回空 2.已经没有对话可以展示了excel里面
        if (currentRow.nextId != -1) GoToId(currentRow.nextId);
        else EndDialogue();
    }
    public void OnOptionSelected(int optionIndex)
    {
        if (!isWaitingForChoice || currentOptions == null || optionIndex >= currentOptions.Count)
            return;
        DialogueRow selected = currentOptions[optionIndex];
        isWaitingForChoice = false;
        currentOptions = null;

        if (pendingActions != null && pendingActions.Count > 0)
        {
            Log($"选项{optionIndex}被触发");
            pendingActions[optionIndex].Invoke();
        }

        ApplyEffect(selected.effect, selected.target);
        if (selected.nextId != -1) GoToId(selected.nextId);
        else EndDialogue();
    }


    #endregion

    #region Tool
    private void SafeSubscribe<T>(string key, Action<T> action) where T : EventBase
    {
        if (_eventBus == null)
        {
            Log("事件总线初始化未成功，订阅失败");
            return;
        }
        if (_subs == null)
            _subs = new Dictionary<string, IDisposable>();

        if (_subs.TryGetValue(key, out var oldSub))
            oldSub?.Dispose();

        _subs[key] = _eventBus.Subscribe(action);
    }

    //将行与ID建立映射，方便快速跳转
    private void BuildRowMap(List<DialogueRow> rows)
    {
        rowMap = new Dictionary<int, DialogueRow>();
        foreach (var row in rows)
        {
            if (row.flag != "END")
                rowMap[row.id] = row;
        }
    }

    private bool ApplyEffect(string effect, string target)
    {
        if (string.IsNullOrEmpty(effect)) return true;
        //================切换事件=================
        if (effect.StartsWith("切换"))
        {
            bool toLive2D = effect.Contains("Live2D");
            var newType = toLive2D ? CharacterDisplayType.Live2D : CharacterDisplayType.Sprite;
            string targetChar = string.IsNullOrEmpty(target) ? currentRow.characterId : target;
            var charData = spriteConfig.GetCharacter(targetChar);
            if (charData != null)
            {
                charData.currentDisplayType = newType;
                currentView.RefreshCharacterDisplay(targetChar, currentRow.expression);
            }
            return true;
        }
        //================送礼事件=================
        if (effect.StartsWith("送礼"))
        {
            _giftNpcName = target;                 // NPC 显示名
            _giftNormalNextId = currentRow.nextId; // 普通回复 ID
            UIManager.Instance.OpenPanel(UIID._backpack, new InventoryPanelData
            {
                Context = InventoryContext.Select,
                OnItemSelected = OnGiftSelected,
                OnCanceled = OnGiftCanceled
            }
            );
            return false;   // 暂停，不继续跳转
        }
        // 3. 随机物品
        if (effect.StartsWith("随机物品")) { GiveRandomItem(); return true; }

        // 4. 规范：效果列 = "类型@值"，目标列 = 角色
        int at = effect.IndexOf('@');
        string type = at >= 0 ? effect.Substring(0, at).Trim() : effect.Trim();
        string value = at >= 0 ? effect.Substring(at + 1).Trim() : "";   // ★ 值只从 @ 后面取，不再回退到 target

        switch (type)
        {
            case "好感度": ApplyFavor(value, target); return true;   // ★ target 是角色名
            case "银币": ApplySilver(value); return true;
            case "金币": ApplyGold(value); return true;
            case "打开商店": OpenShop(value); return true;   // ★ 值 = 甜品/药材
            case "认识": MeetNpc(value); return true;   // ★ 值 = NPC id
            default: LogWarning($"未知效果类型: {type}"); return true;
        }
    }

    // —— 送礼结果回调（不变） ——
    private void OnGiftSelected(Item item)
    {
        if (item == null) { OnGiftCanceled(); return; }

        InventoryManager.Instance.Remove(item.id, 1);   // 扣除礼物

        var npc = DatabaseManager.Instance.GetNpcByName(_giftNpcName);
        bool liked = npc != null && !string.IsNullOrEmpty(npc.preference) && npc.preference.Contains(item.name);

        int targetId = liked ? _giftNormalNextId + 1 : _giftNormalNextId;   // 喜欢=+1，普通=原 nextId
        GoToId(targetId);
    }

    private void OnGiftCanceled()
    {
        if (_farewellNextId >= 0) GoToId(_farewellNextId);
        else EndDialogue();
    }

    // —— 好感度 / 货币 ——
    // ★ 改成：值从 @ 取，目标用 target（角色名），空则默认当前说话角色
    private void ApplyFavor(string value, string targetName)
    {
        if (!int.TryParse(value, out int amount)) return;   // 能解析 "+5" / "-10"
        string name = string.IsNullOrEmpty(targetName) ? currentRow.characterId : targetName;
        var npc = DatabaseManager.Instance.GetNpcByName(name);   // 名字 → NPC
        if (npc != null) SocialManager.Instance.AddFavor(npc.id, amount);
    }

    private void ApplyGold(string value)
    {
        if (int.TryParse(value, out int v))
        {
            PlayerDataManager.Instance.AddGold(v);
            PopupManager.Instance.ShowSidePrompt(v >= 0 ? $"获得 {v} 金币" : $"失去 {-v} 金币");
        }
        //用SidePrompt提示
    }
    private void ApplySilver(string value)
    {
        if (int.TryParse(value, out int v))
        {
            PlayerDataManager.Instance.AddSilver(v);
            PopupManager.Instance.ShowSidePrompt(v >= 0 ? $"获得 {v} 银币" : $"失去 {-v} 银币");
        }
    }

    // —— 打开商店 / 认识（占位，商店/NPC 系统做好后接） ——
    private void OpenShop(string shopType)
    {
        // shopType = "甜品" 或 "药材"，打开对应商店 U
        switch (shopType)
        {
            case "甜品":
                 _ = UIManager.Instance.SwitchPanelAsync(UIID._shop,
            new ShopPanelData { Type = ShopType.Dessert }, fade: true);
                break;
            case "药店":
                _ = UIManager.Instance.SwitchPanelAsync(UIID._shop,
            new ShopPanelData { Type = ShopType.Herb }, fade: true);
                break;
        }
    }

    private void MeetNpc(string npcId)
    {
        var npc = DatabaseManager.Instance.GetNpc(npcId);
        if (npc == null) return;

        if (SocialManager.Instance.Meet(npcId))
        {
            // 新认识 → 侧边弹窗提示获得新情报
            PopupManager.Instance.ShowSidePrompt($"获得新情报：{npc.name}");
        }
        // 已认识 → 无作为
    }

    private void GiveRandomItem()
    {
        //随机获取一个物品添加到玩家仓库并弹窗
        var items = DatabaseManager.Instance.AllItems as List<Item>;
        if (items == null || items.Count == 0) return;
        var item = items[UnityEngine.Random.Range(0, items.Count)];
        InventoryManager.Instance.Add(item.id, 1);
        PopupManager.Instance.ShowItem(new List<ItemStackData> { new() { itemId = item.id, count = 1 } });
    }

    #endregion

    #region Logs
    public void Log(string info)
    {
        Debug.Log($"[DialogueManager]: {info}");
    }
    public void LogWarning(string info)
    {
        Debug.LogWarning($"[DialogueManager]: {info}");
    }
    public void LogError(string info)
    {
        Debug.LogError($"[DialogueManager]: {info}");
    }
    #endregion

}
