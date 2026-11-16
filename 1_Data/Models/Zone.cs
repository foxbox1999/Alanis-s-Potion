using System;
using UnityEngine;

/// <summary>采集地区。</summary>
[Serializable]
public class Zone
{
    public string id;
    public string name;
    public Sprite icon;
    [TextArea] public string description;
}