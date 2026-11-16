using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "数据库/甜品数据库")]
public class DessertDatabase : ScriptableObject
{
    public List<Dessert> desserts;
}