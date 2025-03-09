#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace EditorReplacement
{
    public static class UIToolkitHelper
    {
        public static void ContextualFillDefaultInspector(VisualElement container, Editor editor, bool visibleOnly = true, bool disableScript = true)
        {
            using (new PropertyHandlerScope(editor)) //?
            {
                var serializedObject = editor.serializedObject;

                SerializedProperty property = serializedObject.GetIterator();
                if (property.NextVisible(true))
                {
                    do
                    {
                        //var field = new WrapperPropertyField(editor, property);
                        var field = new PropertyField(property);
                        field.name = "WrapperPropertyField:" + property.propertyPath;

                        if (disableScript && property.propertyPath == "m_Script" && serializedObject.targetObject != null)
                            field.SetEnabled(false);

                        container.Add(field);
                    }
                    while (visibleOnly ? property.NextVisible(false) : property.Next(false));
                }
            }
        }
    }
}
#endif
