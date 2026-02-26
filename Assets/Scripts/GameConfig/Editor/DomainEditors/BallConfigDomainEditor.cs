using UnityEditor;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class BallConfigDomainEditor
    {
        public static void Draw(SerializedObject serializedObject)
        {
            DomainEditorUtil.DrawArray(serializedObject, "_ball", "Ball Values");
        }
    }
}

