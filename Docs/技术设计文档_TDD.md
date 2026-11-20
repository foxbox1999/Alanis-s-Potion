# 《艾拉妮丝的魔药》技术设计文档（TDD）

> 版本：v1.0
> 文档性质：技术设计文档（Technical Design Document）
> 适用项目：Unity 2D 模拟经营 + 叙事 RPG《艾拉妮丝的魔药》（Elenora's Brew）
> 读者对象：客户端开发、TA、技术负责人、后续接手维护者

---

## 目录

1. 文档说明与阅读指引
2. 项目概述
3. 总体架构
4. 框架层（0_Core）
5. 数据层（1_Data）
6. 业务层（2_Game）
7. 关键技术方案
8. 核心流程与时序
9. 性能与内存
10. 工程规范
11. 已知问题与技术债
12. 附录

---

## 1. 文档说明与阅读指引

本文档描述《艾拉妮丝的魔药》客户端的技术实现：包括架构分层、各管理器职责、核心系统设计、关键流程、工程规范与技术债。

阅读建议：

- 想快速理解项目：先看第 2 章（项目概述）和第 3 章（总体架构）。
- 要接手某个系统：直接看第 6 章对应小节，再回到第 4 章查它依赖的框架能力。
- 要做性能优化：看第 9 章。
- 要修 bug / 加功能：先看第 11 章（已知问题），避免踩坑。

术语约定：

- Manager：承担业务逻辑与数据的管理器（如 ClinicManager）。
- View：只负责展示与输入的面板（如 ClinicView）。
- DTO：数据传输对象，用于存档序列化（如 PlayerSaveData）。
- 事件：通过 EventBus 传递的跨模块消息（如 NewDayEvent）。

---

## 2. 项目概述

### 2.1 项目定位

《艾拉妮丝的魔药》是一款 2D 模拟经营 + 叙事 RPG 的单机游戏。玩家扮演一位隐藏身份的游医，在小镇经营药铺，核心循环为：采集 → 炼药 → 看病 → 赚钱 → 养成 → 社交 → 推进剧情。

### 2.2 技术栈

| 项目 | 选型 |
|---|---|
| 引擎 | Unity |
| 语言 | C# |
| UI | UGUI（Canvas / RectTransform / CanvasScaler） |
| 输入 | Unity 新 Input System |
| 数据驱动 | ScriptableObject 配置数据库 + CSV 剧情 |
| 序列化 | JsonUtility（存档 DTO） |
| 架构模式 | 单例、观察者（事件总线）、状态机、工厂、对象池 |
| 目标平台 | PC（Windows）优先，2D 单机 |

### 2.3 运行环境

- Unity 版本：建议 2022.3 LTS 及以上。
- 渲染管线：Built-in（2D）。
- 目标分辨率：设计基准 1920×1080，通过 CanvasScaler 适配更高/更低分辨率。

<!-- 配图说明：这里插入「项目技术栈与运行环境总览图」，展示 Unity 编辑器、目标平台、分辨率适配关系 -->

---

## 3. 总体架构

### 3.1 分层架构

项目按职责分为三层，目录前缀即为层标识：

- `0_Core/`：框架层。与具体玩法无关的通用能力：单例、事件总线、存档、对象池、UI 框架、状态机、资源加载、音频、时间、输入。
- `1_Data/`：数据层。纯数据定义：数据模型、ScriptableObject 数据库、存档 DTO、枚举与常量。
- `2_Game/`：业务层。具体玩法系统：剧情、对话、炼药、看病、采集、养成、背包、社交、商店、捏脸、NPC、引导。

依赖方向：业务层 → 数据层 → 框架层。框架层不依赖业务层，数据层不依赖业务层。

<!-- 配图说明：这里插入「三层架构依赖方向图」，用箭头标出 2_Game → 1_Data → 0_Core 的单向依赖 -->

### 3.2 架构设计原则

