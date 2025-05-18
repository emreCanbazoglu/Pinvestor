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

            if (ability.ActionDescriptions == null || ability.ActionDescriptions.Count == 0)
                return;

            GameplayEffectScriptableObject mainEffect = ability.GetMainGameplayEffect();
            float duration = ability.TryGetGlobalDuration();

            string preview = AbilityDescriptionUtility.GenerateActionDescriptions(
                ability.ActionDescriptions,
                mainEffect,
                duration);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("\u2728 Live Description Preview", EditorStyles.boldLabel);

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.wordWrap = true;
            boxStyle.normal.textColor = Color.white;
            boxStyle.fontSize = 11;

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField(preview, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }
    }
}