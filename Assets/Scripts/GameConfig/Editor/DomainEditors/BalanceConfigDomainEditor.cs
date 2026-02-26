using UnityEditor;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class BalanceConfigDomainEditor
    {
        public static void Draw(SerializedObject serializedObject)
        {
            DomainEditorUtil.DrawArray(serializedObject, "_balance", "Balance Values");
        }
    }
}

