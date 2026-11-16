using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatabaseManager : MonoSingleton<DatabaseManager>
{
    [Header("数据库")]
    [SerializeField] private HerbDatabase herbDatabase;
    [SerializeField] private PotionDatabase potionDatabase;
    [SerializeField] private DessertDatabase dessertDatabase;
    [SerializeField] private DiseaseDatabase diseaseDatabase;
    [SerializeField] private SymptomDatabase symptomDatabase;
    [SerializeField] private NpcDatabase npcDatabase;
    [SerializeField] private MissionDatabase missionDatabase;
    [SerializeField] private ZoneDatabase zoneDatabase;

    private readonly Dictionary<string, Item> _items = new();
    private readonly Dictionary<string, Potion> _potions = new();
    private readonly Dictionary<string, Disease> _diseases = new();
    private readonly Dictionary<string, Symptom> _symptoms = new();
    private readonly Dictionary<string, Npc> _npcs = new();
    private readonly Dictionary<string, Mission> _missions = new();
    private readonly Dictionary<string, Zone> _zones = new();
    protected override void Awake()
    {
        base.Awake();
        if (herbDatabase?.herbs != null) foreach (var h in herbDatabase.herbs) _items[h.id] = h;
        if (potionDatabase?.potions != null) foreach (var p in potionDatabase.potions) { _items[p.id] = p; _potions[p.id] = p; }
        if (dessertDatabase?.desserts != null) foreach (var d in dessertDatabase.desserts) _items[d.id] = d;
        if (diseaseDatabase?.diseases != null) foreach (var d in diseaseDatabase.diseases) _diseases[d.id] = d;
        if (symptomDatabase?.symptoms != null) foreach (var s in symptomDatabase.symptoms) _symptoms[s.id] = s;
        if (npcDatabase?.npcs != null) foreach (var n in npcDatabase.npcs) _npcs[n.id] = n;
        if (missionDatabase?.missions != null) foreach (var m in missionDatabase.missions) _missions[m.id] = m;
        if (zoneDatabase?.zones != null) foreach (var z in zoneDatabase.zones) _zones[z.id] = z;
    }

    #region Mission
    /// <summary>根据id查找任务</summary>
    public Mission GetMission(string id) => _missions.TryGetValue(id, out var v) ? v : null;
    /// <summary>返回所有Mission的集合</summary>
    public IEnumerable<Mission> AllMissions => _missions.Values;
    #endregion

    #region Disease
    /// <summary>根据id查找疾病</summary>
    public Disease GetDisease(string id) => _diseases.TryGetValue(id, out var v) ? v : null;
    /// <summary>根据id查找症状</summary>
    public Symptom GetSymptom(string id) => _symptoms.TryGetValue(id, out var v) ? v : null;

    /// <summary>返回所有Disease的集合</summary>
    public IEnumerable<Disease> AllDiseases => _diseases.Values;
    #endregion

    #region item
    /// <summary>根据id查找物品</summary>
    public Item GetItem(string id) => _items.TryGetValue(id, out var v) ? v : null;

    /// <summary>所有物品（草药 + 药品 + 甜品）。</summary>
    public IEnumerable<Item> AllItems => _items.Values;
    #endregion

    #region Potion
    /// <summary>根据id查找药品</summary>
    public Potion GetPotion(string id) => _potions.TryGetValue(id, out var v) ? v : null;
    /// <summary>返回所有potion的集合</summary>
    public IEnumerable<Potion> AllPotions => _potions.Values;
    #endregion

    #region NPC
    /// <summary>根据id查找NPC</summary>
    public Npc GetNpc(string id) => _npcs.TryGetValue(id, out var v) ? v : null;
    /// <summary>按阵营取所有 NPC（总体怀疑度计算用）。</summary>
    public List<Npc> GetNpcsOfFaction(FactionType faction)
    {
        var list = new List<Npc>();
        foreach (var npc in _npcs.Values)
            if (npc.category == faction)
                list.Add(npc);
        return list;
    }

    /// <summary>按显示名查找 NPC（对话 CSV 用的是名字）。</summary>
    public Npc GetNpcByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var npc in _npcs.Values)
            if (npc.name == name) return npc;
        return null;
    }
    #endregion

    #region Zone    
    /// <summary>根据 id 查找地区。</summary>
    public Zone GetZone(string id) => _zones.TryGetValue(id, out var v) ? v : null;

    /// <summary>返回某地区可采集到的所有草药（按 Herb.zoneIds 匹配）。</summary>
    public List<Herb> GetHerbsByZone(string zoneId)
    {
        var result = new List<Herb>();
        if (string.IsNullOrEmpty(zoneId)) return result;
        foreach (var item in _items.Values)
            if (item is Herb herb && herb.zoneIds != null && herb.zoneIds.Contains(zoneId))
                result.Add(herb);
        return result;
    }

    /// <summary>所有地区。</summary>
    public IEnumerable<Zone> AllZones => _zones.Values;
    #endregion
}
