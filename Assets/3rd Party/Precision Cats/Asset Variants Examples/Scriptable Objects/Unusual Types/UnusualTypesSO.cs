//#define UNSAFE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Asset Variants Examples/UnusualTypesSO")]
public class UnusualTypesSO : ScriptableObject
{
    [Space(10)]
    //public ExposedReference<GameObject> gameObjectExposedReference;

#if UNSAFE
    [Space(10)]
    public UnsafeStruct unsafeStruct;
    public UnsafeStruct[] unsafeStructs;

    [Serializable]
    public unsafe struct UnsafeStruct
    {
        public fixed float fixedFloatArray[8];
    }
#endif

    [Space(10)]
    public Hash128 hash128;
    [Header("@long")]
    public long @long;
}
