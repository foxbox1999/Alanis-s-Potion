using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 看病管理器：每日病人、望闻问切（发现症状）、症状筛选药品、治疗结算。
/// - 持久化：病人列表、当天索引。
/// - 运行时（不存档）：当前看诊病人、已发现症状、已选择症状。
/// </summary>
public class ClinicManager : MonoSingleton<ClinicManager>, ISaveable
{
    #region 存档契约
    public string SaveKey => "clinic";
    public int SaveVersion => 2;
    #endregion

    #region 配置
    [Header("结算配置")]
    [Header("结算（银币）")]
    [Tooltip("成功收入 = 药价值 × 此比例（1.1 = 110%）")]
    [SerializeField] private float _successRatio = 1.1f;

    [Tooltip("失败损失 = 药价值 × 此比例（0.5 = 50%）")]
    [SerializeField] private float _failRatio = 0.5f;

    [Tooltip("失败怀疑值变化")]
    [SerializeField] private int _suspicionDelta = 10;   // 特殊病人怀疑度变化

    [Header("病人生成")]
    [Tooltip("每天最多病人数")]
    [SerializeField] private int _maxPatientsPerDay = 3; // 每天最多病人数
    [Tooltip(" 病人最多等几天（之后消失）")]
    [SerializeField] private int _patientMaxWaitDays = 3; // 病人最多等几天（之后消失）

    [Header("有名字 NPC 病人")]
    [Tooltip("有名字 NPC 出现概率（0~1），默认 10%")]
    [SerializeField, Range(0f, 1f)] private float _namedNpcChance = 0.1f;
    [Tooltip("能当病人的有名字 NPC（有症状立绘的）")]
    [SerializeField] private string[] _namedPatientIds = { "Arlo", "Susan", "Pardal", "Evans", "Viggo" };
    [Tooltip("普通病人立绘数量（Man_0 ~ Man_N-1）")]
    [SerializeField] private int _ordinaryPortraitCount = 16;
    #endregion

    #region 持久化数据
    private readonly List<Patient> _patients = new();
    private int _clinicDayIndex = -1;
    #endregion

    #region 运行时数据（不存档）
    private Patient _currentPatient;                        // 当前正在看的病人
    private readonly HashSet<string> _discoveredSymptoms = new(); // 已发现症状
    private readonly HashSet<string> _selectedSymptoms = new();   // 已选择症状（筛选用）

    // 【新增】有名字 NPC 可得的病症表（懒构建，构建一次缓存）
    private readonly Dictionary<string, List<string>> _npcAvailableDiseases = new();
    private bool _npcTableBuilt;
    private int _askIndex;   // 问诊索引（说到第几个口述症状）
    private IDisposable _newDaySub;
    #endregion

