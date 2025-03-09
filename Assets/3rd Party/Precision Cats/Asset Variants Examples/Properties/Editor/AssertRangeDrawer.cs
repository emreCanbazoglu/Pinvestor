#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(AssertRangeAttribute))]
internal sealed class AssertRangeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var rangeAttribute = (AssertRangeAttribute)base.attribute;

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            EditorGUI.PropertyField(position, property, label);
            property.intValue = Mathf.Clamp(property.intValue, rangeAttribute.intMin, rangeAttribute.intMax);
        }
        else if (property.propertyType == SerializedPropertyType.Float)
        {
            EditorGUI.PropertyField(position, property, label);
            property.floatValue = Mathf.Clamp(property.floatValue, rangeAttribute.floatMin, rangeAttribute.floatMax);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use AssertRange with float or int.");
        }
    }
}
#endif