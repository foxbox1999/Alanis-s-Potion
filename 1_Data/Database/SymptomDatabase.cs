using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/症状数据库")]
public class SymptomDatabase : ScriptableObject
{
    public List<Symptom> symptoms;
}