1. **Manager / View 分离**：业务逻辑放在 Manager（单例、无 UI 依赖），界面展示放在 View（BaseUIView 子类），View 只调用 Manager 的公开方法并订阅事件刷新显示。
2. **事件驱动解耦**：跨模块的「动作」通过 EventBus 发布事件，订阅者各自响应，避免管理器之间硬编码互调。
3. **数据驱动**：静态配置放 ScriptableObject，剧情与对话放 CSV，改数值/剧情不改代码。
4. **可扩展存档**：每个需要持久化的系统实现 ISaveable 接口并自注册，SaveManager 不感知具体业务。
5. **对象复用**：UI 与 NPC 走对象池，减少 Instantiate/Destroy 带来的 GC 与卡顿。

### 3.3 核心管理器一览

| 管理器 | 所属层 | 职责 |
|---|---|---|
| GameManager | Core | 总协调、全局状态机、忙碌计数 |
| TimeManager | Core | 游戏时间（年月日时分秒、倍速、暂停、存档） |
| DayCycleManager | Core | 昼夜切换、23:50 强制休息 |
| WorldManager | Core | 世界进度、交租、区域解锁 |
| SaveManager | Core | 存档协调（模块注册、原子写、SHA256 校验） |
| UIManager | Core | 面板栈、层级、转场、弹窗 |
| AssetManager | Core | 资源加载门面（ResourcesLoader） |
| PoolManager | Core | 对象池 |
| AudioManager | Core | BGM 交叉淡入淡出、SFX |
| InputManager | Core | 新 Input System 封装、交互事件 |
| PlayerManager | Core | 玩家实体、相机、冻结/解冻、传送 |
| PopupManager | Core | 弹窗门面 |
| DatabaseManager | Data | 静态数据库聚合查询 |
| PlotDirector | Game | 剧情流程控制 |
| DialogueManager | Game | 对话解析、分支、效果执行 |
| AlchemyManager | Game | 炼药任务、后台计时、结算 |
| ClinicManager | Game | 病人、诊断、治疗、结算 |
| GatherManager | Game | 采集结算 |
| ChildManager | Game | 养子属性、每日任务 |
| InventoryManager | Game | 背包物品增删查 |
| SocialManager | Game | 好感度、怀疑度、认识 |
| PlayerDataManager | Game | 玩家档案、捏脸、剧情进度、会话状态 |

<!-- 配图说明：这里插入「核心管理器关系图」，用中心辐射或分组方式展示各管理器归属层与依赖关系 -->

---

## 4. 框架层（0_Core）

### 4.1 单例基类 MonoSingleton<T>

所有全局管理器继承 `MonoSingleton<T>`，提供 `Instance` 静态访问。Awake 中做重复实例检测（场景里误放两个时销毁后者），OnDestroy 清空静态引用，避免访问已销毁对象。

```csharp
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;
            if (_instance == null) _instance = FindObjectOfType<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            DestroyImmediate(this.gameObject);
            return;
        }
        _instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
```

设计说明：

- 懒加载（FindObjectOfType）用于兜底，正常情况下 Instance 在 Awake 即被赋值。
- 项目为单场景结构（剧情中的『场景加载』实际是黑屏+传送+切 UI 的模拟），因此未强制 DontDestroyOnLoad；若后续拆真场景，应在 Awake 中补 `DontDestroyOnLoad(gameObject)`。

### 4.2 事件总线 EventBus

EventBus 是整个项目的解耦核心，采用观察者模式，支持：

- 泛型订阅/发布：`Subscribe<T>(Action<T>, priority)` / `Publish<T>(T)`。
- 优先级：数字越小越先执行，便于控制『结算 → 刷新』的顺序。
- 返回 IDisposable 订阅令牌，便于在 OnDestroy 释放。

```csharp
// 订阅（prio 越小越先执行）
var sub = EventBus.Instance.Subscribe<NewDayEvent>(e => SettleDailyTasks(), -10);

// 发布
EventBus.Instance.Publish(new NewDayEvent { DayIndex = d });

// 释放
sub?.Dispose();
```

设计要点：

- 事件类统一继承 `EventBase`（自动生成 EventId 与时间戳）。
- 业务事件集中在 `1_Data/GameEvents.cs` 与 `0_Core/EventBus/Events.cs`。
- 仅用于「通知/动作」，查询数据仍走 Manager 的直接方法（避免事件风暴）。

### 4.3 存档系统（ISaveable + SaveManager）

#### 4.3.1 接口设计

