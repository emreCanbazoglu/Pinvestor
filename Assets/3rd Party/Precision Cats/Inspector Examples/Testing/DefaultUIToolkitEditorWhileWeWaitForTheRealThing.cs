#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Test
{
    public static class UITookitEditorHelper
    {
        public static void FillDefaultInspector(VisualElement container, SerializedObject serializedObject, bool hideScript)
        {
            SerializedProperty property = serializedObject.GetIterator();
            if (property.NextVisible(true)) // Expand first child.
            {
                do
                {
                    if (hideScript && property.propertyPath == "m_Script")
                    {
                        continue;
                    }
                    var field = new PropertyField(property);
                    field.name = "PropertyField:" + property.propertyPath;

                    if (property.propertyPath == "m_Script" && serializedObject.targetObject != null)
                    {
                        field.SetEnabled(false);
                    }

                    container.Add(field);
                }
                while (property.NextVisible(false));
            }
        }
    }

#if false //DEBUG_ASSET_VARIANTS && false
    [CustomEditor(typeof(ScriptableObject), editorForChildClasses: true)]
    public class DefaultUIToolkitEditorWhileWeWaitForTheRealThing : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var container = new VisualElement();
            UITookitEditorHelper.FillDefaultInspector(container, serializedObject, false);
            Debug.Log("DefaultUIToolkitEditorWhileWeWaitForTheRealThing duration: " + sw.Elapsed.TotalMilliseconds + "ms");
            return container;
        }
    }
#endif
}
#endif
