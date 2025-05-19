using UnityEditor;
using UnityEngine;
using AbilitySystem.Authoring;

namespace AbilitySystem.Editor
{
    [CustomEditor(typeof(GameplayEffectScriptableObject))]
    public class GameplayEffectScriptableObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty modifiersProp;

        private void OnEnable()
        {
            modifiersProp = serializedObject.FindProperty("gameplayEffect").FindPropertyRelative("Modifiers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Gameplay Effect Modifiers", EditorStyles.boldLabel);

            int modifierCount = modifiersProp.arraySize;

            for (int i = 0; i < modifierCount; i++)
            {
                var modifier = modifiersProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Modifier {i + 1}", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("Attribute"), new GUIContent("Attribute"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("ModifierOperator"), new GUIContent("Operator"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("ModifierMagnitude"), new GUIContent("Magnitude"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("Multiplier"), new GUIContent("Multiplier"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("DescriptionKey"), new GUIContent("Description Key"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("Tone"), new GUIContent("Tone"));
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(8);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gameplayEffectTags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Period"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ModifierAppliedHandlers"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
