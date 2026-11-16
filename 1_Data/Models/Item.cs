using System;
using UnityEngine;

[Serializable]
public class Item
{
    public string id;
    public string name;
    public Sprite icon;
    public int price;
    public ItemCategory category;
    [TextArea] public string description;
}