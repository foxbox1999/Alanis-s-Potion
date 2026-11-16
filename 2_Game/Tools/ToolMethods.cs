
using UnityEngine;

public static class ToolMethods 
{
    public static T GetComponentInStrictChildren<T>(GameObject Parent) where T : Component
    {
        T[] components = Parent.GetComponentsInChildren<T>();
        foreach (T comp in components)
        {
            if (comp.gameObject != Parent) // 跳过自身
                return comp;
        }
        return null;
    }
}
