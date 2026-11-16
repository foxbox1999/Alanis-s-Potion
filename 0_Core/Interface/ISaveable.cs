using System;

/// <summary>
/// 存档模块统一接口。
/// 任何需要持久化的系统（背包、看病、养子、炼药、社交、世界进度、玩家数据……）
/// 实现本接口，并在 Start() 中调用 <see cref="SaveManager.RegisterModule"/> 注册即可，
/// 无需改动 <see cref="SaveManager"/> 本体，即可获得“可扩展存档”。
///
/// 使用约定：
/// 1. <see cref="SerializeState"/> / <see cref="DeserializeState"/> / <see cref="ResetState"/>
///    只能操作纯数据 DTO（标记 [Serializable]，且不含 UnityEngine.Object、委托、Dictionary、
///    long 等 JsonUtility 无法序列化的字段）。
/// 2. 三个方法都必须在主线程调用。
/// 3. <see cref="SaveKey"/> 全局唯一且稳定：发版后改名会导致旧档无法匹配，请勿随意修改。
/// </summary>
public interface ISaveable
{
    /// <summary>模块唯一键，例如 "inventory"、"clinic"、"player"。全局唯一且不可变。</summary>
    string SaveKey { get; }

    /// <summary>本模块存档格式版本号，用于读档时的迁移判断（存档版本 &gt; 代码版本时跳过并保留原数据）。</summary>
    int SaveVersion { get; }

    /// <summary>
    /// 把当前运行时状态序列化为 JSON 字符串（内部使用 JsonUtility.ToJson）。
    /// </summary>
    /// <returns>本模块状态 DTO 的 JSON 字符串；返回 null 或空串表示本轮无需存档（SaveManager 会跳过）。</returns>
    string SerializeState();

    /// <summary>
    /// 用 JSON 字符串恢复运行时状态。
    /// </summary>
    /// <param name="json">由 <see cref="SerializeState"/> 生成的 JSON 字符串。</param>
    /// <param name="version">存档中记录的版本号，供本模块做向下迁移判断。</param>
    void DeserializeState(string json, int version);

    /// <summary>开始新游戏 / 存档中缺失该模块时调用，把运行时状态恢复为默认值。</summary>
    void ResetState();
}

/// <summary>
/// 可选的存档生命周期钩子接口。
/// 模块如需在“写档前刷新派生数据”或“读档后重建回调/刷新 UI”，可额外实现本接口，
/// 由 <see cref="SaveManager"/> 在合适时机自动调用。不实现则无任何影响。
/// </summary>
public interface ISaveLifecycleListener
{
    /// <summary>存档即将写入前触发（可在此时刷新派生数据、做快照等）。</summary>
    void OnBeforeSave();

    /// <summary>所有模块 <see cref="ISaveable.DeserializeState"/> 完成后触发（用于重建委托回调、刷新 UI 等）。</summary>
    void OnAfterLoad();
}