```csharp
public interface ISaveable
{
    string SaveKey { get; }      // 全局唯一、稳定的模块键
    int SaveVersion { get; }     // 模块存档版本
    string SerializeState();     // 序列化为 JSON
    void DeserializeState(string json, int version);
    void ResetState();           // 新游戏/缺省时恢复默认
}
```

各系统在 Start 中自注册：

```csharp
protected override void Start()
{
    base.Start();
    SaveManager.Instance?.RegisterModule(this);
}
```

#### 4.3.2 存档文件结构

存档根对象为 `SaveData`，包含 `header`（档头，供读档界面快速展示）与 `modules`（各模块的 JSON 片段）。

```csharp
public class SaveData
{
    public int formatVersion;
    public SaveHeader header;
    public List<SaveModuleEntry> modules;
}

public class SaveModuleEntry
{
    public string key;      // 模块 SaveKey
    public int version;     // 模块 SaveVersion
    public string payload;  // 模块 DTO 的 JSON
}
```

#### 4.3.3 安全与健壮性

1. **原子写**：先写 `.tmp` 文件，再用 `File.Replace` 替换目标，避免写一半崩溃导致存档损坏。
2. **SHA256 校验**：每个存档旁存一份 `.sha256`，读档时校验，不匹配则隔离到 `.corrupt`。
3. **版本前向兼容**：模块存档版本高于代码版本时跳过并保留原数据。
4. **生命周期钩子**：`ISaveLifecycleListener.OnBeforeSave/OnAfterLoad` 用于写档前刷新、读档后重建。

### 4.4 对象池 PoolManager

按预制件分池，`Get` 优先复用、无则实例化；`Return` 时设置失活并挂回池根。非本池产出的实例直接销毁，避免泄漏。

```csharp
public GameObject Get(GameObject prefab, Transform parent = null);
public void Return(GameObject instance);
public void Warm(GameObject prefab, int count);   // 预热
public void ClearPool(GameObject prefab);
```

池化对象带 `PooledObject` 标记（记录来源 prefab），Return 时据此归池。

### 4.5 UI 系统 UIManager

#### 4.5.1 面板基类 BaseUIView

```csharp
public abstract class BaseUIView : MonoBehaviour
{
    protected string _panelKey;
    protected UILayer _layer;
    protected bool _isSingleton;
    protected bool _isPopup;
    public bool UseBlackFade => _useBlackFade && !IsPopup;

    public virtual void Open(UIPanelData data = null)   // OnOpen -> SetActive(true)
    public virtual void Close()                        // OnClose -> SetActive(false)
    public virtual void Refresh();
    protected virtual bool OnBack() => false;          // 内部返回优先
}
```

#### 4.5.2 面板栈与转场

- `_activePanels`：key → View 的当前打开表。
- `_panelStack`：导航历史栈，根界面（Start/MainMenu/HUD）清栈放栈底。
- `SwitchPanelAsync(openKey, openData, closeKey, duringBlack, fade)`：统一转场。黑屏 → 关旧 → 执行 duringBlack（数据/传送）→ 开新 → 更新栈 → 亮屏。
- `OpenPanelAsync<T>`：纯开（无转场），供弹窗/栈外场景使用。
- `ClosePanel`：同步关闭，不碰栈。
- `GoBack`：弹栈 → 关当前 → 重开上一个。

```csharp
public async Task SwitchPanelAsync(
    string openKey, UIPanelData openData = null, string closeKey = null,
    Action duringBlack = null, bool? fade = null)
{
    bool doFade = fade ?? (GetFadePref(openKey) || GetFadePref(closeKey));
    EnterTransition();
    try
    {
        if (doFade) await ScreenFader.Instance.FadeToBlackAsync();
        if (!string.IsNullOrEmpty(closeKey)) ClosePanel(closeKey);
        duringBlack?.Invoke();
        if (!string.IsNullOrEmpty(openKey)) await OpenPanelAsync<BaseUIView>(openKey, openData);
        UpdateStack(openKey);
    }
    finally
    {
        if (doFade) await ScreenFader.Instance.FadeFromBlackAsync();
        ExitTransition();
    }
}
```

#### 4.5.3 弹窗

`PopupViewBase` 继承 BaseUIView，打开时读 `PopupData` 并绑定按钮；确认/取消回调后关闭。弹窗不入栈、不黑屏。

