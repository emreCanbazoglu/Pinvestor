#if ODIN_INSPECTOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Sirenix;
using Sirenix.Serialization;

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/AssetListExample")]
public class AssetListExample : ScriptableObject
{
    [AssetList]
    [PreviewField(70, ObjectFieldAlignment.Center)]
    public Texture2D SingleObject;

    [AssetList(Path = "/Plugins/Sirenix/")]
    public List<ScriptableObject> AssetList;

    //public SubClass subClass;
    //[Serializable]
    //public class SubClass
    //{
    //    [AssetList(Path = "/Plugins/Sirenix/")]
    //    public List<ScriptableObject> AssetList;
    //}

    [FoldoutGroup("Filtered Odin ScriptableObjects", expanded: false)]
    [AssetList(Path = "Plugins/Sirenix/")]
    public ScriptableObject Object;

    [AssetList(AutoPopulate = true, Path = "Plugins/Sirenix/")]
    [FoldoutGroup("Filtered Odin ScriptableObjects", expanded: false)]
    public List<ScriptableObject> AutoPopulatedWhenInspected;

    [AssetList(LayerNames = "MyLayerName")]
    [FoldoutGroup("Filtered AssetLists examples")]
    public GameObject[] AllPrefabsWithLayerName;

    [AssetList(AssetNamePrefix = "Rock")]
    [FoldoutGroup("Filtered AssetLists examples")]
    public List<GameObject> PrefabsStartingWithRock;

    [FoldoutGroup("Filtered AssetLists examples")]
    [AssetList(Tags = "MyTagA, MyTabB", Path = "/Plugins/Sirenix/")]
    public List<GameObject> GameObjectsWithTag;

    [FoldoutGroup("Filtered AssetLists examples")]
    [AssetList(CustomFilterMethod = "HasRigidbodyComponent")]
    public List<GameObject> MyRigidbodyPrefabs;

    private bool HasRigidbodyComponent(GameObject obj)
    {
        return obj.GetComponent<Rigidbody>() != null;
    }
}
#endif
