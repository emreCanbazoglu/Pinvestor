#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PrecisionCats;

namespace EditorReplacement
{
    [CustomEditor(typeof(ERSettings))]
    public class ERSettingsEditor : Editor
    {
        private static readonly GUIContent rebuildLabel = new GUIContent("Rebuild", "Rebuilds ERManager, and thereby Unity's editor type by inspected type cache.");

        public override void OnInspectorGUI()
        {
            SettingsBaseEditorGUI<ERSettings>.DrawGUI(target);

            var buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (GUI.Button(buttonRect, rebuildLabel))
                ERManager.Rebuild();

            var disabledProp = serializedObject.FindProperty("disabled");
            bool prevDisabled = disabledProp.boolValue;

            DrawDefaultInspector();

            if (target == ERSettings.S && prevDisabled != disabledProp.boolValue)
            {
                if (prevDisabled)
                    ERManager.Rebuild();
                else
                    ERManager.Disable();
            }
        }
    }

    public sealed class ERSettings : SettingsBase<ERSettings>
    {
        static ERSettings()
        {
            defaultGUID = "639cb2c8f187102489b76956d3e58f79";
        }

        public override void OnSettingsChanged()
        {
            ERManager.Rebuild();
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            LoadSettings();
        }

        [Space(5, order = 0)]
        [Header("Filtering", order = 1)]
        [Space(2, order = 2)]

        [Tooltip("Disables Editor Replacement as a whole. For debugging mostly.\n\nNote that EditorProjectPrefs.GetBool(\"EditorReplacement.Disable\", false) also needs to return false, which can be done with the DisableEditorReplacement asset.")]
        public bool disabled = false;

        [Space(2)]
        [Tooltip("Full (namespaced) type names. Affects ERManager.")]
        public StringTypeFilter inspectedTypeFilter = new StringTypeFilter();

        [Space(5, order = 0)]
        [Header("Styling", order = 1)]
        [Space(2, order = 2)]

        [Tooltip("Setting this to true is beneficial for the appearance of color wraps, but if color wraps are not used, then set this to false.")]
        public bool neverUseDefaultMargins = true;
        private static int prevNeverUseDefaultMargins = -1;

        [Space(2)]
        [Tooltip("(Only matters if defaultOnlyIMGUI=false). If this is true then prefab bars are aligned all the way on the left of the inspector window, which is the original behaviour in Unity. Otherwise they will be drawn next to the root property depth.")]
        public bool alignPrefabBars = true;

        [Space(2)]
        [Tooltip("Some vertical spacing used sometimes, to separate distinct Editors in an Editor tree.")]
        public float defaultPreChildrenSpacing = 2;
        public float defaultPostChildrenSpacing = 2;

        [Space(2)]
        [Tooltip("Settings used for SubAssetsEditorReplacers.cs")]
        public SubAssets subAssets = new SubAssets();
        [System.Serializable]
        public class SubAssets
        {
            public bool disabled = false;
            internal static int prevDisabled = -1;

            [Space(2)]
            [Tooltip("Spacing before and after a sub-assets foldout.")]
            public float defaultSpacing = 2;

            [Tooltip("If this is true, then even if there is only one target for a SubAsset (only one main asset is selected) then it will still drawn a foldout for the targets.\nIf this is false then it will draw it to the side instead, inline, saving space.")]
            public bool defaultSingleTargetUsesFoldout = false;

            [Tooltip("The color of the lines that separate subassets.")]
            public Color lineColor = Color.white * 0.5f;
            [Tooltip("The vertical thickness of the lines that separate subassets.")]
            public float lineThickness = 2;
            [Tooltip("The vertical spacing before and after the lines that separate subassets.")]
            public float linePadding = 2;
            [Tooltip("If the sub assets foldout is indented for visual clarity.")]
            public bool indented = true;
        }

        //[Space(2)]
        //[Tooltip("(For UI Toolkit). Sets the EditorGUIUtility.hierarchyMode for a wrapped PropertyField's IMGUIContainer.\n" +
        //"This mostly means that foldouts will not have extra indentation when drawing a custom IMGUI PropertyDrawer inside of UIElements. Unfortunately this also means that foldout arrows (the little triangles) are not clickable, because the VisualElement's rect doesn't contain it.")]
        //public bool propertyIMGUIContainerHierarchyMode = true;

        [Space(5, order = 0)]
        [Header("Advanced", order = 1)]
        [Space(2, order = 2)]

        [Tooltip("If this is true, it will prevent UI Toolkit from being used for WrapperEditors, thereby making the entire inspector draw using IMGUI.")]
        public bool defaultOnlyIMGUI = false;
        private static int prevDefaultOnlyIMGUI = -1;

        private void OnValidate()
        {
            inspectedTypeFilter.ClearTypeFilter();

            EditorApplication.delayCall += () => // Delayed to prevent a Unity freeze in AssetDatabase.FindAssets()/ToArray()/ToArray()/FillSet()/UnionWith()/MoveNext()/<FindAllAssets>MoveNext()/<FindEverywhere>MoveNext()/<FindEverywhere>MoveNext()
            {
                LoadSettings();
                if (this == S)
                {
                    AVCommonHelper.UpdateSetting(subAssets.disabled, ref SubAssets.prevDisabled);

                    AVCommonHelper.UpdateSetting(neverUseDefaultMargins, ref prevNeverUseDefaultMargins, true); //Refreshes selection because I think it's a Unity bug. TODO2:

                    AVCommonHelper.UpdateSetting(defaultOnlyIMGUI, ref prevDefaultOnlyIMGUI);
                }
            };
        }
    }
}
#endif
