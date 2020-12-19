using System;
using UnityEngine;
using static UnityEngine.Mathf;
using System.Linq;

public static class FunctionLibrary
{
    public delegate Vector3 Function(float x, float z, float t);

    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }
    public static Function GetFunction(FunctionName name)
    {
        switch (name)
        {
            case FunctionName.Wave: return Wave;
            case FunctionName.MultiWave: return MultiWave;
            case FunctionName.Ripple: return Ripple;
            case FunctionName.Sphere: return Sphere;
            case FunctionName.Torus: return Torus;
            default: throw new ArgumentException($"Unknown function name {name}", nameof(name));
        }
    }
    public static FunctionName GetNext(FunctionName prevName)
    {
        var values = Enum.GetValues(typeof(FunctionName)).Cast<FunctionName>().ToArray();
        var currentIndex = Array.IndexOf(values, prevName);
        if (currentIndex < 0)
        {
            throw new ArgumentException($"Unknown function name {prevName}", nameof(prevName));
        }
        return values[(currentIndex + 1) % values.Length];
    }

    public static Vector3 Wave(float x, float z, float t) => new Vector3(x, Sin(PI * (x + z + t)), z);

    public static Vector3 MultiWave(float x, float z, float t)
    {
        float y = Sin(PI * (x + 0.5f * t));
        y += 0.5f * Sin(2f * PI * (z + t));
        y += Sin(PI * (x + z + 0.25f * t));
        return new Vector3(x, y * (1f / 2.5f), z);
    }

    public static Vector3 Ripple(float x, float z, float t)
    {
        float d = Sqrt(x * x + z * z);
        float y = Sin(PI * (4f * d - t));
        return new Vector3(x, y / (1f + 10f * d), z);
    }

    public static Vector3 Sphere(float u, float v, float t)
    {
        float r = 0.5f + 0.5f * Sin(PI * t);
        float s = r * Cos(0.5f * PI * v);
        return new Vector3(
            s * Sin(PI * u + t),
            r * Sin(PI * 0.5f * v),
            s * Cos(PI * u + t));
    }

    public static Vector3 Torus(float u, float v, float t)
    {
        float r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
        float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
        float s = r1 + r2 * Cos(PI * v);
        Vector3 p;
        p.x = s * Sin(PI * u);
        p.y = r2 * Sin(PI * v);
        p.z = s * Cos(PI * u);
        return p;
    }
}