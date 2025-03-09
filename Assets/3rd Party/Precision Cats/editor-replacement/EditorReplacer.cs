#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using SystemPrefs;
using PrecisionCats;
using Object = UnityEngine.Object;

namespace EditorReplacement
{
    public enum ChildAction { Keep, RemoveReparent, RemoveWhole }

    public abstract class EditorReplacer : Replacer<EditorReplacer, EditorTreeDef, EditorTreeNode>
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SystemPrefsWindow.tabs.Add(new ReplacerTab()
            {
                title = new GUIContent("Editor Replacers", "Shows every key found in the current EditorReplacer.sses list. (This means inactive color wraps' keys won't be shown).")
            });
        }

        /// <summary>
        /// Like DeepCopy() but the Targets are copied over.
        /// </summary>
        public sealed override EditorReplacer DeepCopy()
        {
            var deepCopy = (EditorReplacer)Activator.CreateInstance(GetType());
            DeepCopyTo(deepCopy);
            return deepCopy;
        }
        /// <summary>
        /// Implement to deep copy fields to an EditorReplacer of the same exact type.
        /// </summary>
        protected abstract void DeepCopyTo(EditorReplacer deepCopy);

        /// <summary>
        /// Override if you want your editor to use different targets.
        /// The purpose of this is to communicate intention with other replacers.
        /// Return null if you can't know ahead of time what the target type is.
        /// </summary>
        public virtual Type GetExpectedTargetType(Type parentTargetType, EditorTreeLocation location) => parentTargetType;
        /// <summary>
        /// Override if you want your editor to use different targets.
        /// They have to all be of the same type.
        /// For the root replacer, parentTargets will be inspectedTargets.
        /// If you return 0 targets, it will remove the node.
        /// </summary>
        public virtual IReadOnlyList<Object> GetTargets(IReadOnlyList<Object> parentTargets, EditorTreeLocation location) => parentTargets;

        /// <summary>
        /// You need to override this to true if OnAssignedTargets() or FinalPostProcess() might remove the root node.
        /// This will make it so that before the root editor type is sent to Unity's cache, the tree will be wrapped up one more time with a generic pass-through replacer.
        /// </summary>
        public virtual bool RootNeedsWrapper => false; //MightRemoveRoot

        /// <summary>
        /// Called after Targets has been filled with GetTargets().
        /// You can use this to modify yourself and the children nodes.
        /// Note that the children nodes do not have Targets yet.
        /// In FinalPostProcess they will.
        /// Return if this node should be kept or removed from its parent.
        /// </summary>
        public virtual ChildAction OnAssignedTargets(IReadOnlyList<Object> inspectedTargets, IReadOnlyList<Object> parentTargets, EditorTreeNode node)
        { return ChildAction.Keep; }

        /// <summary>
        /// Post process the tree instantiation before the editors get created. This happens after OnAssignedTargets().
        /// </summary>
        public virtual void FinalPostProcess(EditorTreeNode node, EditorTreeDef treeDef) { }

        public abstract Type GetEditorType(Type targetType, EditorTreeLocation location);

        /// <summary>
        /// This skips children as well.
        /// </summary>
        public virtual bool ShouldSkip(Editor parentEditor) => false;

        public virtual void OnCreatedEditor(Editor editor) { }

        public virtual void OnCreatedTree(IReplacementEditor root, EditorTreeDef treeDef) { }

        /// <summary>
        /// If there was no original editor for a type, you can use this to ensure they are registered, even if it has to create it.
        /// You need to use this to enumerate all targetTypes that are conditions that determine the results of EditorReplacer.GetEditorType(), if they don't already have a specific [CustomEditor()].
        /// </summary>
        public virtual IEnumerable<Type> CreateInspectedTypes => null;

        /// <summary>
        /// Override this with the RenderPipeline types that are factors in the decisions made in Replace() or PostProcess()
        /// </summary>
        public virtual IEnumerable<Type> GetRenderPipelineTypes(Type inspectedType) => null;
    }

    public abstract class FilteredEditorReplacer<T> : EditorReplacer where T : EditorReplacer
    {
        /// <summary>
        /// This can be for targetType or inspectedType depending on the EditorReplacer's implementation
        /// </summary>
        public static TypeFilter typeFilter = new TypeFilter();

        public override IEnumerable<Type> CreateInspectedTypes
        {
            get
            {
                if (typeFilter.useWhitelist)
                {
                    for (int i = 0; i < typeFilter.whitelist.Count; i++)
                        yield return typeFilter.whitelist[i];
                    for (int i = 0; i < typeFilter.inheritedWhitelist.Count; i++)
                        yield return typeFilter.inheritedWhitelist[i];
                }
                if (typeFilter.useBlacklist)
                {
                    for (int i = 0; i < typeFilter.blacklist.Count; i++)
                        yield return typeFilter.blacklist[i];
                    for (int i = 0; i < typeFilter.inheritedBlacklist.Count; i++)
                        yield return typeFilter.inheritedBlacklist[i];
                }
            }
        }
    }

    public abstract class RemapFilteredEditorReplacer<T> : FilteredEditorReplacer<T> where T : EditorReplacer
    {
        /// <summary>
        /// The way to specify an Editor type for an asset type. For example { Material, AVMaterialEditor }.
        /// Does not apply to child classes.
        /// </summary>
        public static readonly Dictionary<Type, Type> editorTypeRemap = new Dictionary<Type, Type>()
        { };
        /// <summary>
        /// The way to specify an Editor type for an asset type. For example { Material, AVMaterialEditor }.
        /// Also applies to child classes.
        /// </summary>
        public static readonly Dictionary<Type, Type> inheritedEditorTypeRemap = new Dictionary<Type, Type>()
        { };

        public static bool TryGetRemappedEditorType(Type targetType, out Type editorType)
        {
            if (editorTypeRemap.TryGetValue(targetType, out editorType))
                return true;
            for (Type type = targetType; type != null; type = type.BaseType)
                if (inheritedEditorTypeRemap.TryGetValue(type, out editorType))
                    return true;
            editorType = null;
            return false;
        }

        public override IEnumerable<Type> CreateInspectedTypes
        {
            get
            {
                foreach (var type in base.CreateInspectedTypes)
                    yield return type;

                foreach (var kv in editorTypeRemap)
                    yield return kv.Key;
                foreach (var kv in inheritedEditorTypeRemap)
                    yield return kv.Key;
            }
        }
    }
}
#endif
