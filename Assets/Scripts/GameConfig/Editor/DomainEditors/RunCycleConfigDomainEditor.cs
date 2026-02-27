using UnityEditor;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class RunCycleConfigDomainEditor
    {
        public static void Draw(SerializedObject serializedObject)
        {
            DomainEditorUtil.DrawArray(
                serializedObject,
                "_runCycleRounds",
                "Run Cycle Rounds");
        }
    }
}
