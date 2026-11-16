using System;
using UnityEngine;

/// <summary>NPC 数据（镇民 / 巫师 / 教会）。</summary>
[Serializable]
public class Npc
{
    public string id;
    public string name;
    public Sprite sprite;
    /// <summary>阵营：JSON 的 "Villager"→Townsfolk、"Mage"→Witch、"Church"→Church。</summary>
    public FactionType category;

    public string preference;              // 礼物偏好
    public string personality;             // 性格
    [TextArea] public string description;  // 背景介绍
}