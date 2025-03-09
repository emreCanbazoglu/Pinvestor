#if ODIN_INSPECTOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Sirenix;
using Sirenix.Serialization;

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/OdinScriptableObject")]
[TypeInfoBox("Type Info Box")]
public class ExampleOdinScriptableObject : SerializedScriptableObject
{
    //private void OnValidate()
    //{
    //    if (objectToString.Count == 0)
    //    {
    //        objectToString[UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("New Vector 26")[0]))] = "Okie 3";
    //        objectToString[UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("Custom Shader")[0]))] = "Okie";
    //    }
    //    if (floatToString.Count == 0)
    //    {
    //        floatToString[3.14f] = "15";
    //        floatToString[1234] = "5678";
    //        floatToString[UnityEngine.Random.value] = "Random";
    //    }
    //    if (charToString2.Count == 0)
    //    {
    //        charToString2['f'] = "F";
    //        charToString2[(char)UnityEngine.Random.Range(0, 256)] = "Random";
    //    }
    //    if (intToString2.Count == 0)
    //    {
    //        intToString2[212] = "212";
    //        intToString2[UnityEngine.Random.Range(int.MinValue, int.MaxValue)] = "Random";
    //    }
    //}
    //public Dictionary<UnityEngine.Object, string> objectToString = new Dictionary<UnityEngine.Object, string>();
    //public Dictionary<float, string> floatToString = new Dictionary<float, string>();
    //public Dictionary<char, string> charToString2 = new Dictionary<char, string>();
    //public Dictionary<int, string> intToString2 = new Dictionary<int, string>();

    [Space(20)]
    [CustomContextMenu("My custom option", "MyAction")]
    [PropertyOrder(-100)]
    public Vector3 MyVectorWithContextMenu;

    private void MyAction()
    {
        MyVectorWithContextMenu = UnityEngine.Random.onUnitSphere;
        //TODO:::
        //#if UNITY_EDITOR
        //        AssetVariants.AVUtility.OnValuesChanged(this, "MyVectorWithContextMenu");
        //#endif
    }


    [Space(20)]
    public Rect unitySerializedRect;
    [OdinSerialize]
    [NonSerialized]
    public Rect odinSerializedRect;
    public RectInt rectInt;
    //public Vector2Int vector2Int;
    //public Vector3Int vector3Int;

    [Space(20)]
    public Stack<string> stringStack = new Stack<string>();
    public Queue<int> intQueue = new Queue<int>();

    [Space(20)]
    [TabGroup("TabGroup", "Tab 1")]
    [FilePath(Extensions = ".unity")]
    public string ScenePath;

    [TabGroup("TabGroup", "Tab 2")]
    [ShowInInspector]
    [Tooltip("This doesn't have an override button because the SerializationBackend is None, because it's private; it's just shown in the inspector, not serialized.")]
    private bool privateShowInInspector;

    [FoldoutGroup("Foldout Group")]
    [Button("Randomize displayAsStringColor")]
    private void RandomizeDisplayAsStringColor()
    {
        displayAsStringColor = UnityEngine.Random.ColorHSV();
        //TODO:::
        //#if UNITY_EDITOR
        //        AssetVariants.AVUtility.OnValuesChanged(this, "displayAsStringColor");
        //#endif
    }

    [FoldoutGroup("Foldout Group")]
    [DisplayAsString]
    public Color displayAsStringColor;

    public Dictionary<string, string> stringStringDictionary = new Dictionary<string, string>();
    public Dictionary<string, int> stringIntDictionary = new Dictionary<string, int>();

    public string[][] stringArrayArray = new string[2][];

    [HorizontalGroup("Horizontal Group", MinWidth = 200)]
    [TableMatrix]
    public string[,] string2DArray = new string[3, 2];
    //public string[,,] string3DArray = new string[3, 2, 4];

    public HashSet<string> stringHashSet = new HashSet<string>();
    [TabGroup("TabGroup", "Tab 2")]
    public HashSet<float> floatHashSet = new HashSet<float>();

    public readonly List<string> readonlyList = new List<string>();

    [PropertySpace(20)]
    [FoldoutGroup("Properties")]
    [ShowInInspector]
    public float ShowInInspectorProperty { get; set; }
    [FoldoutGroup("Properties")]
    [SerializeField]
    public float SerializeFieldProperty { get; set; }
    [FoldoutGroup("Properties")]
    [OdinSerialize]
    public float OdinSerializeProperty { get; set; }

    [HorizontalGroup("Horizontal Group")]
    [SuffixLabel("suffix")]
    public RegularClass regularClass;
    [OdinSerialize]
    private RegularClass odinSerializedRegularClass = new RegularClass();
    [Serializable]
    public class RegularClass
    {
        [BoxGroup("BoxGroup A")]
        public float boxGroupAFloat;

