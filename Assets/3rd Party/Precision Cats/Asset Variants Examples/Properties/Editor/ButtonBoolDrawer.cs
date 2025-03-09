#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ButtonBoolAttribute))]
internal sealed class ButtonBoolDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position = EditorGUI.IndentedRect(position);
        if (GUI.Button(position, label))
            property.boolValue = true;
    }
}
#endif