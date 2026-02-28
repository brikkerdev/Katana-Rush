using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class ComponentPrefabSelectorAttribute : PropertyAttribute
{
    public Type ComponentType { get; private set; }

    public ComponentPrefabSelectorAttribute()
    {
        ComponentType = null;
    }

    public ComponentPrefabSelectorAttribute(Type componentType)
    {
        ComponentType = componentType;
    }
}