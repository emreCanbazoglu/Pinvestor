#if UNITY_EDITOR && EDITOR_REPLACEMENT
using System;
using System.Collections.Generic;

namespace EditorReplacement.Examples
{
    public class BasicSubAssetsParentEditorReplacer : SubAssetsParentEditorReplacer<BasicSubAssetsParentEditorReplacer, BasicSubAssetsChildEditorReplacer>
    {
        static BasicSubAssetsParentEditorReplacer()
        {
            var bl = typeFilter.blacklist;
            bl.Add(AVCommonHelper.FindType("UnityEngine.InputSystem.Editor.InputActionImporter"));
            bl.Add(AVCommonHelper.FindType("UnityEngine.InputSystem.InputActionAsset"));

            // There's just no real reason for it. It already displays the components anyway.
            bl.Add(typeof(UnityEditor.ModelImporter));

            bl.Add(AVCommonHelper.FindType("UnityEditor.TextScriptImporter")); // Basically no reason for it
            bl.Add(typeof(UnityEditor.MonoImporter)); // Basically no reason for it

            // Causes some weird errors, perhaps because of that editor caching a reorderable list or something
            //bl.Add(AVCommonHelper.FindType("UnityEditor.U2D.Animation.SpriteLibrarySourceAssetImporter"));

            // Because property drawer attributes are used with ___Parameters, which they weren't designed for, and cause errors like "type is not a supported color value" or show up as "Use Range with float or int."
            bl.Add(AVCommonHelper.FindType("UnityEngine.Rendering.PostProcessing.PostProcessProfile"));
            bl.Add(AVCommonHelper.FindType("UnityEngine.Rendering.VolumeProfile")); // TODO2: does the same occur with this?

            bl.Add(AVCommonHelper.FindType("UnityEditorInternal.AssemblyDefinitionImporter"));

            typeFilter.Add(PrecisionCats.ProjectSettingsFiltering.filter);
        }

        public static int maxSubAssetsToDisplay = 20;
        public override int MaxSubAssetsToDisplay => maxSubAssetsToDisplay;

        public override string SystemKey => "BasicSubAssetsParentEditorReplacer";

        protected override void DeepCopyTo(EditorReplacer deepCopy) { }

        public override Type GetEditorType(Type targetType, EditorTreeLocation location)
        {
            return typeof(SubAssetsParentEditor);
        }

        protected override EditorTreeNode CreateTemplateChild()
        {
            var template = new EditorTreeNode(new BasicSubAssetsChildEditorReplacer());
            template.children.Add(new EditorTreeNode(null)); // To draw the original
            return template;
        }

        public override IEnumerable<Type> CreateInspectedTypes
        {
            get
            {
                foreach (var type in base.CreateInspectedTypes)
                    yield return type;

                yield return typeof(UnityEngine.Object);
            }
        }
    }

    public class BasicSubAssetsChildEditorReplacer : SubAssetsChildEditorReplacer<BasicSubAssetsParentEditorReplacer>
    {
        public override string SystemKey => null;

        public override Type GetEditorType(Type targetType, EditorTreeLocation location)
        {
            return typeof(SubAssetsChildEditor);
        }
    }
}
#endif
