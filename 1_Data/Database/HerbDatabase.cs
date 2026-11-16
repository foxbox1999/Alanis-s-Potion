using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/草药数据库")]
public class HerbDatabase : ScriptableObject
{
    public List<Herb> herbs;
}