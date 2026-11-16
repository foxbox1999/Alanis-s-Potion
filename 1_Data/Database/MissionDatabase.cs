using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/任务数据库")]
public class MissionDatabase : ScriptableObject
{
    public List<Mission> missions;
}