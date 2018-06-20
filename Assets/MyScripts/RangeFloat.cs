using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct RangeFloat
{
    public float min;
    public float max;

    public float range => max - min;

    public RangeFloat(float min, float max)
    {
        if (min > max)
        {
            this.min = max;
            this.max = min;
            return;
        }
        
        this.min = min;
        this.max = max;
    }

    public bool Contains(float value, bool inclusiveMin = false, bool inclusiveMax = false)
    {
        if (!(inclusiveMin ? value >= min : value > min)) return false;
        if (!(inclusiveMax ? value <= max : value < max)) return false;

        return true;
    }

    public float Lerp(float t)
    {
        return Mathf.LerpUnclamped(min, max, t);
    }

    public float InverseLerp(float value)
    {
        return Mathf.InverseLerp(min, max, value);
    }
}

public static class RangeHelper
{
    // WIP
    public static float[] GetIntersectionsT(float start, float interval, float min, float max)
    {
        if (Mathf.Approximately(min, max))
        {
            return new float[0];
        }
        
        if (min > max)
        {
            float temp = min;
            min = max;
            max = temp;
        }
        
        float offset = Mathf.Floor((min - start) / interval) * interval;
        float range = max - min;

        var result = new List<float>();

        for (int i = 1;; ++i)
        {
            float point = start + offset + i * interval / range;
            if (point <= min || point >= max) return result.ToArray();

            result.Add(InverseLerpUnclamped(min, max, point));
        }
    }
    
    public static float InverseLerpUnclamped(float a, float b, float value)
    {
        if (a != b)
            return (float)(((double)value - (double)a) / ((double)b - (double)a));
        return 0.0f;
    }
}