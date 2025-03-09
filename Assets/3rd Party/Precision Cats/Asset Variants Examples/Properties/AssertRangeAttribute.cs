using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class AssertRangeAttribute : PropertyAttribute
{
    public readonly float floatMin, floatMax;
    public readonly int intMin, intMax;

    public AssertRangeAttribute(float min, float max)
    {
        intMin = Mathf.RoundToInt(floatMin = min);
        intMax = Mathf.RoundToInt(floatMax = max);
    }

    public AssertRangeAttribute(int min, int max)
    {
        floatMin = intMin = min;
        floatMax = intMax = max;
    }
}