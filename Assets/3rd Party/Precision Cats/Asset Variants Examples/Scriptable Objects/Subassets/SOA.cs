#if UNITY_2018_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SOAB : ScriptableObject
{
    [ButtonBool]
    public bool deleteChildren;
    [Space(10)]
    public string createChildType;
    [ButtonBool]
    public bool createChild;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (deleteChildren)
        {
            deleteChildren = false;

            EditorApplication.delayCall += () =>
            {
                var path = AssetDatabase.GetAssetPath(this);
                var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] != mainAsset)
                        AssetDatabase.RemoveObjectFromAsset(assets[i]);
                }
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path);
            };
        }

        if (createChild)
        {
            createChild = false;

            EditorApplication.delayCall += () =>
            {
                var child = CreateInstance(createChildType);
                var path = AssetDatabase.GetAssetPath(this);
                child.name = createChildType + " " + AssetDatabase.LoadAllAssetsAtPath(path).Length;
                AssetDatabase.AddObjectToAsset(child, this);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path);
            };
        }
    }
#endif
}

[CreateAssetMenu(menuName = "Asset Variants Examples/SOA")]
public class SOA : SOAB
{
    [Space(30)]
    public string text;
    public float number;
    public ClassA[] array = new ClassA[2];
    public int count;
    public ClassA classA;

    public class ClassA
    {
        public string text;
        public float number;
        public int count;
    }
}
#endif
