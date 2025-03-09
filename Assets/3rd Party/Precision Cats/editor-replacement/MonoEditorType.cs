#if UNITY_EDITOR
using System;
using System.Reflection;

namespace EditorReplacement
{
    /// <summary>
    /// A public wrap of Unity's private class of the same name (which is found in the class CustomEditorAttributes)
    /// </summary>
    public struct MonoEditorType
    {
        public Type InspectedType => (Type)m_InspectedType.GetValue(actual);
        public Type InspectorType => (Type)m_InspectorType.GetValue(actual);
        public Type RenderPipelineType => (Type)m_RenderPipelineType.GetValue(actual);
        public bool EditorForChildClasses => (bool)m_EditorForChildClasses.GetValue(actual);
        public bool IsFallback => (bool)m_IsFallback.GetValue(actual);

        internal readonly object actual;
        internal MonoEditorType(object actual)
        {
            this.actual = actual;
        }

        private const BindingFlags pi = BindingFlags.Public | BindingFlags.Instance;
        internal static readonly FieldInfo m_InspectedType = R.monoEditorTypeType.GetField("m_InspectedType", pi);
        internal static readonly FieldInfo m_InspectorType = R.monoEditorTypeType.GetField("m_InspectorType", pi);
        internal static readonly FieldInfo m_RenderPipelineType = R.monoEditorTypeType.GetField("m_RenderPipelineType", pi);
        internal static readonly FieldInfo m_EditorForChildClasses = R.monoEditorTypeType.GetField("m_EditorForChildClasses", pi);
        internal static readonly FieldInfo m_IsFallback = R.monoEditorTypeType.GetField("m_IsFallback", pi);

        internal static object CreateActual(Type inspectedType, Type inspectorType, Type renderPipelineType, bool editorForChildClasses, bool isFallback)
        {
            var met = Activator.CreateInstance(R.monoEditorTypeType);
            m_InspectedType.SetValue(met, inspectedType);
            m_InspectorType.SetValue(met, inspectorType);
            m_RenderPipelineType.SetValue(met, renderPipelineType);
            m_EditorForChildClasses.SetValue(met, editorForChildClasses);
            m_IsFallback.SetValue(met, isFallback);
            return met;
        }
    }
}
#endif
