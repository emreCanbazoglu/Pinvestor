using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.Serialization;
//using Unity.Mathematics;


#region Potions
#if UNITY_EDITOR
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

[CustomPropertyDrawer(typeof(Ingredient))]
public class IngredientDrawer : PropertyDrawer
{
    // IMGUI
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var amountRect = new Rect(position.x, position.y, 30, position.height);
        var unitRect = new Rect(position.x + 35, position.y, 50, position.height);
        var nameRect = new Rect(position.x + 90, position.y, position.width - 90, position.height);

        // Draw fields - pass GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("amount"), GUIContent.none);
        EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("unit"), GUIContent.none);
        EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    // UI Toolkit
    //public override VisualElement CreatePropertyGUI(SerializedProperty property)
    //{
    //    // Create property container element.
    //    var container = new VisualElement();

    //    // Create property fields.
    //    var amountField = new PropertyField(property.FindPropertyRelative("amount"));
    //    var unitField = new PropertyField(property.FindPropertyRelative("unit"));
    //    var nameField = new PropertyField(property.FindPropertyRelative("name"), "Fancy Name");

    //    // Add fields to the container.
    //    container.Add(amountField);
    //    container.Add(unitField);
    //    container.Add(nameField);

    //    return container;
    //}
}
#endif

public enum IngredientUnit { Spoon, Cup, Bowl, Piece }

[Serializable]
public class Ingredient
{
    public string name;
    public int amount = 1;
    public IngredientUnit unit;
}
#endregion


#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DrawLabels))]
public class DrawLabelsDrawer : InlinePropertyDrawer
{
    protected override bool DrawLabels => true;

    protected override bool Indent => false;
}

[CustomPropertyDrawer(typeof(DontDrawLabels))]
public class DontDrawLabelsDrawer : InlinePropertyDrawer
{
    protected override bool DrawLabels => false;
}
#endif

[Serializable]
public class DrawLabels
{
    public float a, b, c;
    public Vector2 vector2;
    public bool boolean;
}

[Serializable]
public class DontDrawLabels
{
    public float a, b, c;
    public Vector2 vector2;
    public bool boolean;
}


#if UNITY_EDITOR && false
[CanEditMultipleObjects]
[CustomEditor(typeof(ExampleScriptableObject))]
public class ExampleScriptableObjectEditor : Editor
{
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    private void OnSceneGUI(SceneView sceneView)
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        Handles.TransformHandle(ref pos, ref rot);

        //TODO: I know two Editors get created normally, but how have people been dealing with the duplicate duringSceneGui calls?

        //Handles.BeginGUI();
        //GUILayout.Button("OnSceneGUI Test Button");
        //Handles.EndGUI();
    }
}
#endif


[CreateAssetMenu(menuName = "Asset Variants Examples/ExampleScriptableObject")]
public class ExampleScriptableObject : ScriptableObject
{
    public void TestMethod1()
    {
        Debug.Log("TestMethod1() on \"" + name + "\"");
    }
    public void TestMethod2()
    {
        Debug.Log("TestMethod2() on \"" + name + "\"");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        clamped = Mathf.Clamp(clamped, clampFrom, clampTo);

        if (callUnityEvent)
        {
            callUnityEvent = false;
            unityEvent.Invoke();
        }
    }
#endif

    [Space(4)]

    [BeginFolder("Potions Folder", 10)]
    public Ingredient potionResult;
    [EndFolder]

    [Space(4)]
    public DrawLabels drawLabels;

    [Space(4)]

    [BeginFolder("Folder 2", 15)]
    [Space(4)]
    [MinMax(-2, 3, MinLimit = -6, MaxLimit = 10)]
    public Vector2 minMaxDrawer;
    [EndFolder]

    [Space(4)]
    public Class1 class1;

    [Tooltip("Currently Extended is the default. [NonExtendedScriptableObject] can be used to disable it on a per field basis.")]
    //[NonExtendedScriptableObject]
    public ScriptableObject defaultScriptableObjectDrawer;
    [Tooltip("This uses [NonExtendedScriptableObject]")]
    [NonExtendedScriptableObject]
    public ScriptableObject nonExtendedScriptableObjectDrawer;

    [Space(4)]

    public Bool4 bool4;
    public Bool3 bool3;

    [Tooltip("This is an example of an unwrapped property. The drawer (which... I made (a long time ago)) calls methods like GUI.HorizontalSlider, so individual override buttons cannot be shown outside of Raw View.")]
    [SuggestionRange(0, 10, 0)]
    public Vector4 suggestionRangeVector4;
    [Space(4)]

