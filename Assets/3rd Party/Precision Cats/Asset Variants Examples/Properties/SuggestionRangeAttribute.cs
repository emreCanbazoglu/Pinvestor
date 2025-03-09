using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SuggestionRangeAttribute : PropertyAttribute
{
    public float min, max;
    public float absoluteMin, absoluteMax;

    public SuggestionRangeAttribute(float min, float max, float absoluteMin = -Mathf.Infinity, float absoluteMax = Mathf.Infinity)
    {
        this.min = min;
        this.max = max;
        this.absoluteMin = absoluteMin;
        this.absoluteMax = absoluteMax;
    }
}