```csharp
protected void ConfirmAndClose()
{
    Popup?.onConfirm?.Invoke();
    UIManager.Instance.ClosePanel(PanelKey);
}
```

### 4.6 状态机（GameManager + IGameState）

游戏状态机管理启动到游玩的主流程：`BootState → StartState → GameLoopState`（另有 Loading/Paused/Test 占位）。

```csharp
public interface IGameState
{
    void Enter();
    void Update();
    void Exit();
}
```

GameLoopState 内部用子状态（SubState：FreeRoam / FullUI / Dialog）区分『可移动 / 全屏 UI / 对话』。

### 4.7 资源加载 AssetManager

门面模式，当前实现为 `ResourcesLoader`（Resources.Load 同步加载 + 缓存），对外暴露 `LoadAssetAsync<T>/LoadAllAssetsAsync<T>/LoadSceneAsync/ReleaseAsset`。设计上预留了 Addressables 的 `IAssetLoader` 接口。

### 4.8 音频 AudioManager

- 双 AudioSource 交叉淡入淡出切换 BGM。
- SFX 单源叠加，音量走 AudioMixer。
- 预加载列表减少首次播放延迟。
- 订阅 SettingsChangedEvent 应用音量配置。

```csharp
public void PlayBGM(AudioClip clip, float fade = 0.5f);
public void PlayBGMByTime(string dayKey, string nightKey);  // 按昼夜选曲
public void PlaySFX(string key);
```

### 4.9 时间 TimeManager / DayCycleManager

- `CurrentTotalSeconds`：游戏内总秒数，按倍速（默认 12 倍）流逝。
- `AddTime(seconds)`：推进时间并发布跨天事件。
- `Pause()/Resume()`：暂停/恢复游戏时间（与 Time.timeScale 无关）。
- `DayCycleManager` 根据小时判断 Day/Dusk/Night 并发布 `TimeOfDayChangedEvent`；23:50 触发强制休息。

### 4.10 输入 InputManager

封装新 Input System 的 `PlayerControls`：`MoveVector`、`OnInteractPerformed` 事件、UI/Player ActionMap 切换。

```csharp
_controls.Player.Interact.performed += ctx => OnInteractPerformed?.Invoke();
```

### 4.11 世界与场景 WorldManager

维护世界进度（交租日、区域解锁、世界旗标），实现 ISaveable。

<!-- 配图说明：这里插入「0_Core 框架层模块图」，展示单例/事件/存档/UI/池/音频/时间等模块及相互依赖 -->

---

## 5. 数据层（1_Data）

### 5.1 数据模型

| 模型 | 字段要点 |
|---|---|
| Item（基类） | id、name、icon、price、category、description |
| Herb : Item | effect、zoneIds、minQuantity、maxQuantity |
| Potion : Item | diseaseId、recipe（timeRequired + ingredients） |
| Dessert : Item | 甜品（占位） |
| Disease | id、name、description、symptoms |
| Symptom | id、name、sprite、description、displayType |
| Npc | id、name、sprite、category（阵营）、preference、personality |
| Zone | id、name、icon、description |
| Mission | id、name、type、stat、statGain、zoneId、icon |
| Patient | npcId、portraitId、diseaseId、status、remainingDays、isNamed |

```csharp
[Serializable]
public class Potion : Item
{
    public string diseaseId;
    public Recipe recipe;

    [Serializable]
    public class Recipe
    {
        public float timeRequired;
        public List<Ingredient> ingredients;
    }

    [Serializable]
    public class Ingredient
    {
        public string itemId;
        public int quantity;
    }
}
```

### 5.2 ScriptableObject 数据库

每种静态数据一个 Database 资产（HerbDatabase/PotionDatabase/…），DatabaseManager 在 Awake 中把它们聚合成字典，提供 `GetItem/GetPotion/GetDisease/GetNpc/GetZone/GetHerbsByZone` 等查询。

```csharp
protected override void Awake()
{
    base.Awake();
    foreach (var h in herbDatabase.herbs) _items[h.id] = h;
    foreach (var p in potionDatabase.potions) { _items[p.id] = p; _potions[p.id] = p; }
    // …
}
```

