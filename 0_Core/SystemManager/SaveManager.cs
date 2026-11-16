using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器（单例）。
/// 设计要点：
/// - 可扩展：任何系统实现 ISaveable 并注册即可纳入存档，本类不感知具体业务类型。
/// - 安全：原子写（临时文件 + File.Replace）+ SHA256 完整性校验 + 损坏档隔离（.corrupt）。
/// - 性能：纯数据 DTO 主线程序列化，磁盘 IO 可异步。
/// - 解耦：通过 EventBus 广播存/读结果。
/// - 前向兼容：未知模块原样保留写回，不丢数据。
/// </summary>
public class SaveManager : MonoSingleton<SaveManager>
{
    #region 基础配置
    /// <summary>已注册的存档模块（按 SaveKey 去重）。</summary>
    private readonly Dictionary<string, ISaveable> _modules = new Dictionary<string, ISaveable>();
    /// <summary>
    /// 存档中存在但当前未注册的模块（前向兼容保留）。
    /// </summary>
    private readonly Dictionary<string, SaveModuleEntry> _orphanedEntries = new Dictionary<string, SaveModuleEntry>();

    private bool _isBusy;

    /// <summary>是否正在执行存档/读档操作（防重入）。</summary>
    public bool IsBusy => _isBusy;

    private int _currentSlot = -1;   // 当前正在玩的槽位；-1 = 无（在主菜单/选档界面）

    /// <summary>当前正在使用的槽位；-1 表示还没有活动存档。</summary>
    public int CurrentSlot => _currentSlot;

    /// <summary>是否已有活动槽位（已读档或已开始新游戏）。</summary>
    public bool HasActiveSlot => _currentSlot >= 0;

    protected override void Awake()
    {
        base.Awake();
        SavePaths.EnsureDirectory();
        // 时间模块由 TimeManager 自身实现 ISaveable 并在其 Start() 注册，此处无需内置注册
    }
    #endregion

    #region 模块注册

    /// <summary>注册存档模块。按 SaveKey 去重，重复注册覆盖旧实例。</summary>
    /// <param name="module">实现 ISaveable 的模块实例。</param>
    public void RegisterModule(ISaveable module)
    {
        if (module == null) return;
        if (string.IsNullOrEmpty(module.SaveKey))
        {
            Debug.LogError("[SaveManager] 模块 SaveKey 为空，拒绝注册。");
            return;
        }
        //ReferenceEquals 判断两个对象是否是同一个实例，避免重复注册同一个对象时发出警告
        if (_modules.TryGetValue(module.SaveKey, out var exist) && !ReferenceEquals(exist, module))
            Debug.LogWarning($"[SaveManager] 模块 {module.SaveKey} 重复注册，已覆盖。");
        _modules[module.SaveKey] = module;
       Debug.Log($"[SaveManager] 模块 {module.SaveKey} 注册成功。");
    }

    /// <summary>注销存档模块（仅实例相同时移除）。</summary>
    /// <param name="module">要移除的模块实例。</param>
    public void UnregisterModule(ISaveable module)
    {
        if (module == null) return;
        if (_modules.TryGetValue(module.SaveKey, out var exist) && ReferenceEquals(exist, module))
            _modules.Remove(module.SaveKey);
    }

    /// <summary>按键获取已注册模块。</summary>
    /// <param name="key">模块 SaveKey。</param>
    /// <returns>模块实例；未注册返回 null。</returns>
    public ISaveable GetModule(string key) => _modules.TryGetValue(key, out var m) ? m : null;

    #endregion

    #region 对外接口

    /// <summary>指定槽位是否存在存档。</summary>
    /// <param name="slot">槽位索引。</param>
    public bool HasSave(int slot) => File.Exists(SavePaths.FilePath(slot));

    /// <summary>只读档头（加载界面展示用），不加载全部数据。</summary>
    /// <param name="slot">槽位索引。</param>
    /// <returns>档头；不存在或解析失败返回 null。</returns>
    public SaveHeader PeekHeader(int slot)
    {
        try
        {
            if (!File.Exists(SavePaths.FilePath(slot))) return null;
            string json = File.ReadAllText(SavePaths.FilePath(slot), Encoding.UTF8);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data?.header;
        }
        catch { return null; }
    }

