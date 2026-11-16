using System;
using System.Collections.Generic;

[Serializable]
public class ChildSaveData
{
    public string name;
    public int magic;         // 魔力
    public int stamina;       // 体力
    public int commonSense;   // 常识
    public int alchemy;       // 魔药水平
    public int taskDayIndex;                 // 上次安排任务的游戏日
    public List<string> assignedTaskIds = new();  // 当天安排的任务 id
}