设计说明：数据库只做「查询」，不放业务逻辑；业务逻辑在 2_Game 的 Manager 里。

### 5.3 存档 DTO

每个模块的 DTO 都是纯 `[Serializable]` 类，只含 JsonUtility 可序列化字段（禁 long/Dictionary/UnityEngine.Object）。例如：

```csharp
[Serializable]
public class PlayerSaveData
{
    public int plotStep;
    public int locationId;
    public int sessionState;
    public int goldCoins;
    public int silverCoins;
    public string backHair, eyes, eyebrows, frontHair, mouth;
}
```

### 5.4 枚举与常量

- `UIID`：面板 key 常量（Start/MainMenu/HUD/Gather/Alchemy/Clinic/...）。
- `LocationId`：场景点位枚举（Garden/StreetRight/StreetLeft/Church/Triumphal/Home）。
- `ItemCategory` / `FactionType` / `PatientStatus` / `ChildTaskType` / `ShopType` 等。

---

## 6. 业务层（2_Game）

### 6.1 剧情系统 PlotDirector

以 PlotStep 为状态，逐步推进开局剧情：

| PlotStep | 内容 |
|---|---|
| 0 | 第一段剧情 |
| 1 | 捏脸 |
| 2 | 第二段剧情 |
| 3 | 进镇（黑屏） |
| 4 | 第三段剧情 |
| 5 | 回药店（黑屏→HUD→对话） |
| 6 | HUD 教程 |
| 7 | 自言自语（对话） |
| 8 | 自由活动 + 开局赠礼 |
| 9 | 采集教程 |
| 10 | 炼药教程 |
| 11+ | 正常游玩 / 第五天结局 |

```csharp
public void Advance()
{
    if (!_flowRunning) return;
    switch (PlayerDataManager.Instance.PlotStep)
    {
        case 0: PlayDialogue(DIALOGUE_INTRO); break;
        case 1: OpenFaceCustomizer(); break;
        // …
        case 10: PlayTutorial(TutorialKind.Alchemy); break;
    }
}
```

### 6.2 对话系统

#### 6.2.1 CSV 结构

`Resources/Txts/StoryLine.csv`，列为：标志、ID、人物、表情、内容、跳转、效果、目标。标志：`#` 对白、`&` 选项、`END` 段落结束。

```
#,0,艾拉妮丝,nielian,……,1,,&#10;
&,5,,,选项A,10,认识@Viggo,Viggo
```

#### 6.2.2 解析与执行

`CsvParser` 解析成 `DialogueRow` 列表，`DialogueManager` 维护 `rowMap`（id→row），`GoToId` 按跳转推进；遇到选项行显示选项；效果指令（`认识@id`、`好感度@+5`、`银币@-10`、`打开商店@甜品`）在 `ApplyEffect` 中执行。

```csharp
private void ApplyEffect(string effect, string target)
{
    if (effect.StartsWith("认识")) MeetNpc(target);
    else if (effect.StartsWith("好感度")) SocialManager.Instance.AddFavor(target, ParseDelta(effect));
    else if (effect.StartsWith("银币")) PlayerDataManager.Instance.AddSilver(ParseDelta(effect));
    // …
}
```

#### 6.2.3 跳过逻辑

玩家点『跳过』时，从当前行起按顺序遍历 `rowMap`，应用所有对白行的副作用（认识/好感/货币），遇到选项行则停止（选项决定后续分支），然后结束对话。

```csharp
private void OnRequestEndDialogue(RequestEndDialogue e)
{
    if (rowMap != null && currentRow != null)
    {
        int id = currentRow.id;
        while (rowMap.TryGetValue(id, out var row))
        {
            if (row.flag == "&") break;
            if (!string.IsNullOrEmpty(row.effect)) ApplyEffect(row.effect, row.target);
            id++;
        }
    }
    GoToId(-1);
}
```

### 6.3 炼药系统 AlchemyManager / AlchemyView

流程：选药方 → 看配方 → 开始炼制（扣材料、创建任务）→ 后台计时 → 完成 → 弹窗领取。

```csharp
public class AlchemyJob
{
    public string jobId;
    public string resultItemId;
    public float remaining;   // 剩余真实秒数
    public int resultCount = 1;
    public bool IsDone => remaining <= 0f;
}
```

