using System;
using UnityEngine;

public static class GameObjectExtensions
{
    public static T RequireComponent<T>(this GameObject gameObject)
    {
        var result = gameObject.GetComponent<T>();
        if (result == null) { throw new InvalidOperationException("Failed to get component " + typeof(T).Name + " on game object " + gameObject); }
        return result;
    }
}
