using UnityEditor;
using UnityEngine;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class DomainEditorUtil
    {
        public static void DrawArray(SerializedObject serializedObject, string propertyName, string label)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop == null)
            {
                EditorGUILayout.HelpBox($"Property not found: {propertyName}", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
        }
    }
}

