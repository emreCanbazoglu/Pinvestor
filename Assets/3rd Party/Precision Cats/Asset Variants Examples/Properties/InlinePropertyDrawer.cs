#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public abstract class InlinePropertyDrawer : PropertyDrawer
{
    protected virtual float PreSpace { get { return 0; } }

    protected virtual bool DrawLabels { get { return true; } }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        property.isExpanded = true;

        float h = PreSpace;

        int count = 0;

        var prop = property.Copy();
        if (prop.NextVisible(true))
        {
            int depth = prop.depth;
            do
            {
                h += EditorGUI.GetPropertyHeight(prop, true);
                count++;
            }
            while (prop.NextVisible(false) && prop.depth >= depth);
        }

        h += (count - 1) * EditorGUIUtility.standardVerticalSpacing;

        return h;
    }

    protected virtual void DoProperty(Rect position, SerializedProperty prop)
    {
        if (!DrawLabels)
            EditorGUI.PropertyField(position, prop, GUIContent.none, true);
        else
            EditorGUI.PropertyField(position, prop, true);
    }

    protected virtual bool Indent { get { return true; } }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.y += PreSpace;
        position.height -= PreSpace;

        EditorGUI.BeginProperty(position, label, property);

        //position = EditorGUI.PrefixLabel(position, label); //if (label != GUIContent.none)
        if (Indent)
            EditorGUI.indentLevel++;

        float svs = EditorGUIUtility.standardVerticalSpacing;

        var prop = property.Copy();
        if (prop.NextVisible(true))
        {
            int depth = prop.depth;
            do
            {
                position.height = EditorGUI.GetPropertyHeight(prop, true);
                DoProperty(position, prop);
                position.y += position.height + svs;
            }
            while (prop.NextVisible(false) && prop.depth >= depth);
        }

        if (Indent)
            EditorGUI.indentLevel--;

        EditorGUI.EndProperty();
    }
}
#endif