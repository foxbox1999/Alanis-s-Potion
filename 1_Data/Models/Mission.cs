using System;
using UnityEngine;

/// <summary>养子每日任务：上课（提升属性）/ 野外探索（去采集区域）。</summary>
[Serializable]
public class Mission
{
    public string id;
    public string name;

    /// <summary>任务类型：Course（上课）/ Expedition（野外探索）。</summary>
    public ChildTaskType type;

    /// <summary>上课时提升的属性（Expedition 时忽略）。</summary>
    public ChildStatType stat;

    /// <summary>上课时提升量（Expedition 时忽略）。</summary>
    public int statGain;

    /// <summary>野外探索时目标地区 id（Course 时为空）。</summary>
    public string zoneId;

    public Sprite icon;
}