    #region 生命周期
    protected override void Start() { base.Start(); SaveManager.Instance?.RegisterModule(this); _newDaySub = EventBus.Instance.Subscribe<NewDayEvent>(e => EnsureDailyPatients(), 0); }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _newDaySub?.Dispose();
    }
    #endregion

    #region ===== 每日病人生成 =====

    /// <summary>进入诊所时调用：当天首次则生成病人（第一天没人）。</summary>
    public void EnsureDailyPatients()
    {
        int day = WorldManager.Instance.CurrentDay;
        if (day < 1) return;              // 第一天没人，第二天（day>=1）才有
        if (day == _clinicDayIndex) return; // 当天已生成

        _clinicDayIndex = day;
        TickUntreatedPatients();          // 未治疗的病人天数 -1，到 0 消失

        int count = Random.Range(0, _maxPatientsPerDay + 1);  // 0 ~ max
        for (int i = 0; i < count; i++)
            _patients.Add(GeneratePatient());
    }

    /// <summary>未治疗病人每天 -1 天，到 0 变 Gone（3 天消失）。</summary>
    private void TickUntreatedPatients()
    {
        foreach (var p in _patients)
        {
            if (p.status == PatientStatus.Waiting || p.status == PatientStatus.Deferred)
            {
                p.remainingDays--;
                if (p.remainingDays <= 0) p.status = PatientStatus.Gone;
            }
        }
    }

    /// <summary>随机生成一个病人（病症 + NPC + 是否特殊）。</summary>
    private Patient GeneratePatient()
    {
        EnsureNpcTable();
        // 有名字 NPC：10% 概率，病症受限
        if (_namedPatientIds.Length > 0 && Random.value < _namedNpcChance)
        {
            string npcId = _namedPatientIds[Random.Range(0, _namedPatientIds.Length)];
            if (_npcAvailableDiseases.TryGetValue(npcId, out var diseases) && diseases.Count > 0)
            {
                return new Patient
                {
                    npcId = npcId,
                    portraitId = npcId,
                    diseaseId = diseases[Random.Range(0, diseases.Count)],
                    status = PatientStatus.Waiting,
                    remainingDays = _patientMaxWaitDays,
                    isNamed = true
                };
            }
        }
        return GenerateOrdinaryPatient();
    }
    private Patient GenerateOrdinaryPatient()
    {
        var diseases = DatabaseManager.Instance.AllDiseases.ToList();
        return new Patient
        {
            npcId = "",
            portraitId = "Man_" + Random.Range(0, _ordinaryPortraitCount),
            diseaseId = diseases[Random.Range(0, diseases.Count)].id,
            status = PatientStatus.Waiting,
            remainingDays = _patientMaxWaitDays,
            isNamed = false
        };
    }
    #endregion

    #region ===== 有名字 NPC 病症表（懒构建） =====
    private void EnsureNpcTable()
    {
        if (_npcTableBuilt) return;
        _npcTableBuilt = true;

        _npcAvailableDiseases.Clear();
        foreach (var npcId in _namedPatientIds)
        {
            var available = new List<string>();
            foreach (var disease in DatabaseManager.Instance.AllDiseases)
                if (NpcCanHaveDisease(npcId, disease))
                    available.Add(disease.id);
            _npcAvailableDiseases[npcId] = available;
        }
    }

    /// <summary>NPC 是否可得该病：所有「可见」症状都必须有该 NPC 的立绘。</summary>
    private bool NpcCanHaveDisease(string npcId, Disease disease)
    {
        foreach (var symptomId in disease.symptoms)
        {
            var symptom = DatabaseManager.Instance.GetSymptom(symptomId);
            if (symptom == null || symptom.displayType != SymptomDisplayType.Visual) continue;
            string english = symptomId.Replace("Symptom_", "");
            if (Resources.Load<Sprite>($"Images/Symptoms/NPCSymptoms/Symptom_{npcId}_{english}") == null)
                return false;
        }
        return true;
    }
    #endregion

    #region ===== 立绘辅助 =====
    /// <summary>病人全身立绘。</summary>
    public Sprite GetPatientPortrait(Patient p)
    {
        if (p == null) return null;
        if (p.isNamed)
        {
            var npc = DatabaseManager.Instance.GetNpc(p.npcId);
            Debug.Log($"[Clinic] named病人={p.npcId}, GetNpc非空={npc != null}, sprite非空={npc != null && npc.sprite != null}");
            return npc?.sprite;
        }
        var s = Resources.Load<Sprite>($"Images/Symptoms/{p.portraitId}");
        Debug.Log($"[Clinic] 普通病人 portraitId={p.portraitId}, sprite非空={s != null}");
        return s;
    }

    /// <summary>症状立绘：named 用 NPC 专属图，普通用通用图。</summary>
    public Sprite GetSymptomSprite(Patient p, string symptomId)
    {
        if (p == null) return null;
        var symptom = DatabaseManager.Instance.GetSymptom(symptomId);
        if (symptom == null) return null;
        if (!p.isNamed) return symptom.sprite;
        string english = symptomId.Replace("Symptom_", "");
        return Resources.Load<Sprite>($"Images/Symptoms/NPCSymptoms/Symptom_{p.npcId}_{english}");
    }
    #endregion

    #region ===== 看诊会话 =====

    /// <summary>选中一个等待中的病人开始看诊，清空上次诊断状态。</summary>
    public void StartExamination(string patientNpcId)
    {
        _currentPatient = _patients.Find(p => p.npcId == patientNpcId && p.status == PatientStatus.Waiting);
        _discoveredSymptoms.Clear(); //清空被发现的病症
        _selectedSymptoms.Clear();   //清空已经选择的病症
        _askIndex = 0;   // 重置问诊索引
    }

    public Patient CurrentPatient => _currentPatient;

    public Disease CurrentDisease =>
        _currentPatient == null ? null : DatabaseManager.Instance.GetDisease(_currentPatient.diseaseId);

    /// <summary>供 UI 显示病人列表。</summary>
    public List<Patient> GetWaitingPatients() => _patients.FindAll(p => p.status == PatientStatus.Waiting);
    public List<Patient> GetAllPatients() => new List<Patient>(_patients);

    #endregion

    #region ===== 望闻问切（发现症状） =====

    /// <summary>望：观察病人身体，揭示一个「可见」症状。</summary>
    public void Observe() => DiscoverSymptomOfType(SymptomDisplayType.Visual);

    /// <summary>问：询问病人，揭示一个「口述」症状。</summary>
    /// <summary>问：病人说出下一个口述症状（说完了从头循环）。返回说出的症状 id。</summary>
    public string Ask()
    {
        var verbal = GetVerbalSymptomIds();
        if (verbal.Count == 0) return null;

        string sid = verbal[_askIndex % verbal.Count];
        _askIndex++;

        _discoveredSymptoms.Add(sid);   // HashSet 去重，循环时不会重复添加
        return sid;
    }
    /// <summary>当前病症的所有口述症状 id（固定顺序）。</summary>
    private List<string> GetVerbalSymptomIds()
    {
        var list = new List<string>();
        var disease = CurrentDisease;
        if (disease == null) return list;
        foreach (var sid in disease.symptoms)
        {
            var s = DatabaseManager.Instance.GetSymptom(sid);
            if (s != null && s.displayType == SymptomDisplayType.Verbal)
                list.Add(sid);
        }
        return list;
    }
    private void DiscoverSymptomOfType(SymptomDisplayType type)
    {
        var disease = CurrentDisease;
        if (disease == null) return;

        // 找一个「属于当前病症 + 对应类型 + 还没发现」的症状
        foreach (var sid in disease.symptoms)
        {
            if (_discoveredSymptoms.Contains(sid)) continue;
            var symptom = DatabaseManager.Instance.GetSymptom(sid);
            if (symptom != null && symptom.displayType == type)
            {
                _discoveredSymptoms.Add(sid);
                return;   // 一次只揭示一个
            }
        }
    }

    public List<string> GetDiscoveredSymptoms() => new List<string>(_discoveredSymptoms);
    public bool IsSymptomDiscovered(string symptomId) => _discoveredSymptoms.Contains(symptomId);

    /// <summary>发现指定症状（点击病人身上的某个可见症状用）。</summary>
    public void DiscoverSymptom(string symptomId)
    {
        if (string.IsNullOrEmpty(symptomId)) return;
        _discoveredSymptoms.Add(symptomId);
    }

    #endregion

    #region ===== 症状筛选药品 =====

    /// <summary>选择/取消一个症状。</summary>
    public void SelectSymptom(string symptomId, bool selected)
    {
        if (string.IsNullOrEmpty(symptomId)) return;
        if (selected) _selectedSymptoms.Add(symptomId);
        else _selectedSymptoms.Remove(symptomId);
    }

    public List<string> GetSelectedSymptoms() => new List<string>(_selectedSymptoms);

    /// <summary>
    /// 按已选症状筛选药品：病症「包含所有选中症状」→ 该病症对应的药。
    /// 全选后通常只剩 1~2 个（普通病 + 加强版，同一种药的普通/强）。
    /// </summary>
    public List<Potion> GetMatchedPotions()
    {
        var result = new List<Potion>();
        if (_selectedSymptoms.Count == 0) return result;

        foreach (var disease in DatabaseManager.Instance.AllDiseases)
        {
            if (!DiseaseContainsAll(disease, _selectedSymptoms)) continue;
            foreach (var potion in DatabaseManager.Instance.AllPotions)
                if (potion.diseaseId == disease.id && !result.Contains(potion))
                    result.Add(potion);
        }
        return result;
    }

    /// <summary>病症是否包含所有选中症状。</summary>
    private bool DiseaseContainsAll(Disease disease, HashSet<string> selected)
    {
        foreach (var s in selected)
            if (!disease.symptoms.Contains(s))
                return false;
        return true;
    }

    #endregion

    #region ===== 治疗 =====

    /// <summary>给当前病人用药；返回是否治好。</summary>
    public TreatResult Treat(string potionId)
    {
        if (_currentPatient == null) return TreatResult.Error;
        var potion = DatabaseManager.Instance.GetPotion(potionId);
        if (potion == null) return TreatResult.Error;

        // 没药：无奖惩，病人先走，第二天再来
        if (!InventoryManager.Instance.Has(potionId))
        {
            _currentPatient.status = PatientStatus.Deferred;
            ResetDiagnosis();
            return TreatResult.NoMedicine;
        }

        InventoryManager.Instance.Remove(potionId);
        bool success = potion.diseaseId == _currentPatient.diseaseId;
        // ★ 无论成败，病人都离开等待列表（成功=治好，失败=赔钱走人）
        _currentPatient.status = PatientStatus.Treated;

        if (success) SettleSuccess(_currentPatient, potion);
        else SettleFail(_currentPatient, potion);
        ResetDiagnosis();
        return success ? TreatResult.Success : TreatResult.WrongMedicine;
    }

    private void SettleSuccess(Patient p, Potion potion)
    {
        int silver = Mathf.RoundToInt(potion.price * _successRatio);  // 收入：药价值 × 成功比例
        PlayerDataManager.Instance.AddSilver(silver);
        if (p.isNamed) SocialManager.Instance.AddSuspicion(p.npcId, -_suspicionDelta);
    }

    private void SettleFail(Patient p, Potion potion)
    {
        int silver = Mathf.RoundToInt(potion.price * _failRatio);     // 损失：药价值 × 失败比例
        PlayerDataManager.Instance.TrySpendSilver(silver);
        if (p.isNamed) SocialManager.Instance.AddSuspicion(p.npcId, _suspicionDelta);
    }

    private void ResetDiagnosis()
    {
        _currentPatient = null;
        _discoveredSymptoms.Clear();
        _selectedSymptoms.Clear();
    }

    #endregion

    #region ===== ISaveable =====
    public string SerializeState()
    {
        var data = new ClinicSaveData { clinicDayIndex = _clinicDayIndex };
        foreach (var p in _patients)
            data.patients.Add(new PatientData
            {
                npcId = p.npcId,
                portraitId = p.portraitId,
                diseaseId = p.diseaseId,
                status = (int)p.status,
                remainingDays = p.remainingDays,
                isNamed = p.isNamed
            });
        return JsonUtility.ToJson(data);
    }

    public void DeserializeState(string json, int version)
    {
        if (string.IsNullOrEmpty(json)) { ResetState(); return; }
        var dto = JsonUtility.FromJson<ClinicSaveData>(json);
        _patients.Clear();
        if (dto == null) return;

        _clinicDayIndex = dto.clinicDayIndex;
        if (dto.patients == null) return;
        foreach (var d in dto.patients)
        {
            bool named = d.isNamed || !string.IsNullOrEmpty(d.npcId);
            string portrait = string.IsNullOrEmpty(d.portraitId)
                ? (named ? d.npcId : "Man_0")
                : d.portraitId;
            _patients.Add(new Patient
            {
                npcId = d.npcId,
                portraitId = d.portraitId,
                diseaseId = d.diseaseId,
                status = (PatientStatus)d.status,
                remainingDays = d.remainingDays,
                isNamed = d.isNamed
            });
        }
    }

    public void ResetState()
    {
        _patients.Clear();
        _clinicDayIndex = -1;
        _currentPatient = null;
        _discoveredSymptoms.Clear();
        _selectedSymptoms.Clear();
    }
    #endregion
}