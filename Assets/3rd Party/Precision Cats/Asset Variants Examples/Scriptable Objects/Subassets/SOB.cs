#if UNITY_2018_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Asset Variants Examples/SOB")]
public class SOB : SOAB
{
    [Space(30)]
    public string text2;
    public float number2;
    public ClassA[] array2 = new ClassA[2];
    public int count2;
    public ClassA classA2;

    [System.Serializable]
    public class ClassA
    {
        public string text2;
        public float number2;
        public int count2;
    }
}
#endif
