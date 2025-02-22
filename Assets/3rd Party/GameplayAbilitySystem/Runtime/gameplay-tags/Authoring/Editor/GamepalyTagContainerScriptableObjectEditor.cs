using UnityEditor;
using UnityEngine;

namespace GameplayTag.Authoring
{
    [CustomEditor(typeof(GameplayTagContainerScriptableObject))]
    public class GameplayTagContainerScriptableObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GameplayTagContainerScriptableObject scriptableObject = (GameplayTagContainerScriptableObject)target;

            DrawDefaultInspector();
            
            // Add a custom button
            if (GUILayout.Button("Collect Gameplay Tags"))
                scriptableObject.CollectGameplayTags();
        }
    }
}