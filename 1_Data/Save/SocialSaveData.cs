using System;
using System.Collections.Generic;

/// <summary>单个 NPC 的社交值：巫师=好感度，镇民/教会=怀疑度（含义由 NPC 阵营决定）。</summary>
[Serializable]
public class NpcSocialEntry
{
    public string npcId;
    public int value;
}

/// <summary>
/// npc的社交值存档
/// </summary>
[Serializable]
public class SocialSaveData
{
    public List<NpcSocialEntry> npcs = new();
}