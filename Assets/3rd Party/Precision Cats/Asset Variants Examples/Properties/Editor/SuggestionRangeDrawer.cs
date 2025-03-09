#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using System.ComponentModel;
using System;

using SPT = UnityEditor.SerializedPropertyType;

[CustomPropertyDrawer(typeof(SuggestionRangeAttribute))]
public class SuggestionRangeDrawer : PropertyDrawer
{
    // Constants
    public static float TEXT_WIDTH = 50;
    public static float TEXT_BUFFER = 5;
    public static float PREFIX_WIDTH = 25;
    public static float INT_SENSITIVITY = 0.1f;

    private static MethodInfo doFloatFieldMethod;
    private static MethodInfo doIntFieldMethod;
    private static object recycledEditor;


    //Constructor
    static SuggestionRangeDrawer()
    {
        Type editorGUIType = typeof(EditorGUI);
        Type RecycledTextEditorType = Assembly.GetAssembly(editorGUIType).GetType("UnityEditor.EditorGUI+RecycledTextEditor");
        Type[] argumentTypes = new Type[]
        {
            RecycledTextEditorType,
            typeof(Rect), typeof(Rect),
            typeof(int), typeof(float),
            typeof(string), typeof(GUIStyle),
            typeof(bool)
        };
        doFloatFieldMethod = editorGUIType.GetMethod("DoFloatField", BindingFlags.NonPublic | BindingFlags.Static, null, argumentTypes, null);
        argumentTypes = new Type[]
        {
            RecycledTextEditorType,
            typeof(Rect), typeof(Rect),
            typeof(int), typeof(int),
            typeof(string), typeof(GUIStyle),
            typeof(bool),
            typeof(float)
        };
        doIntFieldMethod = editorGUIType.GetMethod("DoIntField", BindingFlags.NonPublic | BindingFlags.Static, null, argumentTypes, null);

        FieldInfo fieldInfo = editorGUIType.GetField("s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static);
        recycledEditor = fieldInfo.GetValue(null);
    }


    // Methods
    private float DoFieldInternal(Rect position, Rect dragHotZone, float value, GUIStyle style, bool isInt) //[DefaultValue("EditorStyles.numberField")] 
    {
        int controlID = GUIUtility.GetControlID("EditorTextField".GetHashCode(), FocusType.Keyboard, position);

        if (isInt)
        {
            var intValue = Mathf.RoundToInt(value);
            object[] parameters = new object[] { recycledEditor, position, dragHotZone, controlID, intValue, "#######0", style, true, INT_SENSITIVITY };
            return (int)doIntFieldMethod.Invoke(null, parameters);
        }
        else
        {
            object[] parameters = new object[] { recycledEditor, position, dragHotZone, controlID, value, "g7", style, true };
            return (float)doFloatFieldMethod.Invoke(null, parameters);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int c = GetCount(property.propertyType);
        return c * EditorGUIUtility.singleLineHeight + (c - 1) * EditorGUIUtility.standardVerticalSpacing;
    }

    private int GetCount(SPT pt)
    {
        if (pt == SPT.Vector4)
            return 4;
#if UNITY_2017_2_OR_NEWER
        else if (pt == SPT.Vector3 || pt == SPT.Vector3Int)
            return 3;
        else if (pt == SPT.Vector2 || pt == SPT.Vector2Int)
            return 2;
#endif
        else if (pt == SPT.Float || pt == SPT.Integer)
            return 1;
        return 0;
    }

    private float GetValue(SerializedProperty property, bool isInt)
    {
        if (isInt)
            return property.intValue;
        else
            return property.floatValue;
    }
    private void SetValue(SerializedProperty property, bool isInt, SuggestionRangeAttribute range, float v)
    {
        v = Mathf.Clamp(v, range.absoluteMin, range.absoluteMax);
        if (isInt)
            property.intValue = Mathf.RoundToInt(v);
        else
            property.floatValue = v;
    }

    private void Do(Rect rect, SerializedProperty property, SuggestionRangeAttribute range, bool isVector = true)
    {
        var content = new GUIContent(property.displayName);
        EditorGUI.BeginProperty(rect, content, property);

        bool isInt = property.propertyType == SPT.Integer;


        Rect dragRect;
        if (isVector)
        {
            dragRect = rect;
            dragRect.width = PREFIX_WIDTH;
            EditorGUI.HandlePrefixLabel(rect, dragRect, content);
            rect.x += PREFIX_WIDTH;
            rect.width -= PREFIX_WIDTH;
        }
        else
        {
            var origRect = rect;
            rect = EditorGUI.PrefixLabel(rect, content);
            dragRect = origRect;
            dragRect.width = rect.x - origRect.x;
        }


        rect.width -= TEXT_WIDTH + TEXT_BUFFER;

        var rect2 = rect;
        rect2.x += rect.width + TEXT_BUFFER;
        rect2.width = TEXT_WIDTH;
        EditorGUI.BeginChangeCheck();
        var textValue = DoFieldInternal(rect2, dragRect, GetValue(property, isInt), EditorStyles.numberField, isInt); //EditorGUI.FloatField(rect2, GetValue());
        if (EditorGUI.EndChangeCheck())
            SetValue(property, isInt, range, textValue);

        EditorGUI.BeginChangeCheck();
        var sliderValue = GUI.HorizontalSlider(rect, GetValue(property, isInt), range.min, range.max); //EditorGUI.Slider(position, property, range.min, range.max, label);
        if (EditorGUI.EndChangeCheck())
        {
            GUI.FocusControl(null);
            SetValue(property, isInt, range, sliderValue);
        }

        EditorGUI.EndProperty();
    }

    public override void OnGUI(Rect rect, SerializedProperty outerProperty, GUIContent label)
    {
        label = EditorGUI.BeginProperty(rect, label, outerProperty);

        SuggestionRangeAttribute range = attribute as SuggestionRangeAttribute;

        int count = GetCount(outerProperty.propertyType);

        if (count == 0)
        {
            EditorGUI.LabelField(rect, label.text, "Incompatible with SuggestionRange");
            return;
        }
        else if (count == 1)
        {
            Do(rect, outerProperty, range, false);
        }
        else
        {
            rect.height = EditorGUIUtility.singleLineHeight;

            float yOff = rect.height + EditorGUIUtility.standardVerticalSpacing;

            var id = GUIUtility.GetControlID(FocusType.Keyboard);
            rect = EditorGUI.PrefixLabel(rect, id, label);

            if (count >= 2)
            {
                Do(rect, outerProperty.FindPropertyRelative("x"), range);
                rect.y += yOff;
                Do(rect, outerProperty.FindPropertyRelative("y"), range);
                rect.y += yOff;
            }
            if (count >= 3)
            {
                Do(rect, outerProperty.FindPropertyRelative("z"), range);
                rect.y += yOff;
            }
            if (count == 4)
                Do(rect, outerProperty.FindPropertyRelative("w"), range);
        }

        EditorGUI.EndProperty();
    }
}
#endif