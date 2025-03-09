#if ODIN_INSPECTOR
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/OdinBugArray2DSO")]
public class OdinBugArray2DSO : SerializedScriptableObject
{
    [InfoBox("Undo seems broken in Odin for 2D arrays")]
    public string[,] string2DArray = new string[3, 2];
    [Space(10)]
    public float[,] float2DArray = new float[3, 2];

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void Init()
    {
        var bl = AssetVariants.AVFilter.typeFilter.blacklist;
        bl.Add(typeof(OdinBugArray2DSO));
    }
#endif
}
#endif
