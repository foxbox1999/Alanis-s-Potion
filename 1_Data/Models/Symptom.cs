using System;
using UnityEngine;

/// <summary>症状（病人表现出来的一条线索）。</summary>
[Serializable]
public class Symptom
{
    public string id;
    public string name;

    public Sprite sprite;
    [TextArea] public string description;

    /// <summary>呈现方式：口述 / 可见。</summary>
    public SymptomDisplayType displayType;
}