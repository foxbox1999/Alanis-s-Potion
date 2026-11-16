using System;
using System.Collections.Generic;
using UnityEngine;
using static Potion;

[Serializable]
public class Potion : Item
{
    public string diseaseId;        // 对症病症 id
    public Recipe recipe;

    [Serializable]
    public class Recipe
    {
        public float timeRequired;
        public List<Ingredient> ingredients;

    }

    [Serializable]
    public class Ingredient
    {
        public string itemId;       // ★ 草药 或 基础药品 的 id
        public int quantity;
        public override string ToString()
        {
            return $"{itemId} x{quantity}";
        }
    }

    public override string ToString()
    {
        return $"Potion: {name}, Recipe: {recipe.ingredients} ";
    }
}