#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Bool4))]
public class Bool4Drawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, label, property);

        rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);

        rect.width *= 0.25f;

        var propertyCopy = property.Copy();
        for (int i = 0; i < 4; i++)
        {
            propertyCopy.NextVisible(true);
            EditorGUI.PropertyField(rect, propertyCopy, GUIContent.none);
            rect.x += rect.width;
        }
        propertyCopy.Dispose();

        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(Bool3))]
public class Bool3Drawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, label, property);

        rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);

        rect.width *= (1.0f / 3.0f);

        var propertyCopy = property.Copy();
        for (int i = 0; i < 3; i++)
        {
            propertyCopy.NextVisible(true);
            EditorGUI.PropertyField(rect, propertyCopy, GUIContent.none);
            rect.x += rect.width;
        }
        propertyCopy.Dispose();

        EditorGUI.EndProperty();
    }
}
#endif
