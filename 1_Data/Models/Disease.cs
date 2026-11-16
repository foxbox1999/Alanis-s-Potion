using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>病症（症状 → 病症 → 对症药）。</summary>
[Serializable]
public class Disease
{
    public string id;
    public string name;
    [TextArea] public string description;

    /// <summary>包含的症状 id 列表（导入时会从 JSON 里的症状「名字」自动转成 id）。</summary>
    public List<string> symptoms;
}