    /// <summary>同步存档到指定槽位。</summary>
    /// <param name="slot">槽位索引。</param>
    public void Save(int slot)
    {
        if (!TryBegin()) return;
        try { SaveCore(slot, false).GetAwaiter().GetResult(); }
        catch (Exception ex) { PublishSaveFailed(slot, ex.Message); }
        finally { _isBusy = false; }
    }

    /// <summary>异步存档（磁盘 IO 在后台线程，序列化在主线程）。</summary>
    /// <param name="slot">槽位索引。</param>
    public async Task SaveAsync(int slot)
    {
        if (!TryBegin()) return;
        try { await SaveCore(slot, true); }
        catch (Exception ex) { PublishSaveFailed(slot, ex.Message); }
        finally { _isBusy = false; }
    }

    /// <summary>保存到当前活动槽位。</summary>
    public void Save()
    {
        if (!HasActiveSlot)
        {
            Debug.LogWarning("[SaveManager] 当前无活动槽位，无法存档。");
            return;
        }
        Save(_currentSlot);
    }

    /// <summary>退出 / 结束本次存档时调用（同步写盘）。</summary>
    public void AutoSave()
    {
        if (!HasActiveSlot) return;
        Debug.Log("[SaveManager] 自动存档中...");
        Save(_currentSlot);
    }

    /// <summary>同步读档，成功返回 true。</summary>
    /// <param name="slot">槽位索引。</param>
    public bool Load(int slot)
    {
        if (!TryBegin()) return false;
        try
        {
            bool ok = LoadCore(slot, false).GetAwaiter().GetResult();
            if (ok) _currentSlot = slot;   // ← 玩家在选档界面选了哪个，就记住哪个
            return ok;
        }
        catch (Exception ex) { PublishLoadResult(slot, false, ex.Message); return false; }
        finally { _isBusy = false; }
    }

    /// <summary>异步读档，成功返回 true。</summary>
    /// <param name="slot">槽位索引。</param>
    public async Task<bool> LoadAsync(int slot)
    {
        if (!TryBegin()) return false;
        try {
            bool ok = await LoadCore(slot, true);
            if (ok) _currentSlot = slot;   // ← 玩家在选档界面选了哪个，就记住哪个
            return ok;
        }
        catch (Exception ex) { PublishLoadResult(slot, false, ex.Message); return false; }
        finally { _isBusy = false; }
    }

    /// <summary>删除指定槽位的存档（数据文件 + 校验文件）。</summary>
    /// <param name="slot">槽位索引。</param>
    public void Delete(int slot)
    {
        bool deleted = TryDeleteFile(SavePaths.FilePath(slot));
        deleted |= TryDeleteFile(SavePaths.ChecksumPath(slot));
        if (deleted) EventBus.Instance.Publish(new SaveDeletedEvent(slot));
    }

    /// <summary>开始新游戏：所有模块恢复默认状态（不写盘）。</summary>
    public void NewGame(int slot)
    {
        if (!TryBegin()) return;
        try
        {
            _orphanedEntries.Clear();
            foreach (var kvp in _modules)
            {
                try { kvp.Value.ResetState(); }
                catch (Exception ex) { Debug.LogError($"[SaveManager] 模块 {kvp.Key} 重置失败: {ex.Message}"); }
            }
            _currentSlot = slot;   // ← 记住玩家选的槽位
            EventBus.Instance.Publish(new NewGameStartedEvent());
        }
        finally { _isBusy = false; }
    }

    /// <summary>
    /// 结束本次游玩、返回主菜单：把存档系统恢复到“刚打开游戏”的状态。
    /// </summary>
    /// <param name="saveBeforeExit">返回菜单前是否先保存当前进度（有活动槽位才存）。</param>
    public void EndSession(bool saveBeforeExit = true)
    {
        if (saveBeforeExit)
            AutoSave();             // 有活动槽位才存，无则跳过

        _currentSlot = -1;          // 1. 清空当前槽位 → 回到“无当前存档”
        _orphanedEntries.Clear();   // 2. 丢弃上次读档带来的孤儿数据
    }
    #endregion

    #region 内部实现

    /// <summary>尝试进入“忙碌”态；已在忙碌则忽略本次请求。</summary>
    private bool TryBegin()
    {
        if (_isBusy)
        {
            Debug.LogWarning("[SaveManager] 正在执行存档操作，忽略本次请求。");
            return false;
        }
        _isBusy = true;
        return true;
    }

