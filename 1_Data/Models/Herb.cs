using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Herb : Item
{
    [TextArea] public string effect;
    public List<string> zoneIds;    // 出现区域 id
    public int minQuantity;
    public int maxQuantity;
}