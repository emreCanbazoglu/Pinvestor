#if UNITY_EDITOR
using System;
using UnityEditor;

namespace EditorReplacement
{
    public struct PropertyHandlerScope : IDisposable
    {
        private readonly object prevPropertyHandlerCache;

        public PropertyHandlerScope(object propertyHandlerCache)
        {
            prevPropertyHandlerCache = R.currentCacheFI.GetValue(null);
            R.currentCacheFI.SetValue(null, propertyHandlerCache);
        }
        public PropertyHandlerScope(Editor editor)
        {
            prevPropertyHandlerCache = ERHelper.BeginEditor(editor);
        }
        public PropertyHandlerScope(SerializedObject serializedObject)
        {
            prevPropertyHandlerCache = ERHelper.BeginSO(serializedObject);
        }

        public void Dispose()
        {
            R.currentCacheFI.SetValue(null, prevPropertyHandlerCache);
        }
    }
}
#endif
