using UnityEditor;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class RoundCriteriaConfigDomainEditor
    {
        public static void Draw(SerializedObject serializedObject)
        {
            DomainEditorUtil.DrawArray(serializedObject, "_roundCriteria", "Round Criteria Values");
        }
    }
}