关键设计：计时用真实时间（`Time.deltaTime` 递减 `remaining`），与游戏时间的暂停解耦，保证『退出界面再回来』倒计时仍正常走。

```csharp
private void Update()
{
    if (_currentJob == null || _currentJob.IsDone) return;
    _currentJob.remaining -= Time.deltaTime;
}
```

视图侧书本平移动画：用 `anchoredPosition` 在「原位」与「右移 520」之间 Lerp，并区分『首次炼制播动画』与『重进直接到位』。

### 6.4 看病系统 ClinicManager / ClinicView

病人每天生成（有名字 NPC 概率 10%，普通病人随机立绘），状态机：Waiting → Deferred / Treated / Gone。

诊断流程：望闻问切（发现症状）→ 症状筛选药品 → 用药治疗 → 结算（成功赚银币、失败赔钱+怀疑度）。

```csharp
public TreatResult Treat(string potionId)
{
    if (_currentPatient == null) return TreatResult.Error;
    var potion = DatabaseManager.Instance.GetPotion(potionId);
    if (!InventoryManager.Instance.Has(potionId)) { _currentPatient.status = PatientStatus.Deferred; ResetDiagnosis(); return TreatResult.NoMedicine; }
    InventoryManager.Instance.Remove(potionId);
    bool success = potion.diseaseId == _currentPatient.diseaseId;
    _currentPatient.status = PatientStatus.Treated;
    if (success) SettleSuccess(_currentPatient, potion);
    else SettleFail(_currentPatient, potion);
    ResetDiagnosis();
    return success ? TreatResult.Success : TreatResult.WrongMedicine;
}
```

### 6.5 采集系统 GatherManager / GatherView

选择区域 → 显示可采草药 → 开始采集 → 播放动画 → 结算（随机数量、加背包、推进时间）。

```csharp
public List<ItemStackData> CompleteGather(string zoneId)
{
    var gained = new List<ItemStackData>();
    foreach (var herb in DatabaseManager.Instance.GetHerbsByZone(zoneId))
    {
        int qty = Random.Range(herb.minQuantity, herb.maxQuantity + 1);
        if (qty <= 0) continue;
        InventoryManager.Instance.Add(herb.id, qty);
        gained.Add(new ItemStackData { itemId = herb.id, count = qty });
    }
    return gained;
}
```

### 6.6 养成系统 ChildManager / ChildView

三个时段（上午/中午/下午）各安排一个任务（课程提升属性 / 野外探索获取草药），次日结算。

```csharp
private void SettleDailyTasks()
{
    if (!isDayTaskPlan) return;
    foreach (var m in _todayPlan) { /* 课程加属性 / 探索发草药 */ }
    _todayPlan.Clear();
}
```

### 6.7 背包与商店

- `InventoryManager`：物品增删查、堆叠、实现 ISaveable。
- `BackpackView`：分类展示、选择（送礼/出售）、出售。
- `ShopView`：按 `ShopType`（甜品/草药）展示商品、购买。

### 6.8 社交与情报

- `SocialManager`：好感度/怀疑度增减、认识 NPC、按阵营统计。
- `IntelligenceViews`：按阵营（村民/巫师/教会）分页展示 NPC 与数值。

### 6.9 捏脸系统

`UIFaceCustomizer` 提供部位（发型/眼睛/眉毛/嘴）与颜色选择，确认后发布 `OnAvatarCustomizationOver`；`PlayerDataManager` 保存部位标识并调用 `SetAppearance()`，通过 `SpriteCombiner` 运行时合成立绘。

```csharp
public void SetAppearance()
{
    List<Sprite> parts = new List<Sprite>();
    foreach (var key in _appearance.Keys)
        parts.Add(Resources.Load<Sprite>($"{ResourcePaths.FaceSculptingDir}/{_appearance[key]}"));
    PlayerAppearance = SpriteCombiner.CombineSprites(parts, PlayerAppearanceName);
}
```

### 6.10 NPC 与引导

- `NpcSpawner`：按天刷新场景 NPC（对象池复用），F 键交互最近 NPC。
- `GuideManager/GuideTip`：按目标名定位 UI，显示箭头与提示，点击遮罩推进。

### 6.11 地图与场景

