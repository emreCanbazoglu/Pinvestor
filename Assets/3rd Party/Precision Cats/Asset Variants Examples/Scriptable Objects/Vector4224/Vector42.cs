using UnityEngine;
using System;

public class Vector4224 : ScriptableObject
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void Init()
    {
        AssetVariants.AVHeader.parentFilterTypes[typeof(Vector42)] = typeof(Vector4224);
        AssetVariants.AVHeader.parentFilterTypes[typeof(Vector24)] = typeof(Vector4224);
    }
#endif
}

[CreateAssetMenu(menuName = "Asset Variants Examples/Vector42")]
public class Vector42 : Vector4224
{
    public Vector4 a;
    public Vector2 b;
}
