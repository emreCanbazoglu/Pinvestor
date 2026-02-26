using UnityEditor;

namespace Pinvestor.GameConfigSystem.Editor
{
    internal static class ShopConfigDomainEditor
    {
        public static void Draw(SerializedObject serializedObject)
        {
            DomainEditorUtil.DrawArray(serializedObject, "_shop", "Shop Values");
        }
    }
}

