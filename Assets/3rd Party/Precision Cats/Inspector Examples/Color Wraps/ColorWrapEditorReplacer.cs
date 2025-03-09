#if UNITY_EDITOR && EDITOR_REPLACEMENT
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace EditorReplacement.Examples
{
    public class ColorWrapEditor : WrapperEditor
    {
        public ColorWrapEditorSettings settings;

        protected override float PreChildrenSpacing => ReferenceEquals(null, settings) ? 0 : settings.preChildrenSpacing;
        protected override float PostChildrenSpacing => ReferenceEquals(null, settings) ? 0 : settings.postChildrenSpacing;

        protected override bool EditorAlwaysEditable => true; // To prevent an unnecessary call to Helper.IsEditable()

        private GUIStyle style;

        protected override void DoOnInspectorGUI()
        {
            if (settings == null)
            {
                LogNullSettings();
                ChildrenOnInspectorGUI();
                return;
            }

            if (settings.debugTypes)
            {
                GUILayout.Label("(IMGUI) - Target Type: " + target.GetType().FullName);
                for (int i = 0; i < ChildrenEditors.Count; i++)
                    GUILayout.Label("- Child Editor Type: " + ChildrenEditors[i].GetType().FullName);
            }

            if (style == null)
            {
                style = new GUIStyle();
                style.padding.left = settings.leftPadding;
                style.padding.right = settings.rightPadding;
            }

            bool final = (ChildrenEditors.Count > 0) && !(ChildrenEditors[0] is ColorWrapEditor); // (ChildrenEditors[0] is MaterialEditor); //TODO: are there any other editors that have this problem?

            var rect = EditorGUILayout.BeginVertical(style);
            {
                if (final)
                    GUILayout.BeginVertical();

                if (settings.indented)
                {
                    rect = EditorGUI.IndentedRect(rect);
                    rect.width += EditorGUI.indentLevel; // * 0.5f // Prevents ugly pixel bias by offsetting in the other direction
                }

                EditorGUI.DrawRect(rect, settings.color);

                int prevIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel += settings.indentation;
                {
                    ChildrenOnInspectorGUI();
                }
                EditorGUI.indentLevel = prevIndentLevel;

                if (final)
                    GUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void LogNullSettings()
        {
#if DEBUG_ASSET_VARIANTS
            Debug.LogError("(settings == null) for: " + target + " - If you want to use color wraps you need to probably recompile/restart, otherwise you can use Window/Asset Variants/Setup Minimal to disable color wraps.");
#endif
        }

#if UNITY_2019_1_OR_NEWER
        private static readonly float kIndentPerLevel = (float)typeof(EditorGUI).GetField("kIndentPerLevel", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); //15;

        protected override void FillInspectorGUI(VisualElement container)
        {
            if (settings == null)
            {
                LogNullSettings();
                ChildrenCreateInspectorGUI(container);
                return;
            }

            if (settings.debugTypes)
            {
                container.Add(new Label() { text = "(UI Toolkit) - Target Type: " + target.GetType().FullName });
                for (int i = 0; i < ChildrenEditors.Count; i++)
                    container.Add(new Label() { text = "- Child Editor Type: " + ChildrenEditors[i].GetType().FullName });
            }

            container.style.backgroundColor = settings.color; //container.style.opacity = 1;

            container.style.paddingLeft = settings.leftPadding
                + settings.indentation * kIndentPerLevel;
            container.style.paddingRight = settings.rightPadding;

            ChildrenCreateInspectorGUI(container);
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();

            style = null;
        }
    }

    /// <summary>
    /// (typeFilter is for inspectedType)
    /// </summary>
    public class ColorWrapEditorReplacer : RemapFilteredEditorReplacer<ColorWrapEditorReplacer>
    {
        static ColorWrapEditorReplacer()
        {
            typeFilter.blacklist.Add(typeof(ParticleSystem));

            typeFilter.Add(PrecisionCats.ProjectSettingsFiltering.filter);
        }

        public ColorWrapEditorSettings settings;

        public override string SystemKey => settings.SystemKey;

        protected virtual WrapType WrapType => settings.wrapType;

        public override void OnCreatedEditor(Editor editor)
        {
            ((ColorWrapEditor)editor).settings = settings;
        }

        protected override bool CreateDefault => false;

        public override Type GetEditorType(Type targetType, EditorTreeLocation location)
        {
            Type editorType;
            if (TryGetRemappedEditorType(targetType, out editorType))
                return editorType;
            return typeof(ColorWrapEditor);
        }

        public override void Replace(EditorTreeDef treeDef)
        {
            if (typeFilter.ShouldIgnore(treeDef.inspectedType))
                return;

            treeDef.Wrap(this, WrapType);
        }

        protected override void DeepCopyTo(EditorReplacer deepCopy)
        {
            ((ColorWrapEditorReplacer)deepCopy).settings = settings;
        }

        public override IEnumerable<Type> CreateInspectedTypes =>
            new Type[] { typeof(UnityEngine.Object) };

        public static void Load()
        {
            var all = SystemPrefs.SystemPrefsUtility.FindAssets<ColorWrapEditorSettings>();

#if UNITY_2019_3_OR_NEWER
            if (all.Count == 0)
            {
                // Because when first importing Asset Variants (and ASSET_VARIANTS is not defined yet),
                // the ColorWrapEditorSettings haven't been imported yet because ColorWrapEditorSettings.cs was only just compiled.
                // This is simply here to prevent the user from having to recompile the first time in order for the color wraps to show up.

                const string KEY = "ColorWrapEditorReplacer.Recompile";
#if !ASSET_VARIANTS
                EditorPrefs.SetBool(KEY, true);
#else
                if (EditorPrefs.HasKey(KEY))
                {
                    EditorPrefs.DeleteKey(KEY);

                    EditorApplication.delayCall += () =>
                    {
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                    };
                }
#endif

                return;
            }
#endif

            for (int i = 0; i < all.Count; i++)
                all[i].Build(false);

            SortSSes();
        }

        [InitializeBeforeEditorReplacement]
        private static void Init()
        {
            Load();
        }
    }
}
#endif
