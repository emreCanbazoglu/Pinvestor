using UnityEngine;

public static class LogHelper
{
    public static void LogVector(string label, Vector3 v, int decimals = 6)
    {
        string format = "F" + decimals;
        Debug.Log($"{label} = ({v.x.ToString(format)}, {v.y.ToString(format)}, {v.z.ToString(format)})");
    }
}
