using UnityEditor;
using UnityEngine;
using AbilitySystem.Authoring;

namespace AbilitySystem.Editor
{

    [CustomEditor(typeof(AbstractAbilityScriptableObject), true)]
    public class AbilityActionPreviewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            AbstractAbilityScriptableObject ability = (AbstractAbilityScriptableObject)target;

            if (string.IsNullOrWhiteSpace(ability.CustomDescription))
                return;

            var modifiers = ability.GetAllGameplayEffectModifiers();
            float duration = ability.TryGetGlobalDuration();

            if (modifiers == null || modifiers.Length == 0)
                return;

            string preview = AbilityDescriptionUtility.GenerateManualDescription(
                ability.CustomDescription,
                modifiers,
                duration);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("\u2728 Live Description Preview", EditorStyles.boldLabel);

            GUIStyle richTextStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = 11,
                normal = { textColor = Color.white }
            };

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(preview, richTextStyle);
            EditorGUILayout.EndVertical();
        }
    }
}