        [OdinSerialize]
        public string odinSerializedString = "text";

        [OdinSerialize]
        private string privateOdinSerializedString = "secret";

        public float regularFloat = 2;

        public HashSet<int> intHashSet = new HashSet<int>() { 3, 65 };

        [BoxGroup("BoxGroup Centered", centerLabel: true)]
        [HideLabel]
        public string hideLabel = "My label is hidden";

        [BoxGroup("BoxGroup A")]
        public string boxGroupAString = "Some text";

        [BoxGroup("BoxGroup Centered")]
        public string stuff = "Stuff";
    }

    [Space(20)]

    [InfoBox("Info Box")]
    [PropertyOrder(-5)]
    public HashSet<HashSetChild> classHashSet = new HashSet<HashSetChild>();

    [Serializable]
    public class HashSetChild
    {
        public string text = "abc";
        public bool boolean;
    }

    [Space(30)]
    [PreviewField, Required, AssetsOnly, PropertyOrder(6)]
    public GameObject Prefab;
    [OdinSerialize, HideLabel, Required, PropertyOrder(7)]
    public string NameProperty { get; set; }
    [Button(ButtonSizes.Medium), PropertyOrder(8)]
    public void RandomName()
    {
        NameProperty = Guid.NewGuid().ToString();
        //TODO:::
        //#if UNITY_EDITOR
        //        AssetVariants.AVUtility.OnValuesChanged(this, "NameProperty");
        //#endif
    }

    [Space(30)]
    [HorizontalGroup("Split", Width = 50), HideLabel, PreviewField(50)]
    public Texture2D Icon;
    [Space(30)]
    [VerticalGroup("Split/Properties")]
    public string MinionName;
    [VerticalGroup("Split/Properties")]
    public float Health;
    [VerticalGroup("Split/Properties")]
    public float Damage;

    [Space(10)]
    [ValidateInput("IsValid")]
    [SuffixLabel("kg")]
    public int GreaterThanZero;
    private bool IsValid(int value)
    {
        return value > 0;
    }
    [InfoBox("This message is only shown when MyInt is even", "IsEven")]
    [SuffixLabel("cm")]
    public int MyInt;
    private bool IsEven()
    {
        return this.MyInt % 2 == 0;
    }

    [DelayedProperty]
    public string delayedPropertyString = "txt";

    [EnumPaging]
    public SomeEnum SomeEnumField;
    public enum SomeEnum { A, B, C }


    [Space(30)]
    [Title("Hidden Object Pickers")]
    [HideReferenceObjectPicker]
    public MyCustomReferenceType OdinSerializedProperty1 = new MyCustomReferenceType();
    [HideReferenceObjectPicker]
    public MyCustomReferenceType OdinSerializedProperty2Null = null;
    [Title("Shown Object Pickers")]
    public MyCustomReferenceType OdinSerializedProperty3 = new MyCustomReferenceType();
    public MyCustomReferenceType OdinSerializedProperty4 = new MyCustomReferenceType();

    // Protip: You can also put the HideInInspector attribute on the class definition itself to hide it globally for all members.
    // [HideReferenceObjectPicker]
    public class MyCustomReferenceType
    {
        public int A;
        public int B;
        public int C;
    }


    //[Space(30)]
    //[Multiline(10)]
    //public string UnityMultilineField = "";

    [Title("Wide Multiline Text Field", bold: false)]
    [HideLabel]
    [MultiLineProperty(5)]
    public string WideMultilineTextField = "";

    //[InfoBox("Odin supports properties, but Unity's own Multiline attribute only works on fields.")]
    //[ShowInInspector]
    //[MultiLineProperty(10)]
    //public string OdinMultilineProperty { get; set; }





    //This seems to be broken in Odin itself, probably:

    //[Space(30)]
    //[OnStateUpdate("@#(#Tabs).State.Set<int>(\"CurrentTabIndex\", $value + 1)")]
    //[PropertyRange(1, "@#(#Tabs).State.Get<int>(\"TabCount\")")]
    //public int selectedTab = 1;
    //[TabGroup("Tabs", "Tab 1")]
    //public string exampleString1;
    //[TabGroup("Tabs", "Tab 2")]
    //public string exampleString2;
    //[TabGroup("Tabs", "Tab 3")]
    //public string exampleString3;


    [OdinSerialize]
    public Class1 class1 = new Class1();
    public class Class1
    {
        public Class2 class2 = new Class2();
        public class Class2
        {
            public Class3 class3 = new Class3();
            public class Class3
            {
                public Class4 class4 = new Class4();
                public class Class4
                {
                    public string text;
                }
            }
        }
    }
}
#endif
