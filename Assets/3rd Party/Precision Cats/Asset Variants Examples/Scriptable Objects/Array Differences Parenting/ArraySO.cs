using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
// This forces it to use IMGUI, because UI Toolkit is not optimized for large arrays (I believe due to the .FindProperty when binding)
using UnityEditor;
[CustomEditor(typeof(ArraySO))]
[CanEditMultipleObjects]
public class ArraySOEditor : Editor { }
#endif

[CreateAssetMenu(menuName = "Asset Variants Examples/ArraySO")]
public class ArraySO : ScriptableObject
{
    [TextArea(20, 30)]
    public string text;

    [ButtonBool]
    public bool randomize;

    //[NonReorderable]
    public float[] array;

    //[TextArea(100, 100)]
    //public string text2;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (randomize)
        {
            randomize = false;

            for (int i = 0; i < array.Length; i++)
                array[i] = Random.value;
        }
    }
#endif
}
