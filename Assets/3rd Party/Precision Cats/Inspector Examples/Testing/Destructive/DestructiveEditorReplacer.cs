#if UNITY_EDITOR && EDITOR_REPLACEMENT
using UnityEditor;
using System;
using System.Collections.Generic;

namespace EditorReplacement.Examples
{
    public class DestructiveEditor : Editor { }

    public class DestructiveEditorReplacer : EditorReplacer
    {
        public override string SystemKey => "DestructiveEditorReplacer";

        protected override void DeepCopyTo(EditorReplacer deepCopy) { }

        public override Type GetEditorType(Type targetType, EditorTreeLocation location)
        {
            return typeof(DestructiveEditor);
        }

        public override void Replace(EditorTreeDef treeDef)
        {
            treeDef.Root.children.Clear();
            treeDef.Root.replacer = this;
        }
    }
}
#endif
