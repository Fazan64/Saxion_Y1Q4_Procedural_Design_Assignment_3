using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[Serializable]
public struct UvRange
{
    public Rect rect;
    public int numRepeatsHorizontal;
    public int numRepeatsVertical;
    
    public RangeFloat horizontal => new RangeFloat(rect.xMin, rect.xMax);
    public RangeFloat vertical   => new RangeFloat(rect.yMin, rect.yMax);

    public UvRange(Rect rect, int numRepeatsHorizontal = 1, int numRepeatsVertical = 1)
    {
        this.rect = rect;
        this.numRepeatsHorizontal = numRepeatsHorizontal;
        this.numRepeatsVertical   = numRepeatsVertical;
    }
}