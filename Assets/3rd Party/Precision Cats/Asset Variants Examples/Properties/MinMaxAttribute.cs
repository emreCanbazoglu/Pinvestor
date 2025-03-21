//https://gist.github.com/LotteMakesStuff/0de9be35044bab97cbe79b9ced695585

// NOTE DONT put in an editor folder

using UnityEngine;

public sealed class MinMaxAttribute : PropertyAttribute
{
    public float MinLimit = 0;
    public float MaxLimit = 1;
    public bool ShowEditRange;
    public bool ShowDebugValues;

    public MinMaxAttribute(int min, int max)
    {
        MinLimit = min;
        MaxLimit = max;
    }
}