    /// <summary>构建 SaveData 并序列化为字节 + 计算校验和。</summary>
    private string BuildJson(int slot, out byte[] bytes, out string checksum)
    {
        var data = new SaveData
        {
            formatVersion = SaveConstants.FORMAT_VERSION,
            header = BuildHeader(slot)
        };

        foreach (var kvp in _modules)
        {
            try
            {
                if (kvp.Value is ISaveLifecycleListener l) l.OnBeforeSave();
                string payload = kvp.Value.SerializeState();
                if (string.IsNullOrEmpty(payload)) continue;
                data.modules.Add(new SaveModuleEntry { key = kvp.Key, version = kvp.Value.SaveVersion, payload = payload });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] 模块 {kvp.Key} 序列化失败: {ex.Message}");
            }
        }

        foreach (var orphan in _orphanedEntries.Values)
            data.modules.Add(orphan);
        Debug.Log($"[SaveManager] 构建存档 JSON 完成，模块数={data.header.ToString()}");
        string json = JsonUtility.ToJson(data, false);
        bytes = Encoding.UTF8.GetBytes(json);
        checksum = ComputeChecksum(bytes);
        return json;
    }

    /// <summary>构建档头元信息。</summary>
    private SaveHeader BuildHeader(int slot)
    {
        float totalSec = 0f;
        if (GetModule("time") is TimeManager tm)
            totalSec = tm.CurrentTotalSeconds;

        int plotStep = 0;
        int locationId = 0;
        int sessionState = 0;
        if (GetModule("player") is PlayerDataManager pdm)
        {
            plotStep = pdm.PlotStep;
            locationId = (int)pdm.LastLocationId;
            sessionState = (int)pdm.LastSessionState;
        }
        // 退出时活动场景可能正在卸载，兜底一下
        string sceneName = "";
        //try { sceneName = SceneManager.GetActiveScene().name; } catch { }

        return new SaveHeader
        {
            slotIndex = slot,
            saveName = "",
            realUtcTimestamp = DateTime.UtcNow.ToString("o"),
            gameTotalSeconds = totalSec,
            gameDayIndex = Mathf.FloorToInt(totalSec / 86400f),
            sceneName = sceneName,
            plotStep = plotStep,
            locationId = locationId,
            sessionState = sessionState,
            gameVersion = Application.version
        };
    }

    /// <summary>存档核心：序列化后写盘（可同步/异步）。</summary>
    private async Task SaveCore(int slot, bool async)
    {
        string json = BuildJson(slot, out byte[] bytes, out string checksum);
        if (async) await Task.Run(() => WriteToDisk(slot, bytes, checksum));
        else WriteToDisk(slot, bytes, checksum);
        EventBus.Instance.Publish(new SaveCompletedEvent(slot));
    }

    /// <summary>读档核心：读盘 → 校验 → 反序列化 → 应用。可同步/异步。</summary>
    private async Task<bool> LoadCore(int slot, bool async)
    {
        string datPath = SavePaths.FilePath(slot);
        if (!File.Exists(datPath))
        {
            PublishLoadResult(slot, false, "存档不存在");
            return false;
        }

        byte[] bytes = async ? await Task.Run(() => File.ReadAllBytes(datPath)) : File.ReadAllBytes(datPath);

        string shaPath = SavePaths.ChecksumPath(slot);
        if (File.Exists(shaPath))
        {
            string expected = File.ReadAllText(shaPath).Trim();
            if (!string.Equals(expected, ComputeChecksum(bytes), StringComparison.OrdinalIgnoreCase))
            {
                QuarantineCorrupted(slot, bytes);
                PublishLoadResult(slot, false, "存档校验失败，已隔离损坏文件");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[SaveManager] 槽位 {slot} 缺少校验文件，跳过完整性校验。");
        }

        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(Encoding.UTF8.GetString(bytes)); }
        catch (Exception ex) { QuarantineCorrupted(slot, bytes); PublishLoadResult(slot, false, $"存档解析失败: {ex.Message}"); return false; }

        if (data == null) { QuarantineCorrupted(slot, bytes); PublishLoadResult(slot, false, "存档解析结果为空"); return false; }

        ApplySaveData(data);
        PublishLoadResult(slot, true, null);
        return true;
    }

    /// <summary>把 SaveData 分发到各模块。</summary>
    private void ApplySaveData(SaveData data)
    {
        _orphanedEntries.Clear();
        var inSave = new HashSet<string>();

        foreach (var entry in data.modules)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
            inSave.Add(entry.key);

            if (_modules.TryGetValue(entry.key, out var module))
            {
                if (entry.version > module.SaveVersion)
                {
                    _orphanedEntries[entry.key] = entry;
                    Debug.LogWarning($"[SaveManager] 模块 {entry.key} 存档版本({entry.version})高于代码版本({module.SaveVersion})，已保留不覆盖。");
                    continue;
                }
                try { module.DeserializeState(entry.payload, entry.version); }
                catch (Exception ex) { Debug.LogError($"[SaveManager] 模块 {entry.key} 恢复失败: {ex.Message}"); }
            }
            else
            {
                _orphanedEntries[entry.key] = entry;
                Debug.LogWarning($"[SaveManager] 存档包含未注册模块 {entry.key}，已保留待写回。");
            }
        }

        foreach (var kvp in _modules)
        {
            if (inSave.Contains(kvp.Key)) continue;
            try { kvp.Value.ResetState(); }
            catch (Exception ex) { Debug.LogError($"[SaveManager] 模块 {kvp.Key} 重置失败: {ex.Message}"); }
        }

        foreach (var kvp in _modules)
        {
            if (kvp.Value is ISaveLifecycleListener l)
            {
                try { l.OnAfterLoad(); }
                catch (Exception ex) { Debug.LogError($"[SaveManager] 模块 {kvp.Key} OnAfterLoad 失败: {ex.Message}"); }
            }
        }
    }

    /// <summary>原子写盘：先写临时文件，再用 File.Replace 替换。</summary>
    private static void WriteToDisk(int slot, byte[] bytes, string checksum)
    {
        SavePaths.EnsureDirectory();
        string dat = SavePaths.FilePath(slot);
        string sha = SavePaths.ChecksumPath(slot);
        string tmpDat = dat + ".tmp";
        string tmpSha = sha + ".tmp";

        File.WriteAllBytes(tmpDat, bytes);
        File.WriteAllText(tmpSha, checksum, Encoding.ASCII);

        AtomicReplace(tmpDat, dat);
        AtomicReplace(tmpSha, sha);
    }

    /// <summary>用临时文件原子替换目标文件。</summary>
    private static void AtomicReplace(string tmp, string dest)
    {
        if (File.Exists(dest))
        {
            try { File.Replace(tmp, dest, dest + ".bak"); }
            catch { File.Copy(tmp, dest, true); File.Delete(tmp); }
        }
        else
        {
            File.Move(tmp, dest);
        }
    }

    /// <summary>把损坏存档隔离到 .corrupt 文件。</summary>
    private static void QuarantineCorrupted(int slot, byte[] bytes)
    {
        try
        {
            string quarantine = SavePaths.FilePath(slot) + ".corrupt." + DateTime.UtcNow.Ticks;
            File.WriteAllBytes(quarantine, bytes);
        }
        catch (Exception ex) { Debug.LogError($"[SaveManager] 隔离损坏存档失败: {ex.Message}"); }
    }

    /// <summary>尝试删除文件；成功返回 true。</summary>
    private static bool TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) { File.Delete(path); return true; } }
        catch (Exception ex) { Debug.LogError($"[SaveManager] 删除失败 {path}: {ex.Message}"); }
        return false;
    }

    /// <summary>计算字节数组的 SHA256 摘要（Base64）。</summary>
    private static string ComputeChecksum(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    /// <summary>广播存档失败。</summary>
    private void PublishSaveFailed(int slot, string reason)
    {
        Debug.LogError($"[SaveManager] 存档失败 slot={slot}: {reason}");
        EventBus.Instance.Publish(new SaveFailedEvent(slot, reason));
    }

    /// <summary>广播读档结果。</summary>
    private void PublishLoadResult(int slot, bool success, string error)
    {
        if (!success) Debug.LogError($"[SaveManager] 读档失败 slot={slot}: {error}");
        EventBus.Instance.Publish(new SaveLoadedEvent(slot, success, error));
    }

    #endregion

    #region OnApplicationQuit
    protected override void OnApplicationQuit()
    {
        AutoSave();               // 同步写盘
        base.OnApplicationQuit();
    }
    #endregion
}