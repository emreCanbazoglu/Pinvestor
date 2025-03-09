#if UNITY_EDITOR && EDITOR_REPLACEMENT
using System;
using System.Collections.Generic;

namespace EditorReplacement.Examples
{
    public class EmptyEditorReplacer : EditorReplacer
    {
        public override string SystemKey => "EmptyEditorReplacer";

        protected override void DeepCopyTo(EditorReplacer deepCopy) { }

        public override Type GetEditorType(Type targetType, EditorTreeLocation location)
        {
            return typeof(SpacingFreeWrapperEditor);
        }

        public override IEnumerable<Type> CreateInspectedTypes =>
            new Type[] { typeof(UnityEngine.Object) };

        public override void Replace(EditorTreeDef treeDef)
        {
            treeDef.WrapRoot(this);
        }
    }
}
#endif
