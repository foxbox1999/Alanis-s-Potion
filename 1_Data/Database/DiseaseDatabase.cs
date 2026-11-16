using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/疾病数据库")]
public class DiseaseDatabase : ScriptableObject
{
    public List<Disease> diseases;
}