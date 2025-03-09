using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Asset Variants Examples/EmulationSO")]
public class EmulationSO : ScriptableObject
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void Init()
    {
        var em = new AssetVariants.StringHashSetEmulationProperty("hashSetEmulation", ";");
        AssetVariants.PropertyFilter.AddEmulationProperty(typeof(EmulationSO), em, true);

        var em2 = new AssetVariants.DictionaryEmulationProperty("dictionaryEmulation", "original");
        AssetVariants.PropertyFilter.AddEmulationProperty(typeof(EmulationSO), em2, true);

        var em3 = new AssetVariants.HashSetEmulationProperty("objectHashSetEmulation");
        AssetVariants.PropertyFilter.AddEmulationProperty(typeof(EmulationSO), em3, true);
    }
#endif

    [TextArea(10, 100)]
    public string hashSetEmulation; //TODO:rename

    [Space(10)]
    public List<Pair> dictionaryEmulation = new List<Pair>();

    [Space(10)]
    public List<Object> objectHashSetEmulation = new List<Object>();

    [System.Serializable]
    public struct Pair
    {
        public string original;
        public string replaced;
    }
}
