#if ODIN_INSPECTOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Sirenix;
using Sirenix.Serialization;

public class OdinGroupMismatchSOAB : SerializedScriptableObject { }

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/OdinGroupMismatchSO B")]
public class OdinGroupMismatchSOB : OdinGroupMismatchSOAB
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void Init()
    {
        AssetVariants.AVHeader.parentFilterTypes[typeof(OdinGroupMismatchSOB)] = typeof(OdinGroupMismatchSOAB);
    }
#endif

    [OdinSerialize]
    public Class odinSerialized = new Class();

    public class Class
    {
        [BoxGroup("Box1")]
        [TabGroup("Tabs1", "Tab1")]
        public string a;
        [BoxGroup("Box3")]
        [BoxGroup("Box4")]
        [BoxGroup("Box5")]
        [BoxGroup("Box6")]
        [BoxGroup("Box7")]
        [BoxGroup("Box7/Box8")]
        [BoxGroup("Box7/Box8/Box9")]
        [BoxGroup("Box7/Box8/Box9/Box10")]
        [BoxGroup("Box7/Box8/Box9/Box10/Box11")]
        public string b;
        [BoxGroup("Box1")]
        [TabGroup("Tabs1", "Tab2")]
        public string c;
        [TabGroup("Tabs2", "Tab2")]
        public string d;
        [BoxGroup("Box3")]
        public string e;
        [BoxGroup("Box1")]
        [TabGroup("Tabs1", "Tab2")]
        public string f;
        [BoxGroup("Box3")]
        public string g;
        [TabGroup("Tabs2", "Tab1")]
        [BoxGroup("Box2")]
        public string h;
    }
}
#endif
