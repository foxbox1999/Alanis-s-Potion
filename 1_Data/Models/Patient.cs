using System;

[Serializable]
public class Patient
{
    public string npcId;        // 有名字 NPC id（普通为空）
    public string portraitId;   // 立绘 id
    public string diseaseId;
    public PatientStatus status;
    public int remainingDays;
    public bool isNamed;
}