- `MapView`：区域按钮，切换 HUD 并传送玩家。
- `SceneButtons`：场景内可点击对象（街道/教堂/甜品店等）。

<!-- 配图说明：这里插入「业务层系统关系图」，展示剧情、对话、炼药、看病、采集、养成、背包、社交、商店之间的数据流 -->

---

## 7. 关键技术方案

### 7.1 协程 vs Task 的选型

- 帧/时间驱动的视觉逻辑（淡入淡出、平移、逐字、等待）→ 协程（主线程、帧驱动、随对象销毁自动停）。
- IO/异步操作（读盘、网络、`await` 串联流程）→ `async/await` + `Task`，Unity 中 `await` 回主线程继续。
- 禁止 `Task.Run` 内碰 Unity API。

### 7.2 存档原子写 + 校验

```csharp
private static void WriteToDisk(int slot, byte[] bytes, string checksum)
{
    SavePaths.EnsureDirectory();
    string dat = SavePaths.FilePath(slot);
    string tmpDat = dat + ".tmp";
    File.WriteAllBytes(tmpDat, bytes);
    File.WriteAllText(checksumPath, checksum, Encoding.ASCII);
    AtomicReplace(tmpDat, dat);
}
```

### 7.3 数据驱动（ScriptableObject + CSV）

- 静态配置（草药/药品/病症/NPC/地区/任务）→ ScriptableObject，策划在 Inspector 配。
- 剧情与对话 → CSV，便于批量编辑与版本管理。
- 好处：改数值/剧情不改代码，便于迭代。

### 7.4 对象池设计

按 prefab 分池，预热减少运行时 instantiate；Return 时失活并挂回池根；标记来源避免误销毁。

### 7.5 事件优先级与过滤

EventBus 支持优先级控制同帧执行顺序，用于『先结算再刷新 UI』这类依赖场景。

---

## 8. 核心流程与时序

### 8.1 启动流程

1. GameManager.Awake 建状态机。
2. Start 中初始化核心服务（Config、Audio、SaveManager 等），进入 StartState。
3. StartView 展示存档栏位，用户选择新游戏/继续。
4. NewGameStartedEvent → ChangeState(GameLoop) → PlotDirector.Advance()。

```
BootState -> StartState -> GameLoopState -> (FreeRoam / FullUI / Dialog)
```

### 8.2 读档流程

1. SaveManager.Load(slot)：读盘 → 校验 SHA256 → 解析 SaveData。
2. 遍历 modules 调各模块 DeserializeState。
3. 缺省模块 ResetState。
4. 发布 SaveLoadedEvent，各系统自行刷新。

### 8.3 每日结算流程

NewDayEvent 发布后按优先级执行：

```
ChildManager.SettleDailyTasks(-10) → WorldManager.CheckRent(-10)
→ ClinicManager.EnsureDailyPatients(0) → NpcSpawner.Refresh(0)
```

### 8.4 炼药流程

```
选药方 -> 看配方 -> 开始炼制(扣材料/建任务/书右移) -> 后台计时
-> 完成(书停开位/弹窗) -> 确认(发药/书归位)
```

<!-- 配图说明：这里插入「炼药系统时序图」，展示 AlchemyView、AlchemyManager、InventoryManager、PopupManager 的调用顺序 -->

---

## 9. 性能与内存

### 9.1 对象池

UI 与 NPC 复用，减少 Instantiate/Destroy 与 GC。

### 9.2 GC 优化

- 避免每帧 new、字符串拼接用 StringBuilder；
- 缓存组件引用，避免每帧 GetComponent；
- 用泛型集合避免装箱。

### 9.3 资源加载

Resources 缓存已加载资源，减少重复加载；后续可迁移 Addressables 做按需加载与内存释放。

### 9.4 已知性能风险

- `GetHerbsByZone`/`GetNpcByName` 全表线性扫描，调用频繁时可建索引。
- 捏脸合成 Texture2D 每次新建，需销毁旧纹理避免泄漏。

---

## 10. 工程规范

### 10.1 命名规范

- 类：PascalCase（如 ClinicManager）。
- 方法/属性：PascalCase；私有字段：_camelCase。
- 常量：UPPER_SNAKE 或静态只读。

### 10.2 目录规范