    [NewFolder("Folder 3", 10)]
    [Space(4)]
    public Matrix4x4 matrix4x4;

    [Space(4)]
    [Range(-3.1415f, 5.1f)]
    public float rangeFloat;
    [SuggestionRange(-3.1415f, 5.1f)]
    public float suggestionRangeFloat;


    [BeginFolder("Potions Folder")]
    public Ingredient[] potionIngredients = new Ingredient[2];
    [EndFolder]

    [Space(10)]
    [ReadOnlyField]
    [Header("Uses ReadOnlyFieldAttribute")]
    public int readOnlyRandomSeed = Guid.NewGuid().GetHashCode();

    [Space(10)]
    [BeginFolder("Folder 2", 15)]
    public bool boolean;

    [Space(5)]
    [Tooltip("...")]
    public int integer;
    [Tooltip("...")]
    public long longInt;
    [Tooltip("...")]
    public uint unsignedInt;
    [EndFolder]
    [Header("Header")]
    public Vector2 vector2;
    public Vector3 vector3;
    public Vector4 vector4;
    [Space(5)]
    //float2
    [Space(5)]
#if UNITY_2017_2_OR_NEWER
    public Vector2Int vector2Int;
    public Vector3Int vector3Int;
#endif

    [NewFolder("Folder 4")]
    [Header("Header")]
    public KeyCode keyCodeEnum;

    public Quaternion quaternion;

    public AnimationCurve animationCurve;

    [Space(5)]
    public Bounds bounds;
#if UNITY_2017_2_OR_NEWER
    public BoundsInt boundsInt;
#endif
    [Space(5)]
    public LayerMask layerMask;

    [Space(10)]
    [NewFolder("Folder 5")]
    [AssertRange(0, 100)]
    public float assertRangeDrawer0To100;
    [EndFolder]

    [Space(10)]
    public Color32 color32 = Color.red;
    public Color color = Color.blue;
    [NegativeColor]
    public Color negativeColorDrawer = Color.clear - Color.white;

    [Space(5)]
    public Gradient gradient;

    [Space(10)]
    public string text = "Some Text";
    [TextArea(2, 5)]
    public string textArea = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
    public char character = 'e';

    [Space(10)]
    [BeginFolder("Folder 6", 15)]
    public Texture texture;

    // (This is a sloppy implementation because it leaves behind a bool in a build)
    // (additionally, because it's actually a field, it draws an override button)
    [ButtonBool]
    public bool callUnityEvent;

    public UnityEvent unityEvent;

    public Rect rect = new Rect(10, 20, 100, 300);
#if UNITY_2017_2_OR_NEWER
    public RectInt rectInt = new RectInt(10, 20, 100, 300);
#endif
    [EndFolder]

    [Space(10)]
    public List<Class1> listOfClass1 = new List<Class1>() { new Class1() };

    [Serializable]
    public class Class1
    {
        public Class2[] class2Array = new Class2[4];

        [Space(5)]
        public Class2 class2;

        [Space(5)]
        public Ingredient ingredientA;
        public Ingredient ingredientB;

        [Serializable]
        public class Class2
        {
            public UnityEngine.Object unityObject1;
            public UnityEngine.Object unityObject2;
        }
    }

    [ExtendedScriptableObject(true)]
    [FormerlySerializedAs("extendedScriptableObjectDrawer")]
    public ScriptableObject readonlyExtendedScriptableObjectDrawer;

    [Space(5)]
    public UnityEngine.Object unityObject;

    [Space(10)]
    [SerializeField]
    private float privateSerializeField;








    [Space(20)]
    [Header("InlinePropertyDrawer 2")]
    public DontDrawLabels dontDrawLabels;
    public DontDrawLabels[] dontDrawLabelsArray = new DontDrawLabels[3];


    // Just invert this and see the Debug.Log when an override rename occurs
#if true
    [FormerlySerializedAs("formerlySerializedAs2")]
    public string formerlySerializedAs1;
#else
    [FormerlySerializedAs("formerlySerializedAs1")]
    public string formerlySerializedAs2;
#endif


    [Header("OnValidate Mathf.Clamp")]
    public float clampFrom = 0;
    public float clampTo = 1, clamped = 2;


    [Tooltip("Tooltip")]
    public float floatField;
    public double doubleField;


    public HiddenChildrenClass hiddenChildrenClass = new HiddenChildrenClass();
    [Serializable]
    public class HiddenChildrenClass
    {
        [HideInInspector]
        public float r, g;
    }
}
