using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/NPC数据库")]
public class NpcDatabase : ScriptableObject
{
    public List<Npc> npcs;
}