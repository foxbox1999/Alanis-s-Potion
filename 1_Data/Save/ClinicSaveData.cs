using System;
using System.Collections.Generic;

[Serializable]
public class PatientData
{
    public string npcId;          // 有名字 NPC 的 id（普通病人为空）
    public string portraitId;     // 立绘 id：named= npcId，普通= "Man_3"
    public string diseaseId;      // 病症 id
    public int status;            // PatientStatus 枚举值
    public int remainingDays;     // 剩余可等待天数（3 天消失）
    public bool isNamed;        // 是否特殊病人（成败影响怀疑度）
}

/// <summary>
/// 病人存档
/// </summary>
[Serializable]
public class ClinicSaveData
{
    public int clinicDayIndex;    // 上次生成病人的游戏日（防同一天重复生成）
    public List<PatientData> patients = new();
}