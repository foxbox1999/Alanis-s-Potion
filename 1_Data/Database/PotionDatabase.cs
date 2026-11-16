using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/药品数据库")]
public class PotionDatabase : ScriptableObject
{
    public List<Potion> potions;
}