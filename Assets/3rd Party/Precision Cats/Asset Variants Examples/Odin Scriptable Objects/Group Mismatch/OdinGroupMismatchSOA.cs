#if ODIN_INSPECTOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Sirenix;
using Sirenix.Serialization;

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/OdinGroupMismatchSO A")]
public class OdinGroupMismatchSOA : OdinGroupMismatchSOAB
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void Init()
    {
        AssetVariants.AVHeader.parentFilterTypes[typeof(OdinGroupMismatchSOA)] = typeof(OdinGroupMismatchSOAB);
    }
#endif

    [OdinSerialize]
    public Class odinSerialized = new Class();

    public class Class
    {
        public string a;
        public string b;
        public string c;
        public string d;
        public string e;
        public string f;
        public string g;
        public string h;
    }
}
#endif
