using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/地区数据库")]
public class ZoneDatabase : ScriptableObject
{
    public List<Zone> zones;
}