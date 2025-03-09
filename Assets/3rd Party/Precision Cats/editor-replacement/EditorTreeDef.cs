#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;

namespace EditorReplacement
{
    public class EditorTreeLocation : TreeLocation<EditorTreeLocation, EditorReplacer, EditorTreeDef, EditorTreeNode>
    {
        public EditorTreeLocation(int depth, int index, EditorTreeNode node, EditorTreeDef treeDef, EditorTreeLocation parent) : base(depth, index, node, treeDef, parent) { }

        static EditorTreeLocation()
        {
            Create = (depth, index, node, treeDef, parent) =>
            {
                return new EditorTreeLocation(depth, index, node, treeDef, parent);
            };
        }
    }

    public class EditorTreeNode : TreeNode<EditorReplacer, EditorTreeDef, EditorTreeNode>
    {
        public IReadOnlyList<Object> Targets { get; internal set; }

        protected override void DeepCopyTo(EditorTreeNode deepCopy)
        {
            deepCopy.Targets = Targets;
        }

        protected override EditorTreeNode CreateNode(EditorReplacer replacer) { return new EditorTreeNode(replacer); }
        public EditorTreeNode(EditorReplacer replacer) : base(replacer) { }
        public EditorTreeNode(EditorReplacer replacer, params EditorTreeNode[] children) : base(replacer, children) { }
        public EditorTreeNode(EditorReplacer replacer, IList<EditorTreeNode> children) : base(replacer, children) { }
    }

    public class EditorTreeDef : TreeDef<EditorReplacer, EditorTreeDef, EditorTreeNode>
    {
        public readonly Type inspectedType;
        public readonly Type renderPipelineType;
        public readonly bool multiEdit;
        public readonly bool forChildClasses;
        /// <summary>
        /// This is a wrap of the original editor/s that Unity would search through until it found a suitable match.
        /// It will go through these like normal if you're the innermost EditorReplacer in the tree and you call GetSubEditorType()/CreateSubEditor() to find which editor you will wrap.
        /// It can be null if there was no original editor/s specified with CustomEditorAttribute and it needed to create a dictionary entry for inspectedType only for EditorReplacer/s because of EditorReplacer.CreateInspectedTypes.
        /// Modifications to this list will persist across the different passes (the calls to EditorReplacer.Replace() of different multiEdit/forChildClasses/renderPipelineType combinations).
        /// </summary>
        public readonly List<MonoEditorType> monoEditorTypes = null;

        internal EditorTreeDef(Type inspectedType, Type renderPipelineType, bool multiEdit, bool forChildClasses, List<MonoEditorType> monoEditorTypes)
        {
            this.inspectedType = inspectedType;
            this.renderPipelineType = renderPipelineType;
            this.multiEdit = multiEdit;
            this.forChildClasses = forChildClasses;
            this.monoEditorTypes = monoEditorTypes;
        }

        public EditorTreeLocation RootLocation => new EditorTreeLocation(0, 0, Root, this, null);

        public EditorTreeDef DeepCopy()
        {
            var met = monoEditorTypes == null ? null : new List<MonoEditorType>(monoEditorTypes);
            return new EditorTreeDef(inspectedType, renderPipelineType, multiEdit, forChildClasses, met)
            {
                Root = Root.DeepCopy()
            };
        }

        protected override EditorTreeNode CreateNode(EditorReplacer replacer) { return new EditorTreeNode(replacer); }
        protected override EditorTreeNode CreateNode(EditorReplacer replacer, List<EditorTreeNode> children) { return new EditorTreeNode(replacer, children); }
    }
}
#endif
