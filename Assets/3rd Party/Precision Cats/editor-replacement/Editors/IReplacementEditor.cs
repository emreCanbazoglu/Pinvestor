#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace EditorReplacement
{
    public interface IReplacementBase<IRType> where IRType : IReplacementBase<IRType>
    {
        /// <summary>
        /// Do not modify this.
        /// </summary>
        IRType Root { get; set; }
        /// <summary>
        /// Do not modify this. This has to be initialized to -1.
        /// </summary>
        int Depth { get; set; }
        /// <summary>
        /// Sibling index.
        /// Do not modify this.
        /// </summary>
        int Index { get; set; }
        /// <summary>
        /// Do not modify this.
        /// </summary>
        IRType Parent { get; set; }
    }

    public struct AssetEditor
    {
        public Editor importerEditor; //UnityEditor.AssetImporters.AssetImporterEditor
        public Editor assetEditor;
    }

    /// <summary>
    /// You probably want to inherit from WrapperEditor instead.
    /// </summary>
    public interface IReplacementEditor : IReplacementBase<IReplacementEditor>
    {
        /// <summary>
        /// Do not modify this.
        /// </summary>
        List<Editor> ChildrenEditors { get; }

        /// <summary>
        /// Do not modify this.
        /// The editors created for children AssetImporterEditors.
        /// </summary>
        List<AssetEditor> AssetEditors { get; }

        /// <summary>
        /// Do not call this.
        /// </summary>
        void ProcessTree(IReplacementEditor root, EditorTreeDef treeDef);

        /// <summary>
        /// Do not call this.
        /// </summary>
        void OnCreatedTree(IReplacementEditor root, EditorTreeDef treeDef);

        IEnumerable<Editor> EnumerateEditors(Object[] targetsFilter = null);
    }

#if DEBUG_ASSET_VARIANTS //TODO2: ?
    internal static class IReplacementEditorValidator
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            foreach (Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (System.Type t in a.GetTypes())
                {
                    if (!t.IsValueType)
                    {
                        if (typeof(IReplacementEditor).IsAssignableFrom(t))
                        {
                            var customEditor = t.GetCustomAttribute<CustomEditor>(false);
                            if (customEditor != null)
                                Debug.LogError("Type " + t.FullName + " has [CustomEditor()] even though it implements EditorReplacement.IReplacementEditor. This is very problematic. IReplacementEditor is designed to be used with EditorReplacer.");
                        }
                    }
                }
            }
        }
    }
#endif
}
#endif
