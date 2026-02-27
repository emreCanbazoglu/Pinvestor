using UnityEditor;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class BallConfigDomainEditor
    {
        public static void Draw(SerializedObject serializedObject)
        {
            SerializedProperty prop = serializedObject.FindProperty("_ball");
            if (prop == null)
            {
                EditorGUILayout.HelpBox("Ball config property not found.", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(prop, new UnityEngine.GUIContent("Ball Config"), true);
        }
    }
}