- 按层分目录：0_Core / 1_Data / 2_Game。
- 资源按类型分：Resources/Prefabs/UI、Resources/Txts、Resources/Audio 等。

### 10.3 提交规范

- 建议 `feat/fix/docs/refactor` 前缀。
- 一次提交只做一件事。

### 10.4 代码规范

- 每个 View 只负责展示与输入，业务逻辑放 Manager。
- 跨模块动作走 EventBus，查询走 Manager 直接调用。
    
---

## 11. 已知问题与技术债

| 类别 | 问题 | 建议 |
|---|---|---|
| 构建 | 部分运行时文件引用编辑器专用命名空间（UnityEditor 等） | 删除或 `#if UNITY_EDITOR` 包裹，并真正出一次包 |
| 冗余 | EventBus 含大量注释笔记、AddressableLoader 未使用 | 清理死代码 |
| 逻辑 | 养子探索采集、F 键交互、传送点坐标、交租余额等 bug | 按清单逐个修复 |
| 架构 | 全局命名空间、无 asmdef | 后续引入命名空间与程序集划分 |
| 内存 | 捏脸纹理、Resources 缓存只增不减 | 补纹理销毁与卸载策略 |
| 功能 | 甜品店/暂停菜单/按键改绑半成品 | 按优先级补齐或从文档移除 |

---

## 12. 附录

### 12.1 目录与文件清单（摘要）

- 0_Core：Interface / EventBus / Data / SystemManager / Tools / UI
- 1_Data：Database / Models / Save / GameEvents / UIID / Location
- 2_Game：Alchemy / Clinic / Child / Dialogue / Gather / Inventory / Plot / Social / …

### 12.2 常用代码入口

| 想做什么 | 看哪里 |
|---|---|
| 加一个 UI 面板 | BaseUIView + UIManager.SwitchPanelAsync |
| 加一个存档模块 | ISaveable + SaveManager.RegisterModule |
| 加一个事件 | EventBase + EventBus.Publish/Subscribe |
| 加一个数据表 | 新建 ScriptableObject Database + DatabaseManager 聚合 |
| 加一段剧情 | StoryLine.csv |

### 12.3 版本记录

| 版本 | 日期 | 说明 |
|---|---|---|
| v1.0 | 2026-11 | 初版 |

---

## 13. 常见问题排查（FAQ）

### 13.1 面板打不开 / 找不到预制体

检查 `UIID` 的 key 与 `Resources/Prefabs/UI/` 下的预制件名是否一致；`OpenPanelAsync` 找不到时会 `LogError`。

### 13.2 返回时报『已是根面板』

说明栈里只有根界面。常见原因是用 `OpenPanel` 打开的面板没压栈；应改用 `SwitchPanelAsync`。

### 13.3 存档读不出来

先看是否 SHA256 校验失败（会隔离成 `.corrupt`）；再检查 DTO 是否用了 JsonUtility 不支持的字段（long/Dictionary/UnityEngine.Object）。

### 13.4 倒计时不动 / 太快

倒计时若用游戏时间，游戏暂停时会冻结；应改用真实时间（Time.deltaTime 递减剩余秒数）。

### 13.5 对象池拿到的实例是错的

检查 `PooledObject.Prefab` 标记是否被覆盖，以及是否把非池实例 `Return` 回去（会被直接销毁）。

### 13.6 打包后运行报错、编辑器里正常

多半是运行时文件引用了编辑器专用命名空间（UnityEditor 等），需删除或 `#if UNITY_EDITOR` 包裹。

### 13.7 事件不触发

检查订阅是否在发布之前完成（Awake/Start 顺序），以及订阅令牌是否被提前 Dispose。

---

## 14. 代码风格示例

```csharp
// 好的写法：职责单一、命名清晰、事件解耦
public class ClinicManager : MonoSingleton<ClinicManager>, ISaveable
{
    public string SaveKey => "clinic";
    public int SaveVersion => 2;

    private readonly List<Patient> _patients = new();

    public void EnsureDailyPatients() { /* ... */ }
    public TreatResult Treat(string potionId) { /* ... */ }
    public string SerializeState() { /* ... */ }
    public void DeserializeState(string json, int version) { /* ... */ }
    public void ResetState() { /* ... */ }
}
```
