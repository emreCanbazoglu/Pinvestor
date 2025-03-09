#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

[CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
internal sealed class ReadOnlyFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool prevGUIEnabled = GUI.enabled;
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = prevGUIEnabled;
    }

#if UNITY_2019_1_OR_NEWER
    //TODO2: #ifdef for the first version where nesting context is applied
    //public override VisualElement CreatePropertyGUI(SerializedProperty property)
    //{
    //    var propertyField = new PropertyField(property);
    //    propertyField.SetEnabled(false);
    //    return propertyField;
    //}
#endif
}
#endif
