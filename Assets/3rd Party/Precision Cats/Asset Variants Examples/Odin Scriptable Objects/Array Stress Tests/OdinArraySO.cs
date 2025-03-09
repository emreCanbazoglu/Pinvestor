#if ODIN_INSPECTOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/OdinArraySO")]
public class OdinArraySO : SerializedScriptableObject
{
    [TextArea(10, 30)]
    public string text;

    [ButtonBool]
    public bool randomize;

    [System.NonSerialized]
    [OdinSerialize]
    public float[] array = new float[10000];

    [TextArea(100, 100)]
    public string text2;

    private void OnValidate()
    {
        if (randomize)
        {
            randomize = false;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = UnityEngine.Random.value;
            }
        }
    }
}
#endif
