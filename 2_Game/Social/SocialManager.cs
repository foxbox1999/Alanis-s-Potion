using System.Collections.Generic;
using UnityEngine;

/// <summary>社交管理器：每个 NPC 单独的好感度（巫师）/ 怀疑度（镇民、教会），总体怀疑度是算出来的。</summary>
public class SocialManager : MonoSingleton<SocialManager>, ISaveable
{
    public string SaveKey => "social";
    public int SaveVersion => 1;

    // npcId → 好感度/怀疑度
    private readonly Dictionary<string, int> _values = new();

    protected override void Start() { base.Start(); SaveManager.Instance?.RegisterModule(this); }

    // ===== 通用（底层） =====
    public int GetValue(string npcId) => _values.TryGetValue(npcId, out var v) ? v : 0;
    public void AddValue(string npcId, int delta)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        if (!_values.ContainsKey(npcId))
        {
            Debug.LogError($"该npc「{npcId}」还未认识，请检查");
            return; //如果不包含该npc表示没认识肯定哪里错了
        }
        _values[npcId] = GetValue(npcId) + delta;
    }

    // ===== 好感度（巫师） =====
    public int GetFavor(string witchNpcId) => GetValue(witchNpcId);
    public void AddFavor(string witchNpcId, int delta) => AddValue(witchNpcId, delta);

    // ===== 怀疑度（镇民 / 教会，单个 NPC） =====
    public int GetSuspicion(string npcId) => GetValue(npcId);
    public void AddSuspicion(string npcId, int delta) => AddValue(npcId, delta);

    /// <summary>总体怀疑度 = 所有镇民 + 教会 NPC 怀疑度的平均值（计算得出，不存储）。</summary>
    public float GetOverallSuspicion()
    {
        float sum = 0f;
        int count = 0;
        foreach (var npc in DatabaseManager.Instance.GetNpcsOfFaction(FactionType.Townsfolk)) { sum += GetValue(npc.id); count++; }
        foreach (var npc in DatabaseManager.Instance.GetNpcsOfFaction(FactionType.Church)) { sum += GetValue(npc.id); count++; }
        return count == 0 ? 0f : sum / count;
    }

    #region Meet 认识
    /// <summary>认识 NPC：已认识返回 false（无作为），新认识返回 true。</summary>
    public bool Meet(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return false;
        if (_values.ContainsKey(npcId)) return false;   // 已认识
        _values[npcId] = 0;                             // 新认识，初始值 0
        return true;
    }

    /// <summary>是否已认识该 NPC。</summary>
    public bool HasMet(string npcId) => _values.ContainsKey(npcId);

    /// <summary>已认识的某阵营 NPC（情报只显示遇到过的）。</summary>
    public List<Npc> GetMetNpcs(FactionType faction)
    {
        var list = new List<Npc>();
        foreach (var npc in DatabaseManager.Instance.GetNpcsOfFaction(faction))
            if (_values.ContainsKey(npc.id)) list.Add(npc);
        return list;
    }
    #endregion

    #region ISaveable
    public string SerializeState()
    {
        var data = new SocialSaveData();
        foreach (var kv in _values)
            data.npcs.Add(new NpcSocialEntry { npcId = kv.Key, value = kv.Value });
        return JsonUtility.ToJson(data);
    }

    public void DeserializeState(string json, int version)
    {
        if (string.IsNullOrEmpty(json)) { ResetState(); return; }
        var dto = JsonUtility.FromJson<SocialSaveData>(json);
        _values.Clear();
        if (dto?.npcs != null)
            foreach (var e in dto.npcs)
                if (!string.IsNullOrEmpty(e.npcId)) _values[e.npcId] = e.value;
    }

    public void ResetState() => _values.Clear();
    #endregion
}