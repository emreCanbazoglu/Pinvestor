using UnityEditor;
using UnityEngine;
using AbilitySystem.Authoring;

namespace AbilitySystem.Editor
{

    [CustomEditor(typeof(GameplayEffectScriptableObject))]
    public class GameplayEffectScriptableObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty modifiersProp;
        private SerializedProperty descriptionsProp;

        private void OnEnable()
        {
            modifiersProp = serializedObject.FindProperty("gameplayEffect").FindPropertyRelative("Modifiers");
            descriptionsProp = serializedObject.FindProperty("ModifierDescriptions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Gameplay Effect Modifiers", EditorStyles.boldLabel);

            int modifierCount = modifiersProp.arraySize;

            // Sync description size
            while (descriptionsProp.arraySize < modifierCount)
                descriptionsProp.InsertArrayElementAtIndex(descriptionsProp.arraySize);
            while (descriptionsProp.arraySize > modifierCount)
                descriptionsProp.DeleteArrayElementAtIndex(descriptionsProp.arraySize - 1);

            for (int i = 0; i < modifierCount; i++)
            {
                var modifier = modifiersProp.GetArrayElementAtIndex(i);
                var description = descriptionsProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Modifier {i + 1}", EditorStyles.boldLabel);

                // Show Modifier Info
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("Attribute"), new GUIContent("Attribute"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("ModifierOperator"),
                    new GUIContent("Operator"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("ModifierMagnitude"),
                    new GUIContent("Magnitude"));
                EditorGUILayout.PropertyField(modifier.FindPropertyRelative("Multiplier"),
                    new GUIContent("Multiplier"));
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                // Show Effect Description Meta
                EditorGUILayout.LabelField("Description Settings", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(description.FindPropertyRelative("Verb"), new GUIContent("Verb"));
                EditorGUILayout.PropertyField(description.FindPropertyRelative("Tone"), new GUIContent("Tone"));
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
