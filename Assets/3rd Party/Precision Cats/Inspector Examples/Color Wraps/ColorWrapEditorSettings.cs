#if UNITY_EDITOR && EDITOR_REPLACEMENT
using UnityEngine;
using UnityEditor;
using SystemPrefs;

namespace EditorReplacement.Examples
{
    [CreateAssetMenu(menuName = "System Prefs/ColorWrapEditorSettings", order = 1000000)]
    public class ColorWrapEditorSettings : SystemPrefsState
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SystemPrefsWindow.tabs.Add(new SystemPrefsWindow.StatesTab()
            {
                title = new GUIContent("Color Wraps", "Shows every ColorWrapEditorSettings asset."),
                search = "t:ColorWrapEditorSettings",
                assetTypes = new System.Type[] { typeof(ColorWrapEditorSettings), typeof(SystemPrefsGroup) },
            });
        }


        [Space(5)]
        public WrapType wrapType = WrapType.Defaults;
        [Space(5)]
        public Mode mode = Mode.Both;
        public bool indented = true;
        public Color color = Color.white;
        public int indentation = 1;
        public int leftPadding = 4;
        public int rightPadding = 4;
        public float preChildrenSpacing = 2;
        public float postChildrenSpacing = 2;
        [Space(5)]
        public bool debugTypes = false;

        [System.NonSerialized]
        internal ColorWrapEditorReplacer replacer;


        public enum Mode { Neither = -1, Both = 0, Light = 1, Dark = 2 }


        public override bool Valid
        {
            get
            {
                if (!base.Valid)
                    return false;
                if (mode == Mode.Both)
                    return true;
                if (mode == Mode.Neither)
                    return false;
                bool isDark = EditorGUIUtility.isProSkin;
                if ((mode == Mode.Dark && !isDark) || (mode == Mode.Light && isDark))
                    return false;
                return true;
            }
        }

        internal void Build(bool sortReplacers)
        {
            if (Valid && SystemPrefsUtility.GetActive(SystemKey)) // (Checking for active here is just an early out)
            {
                if (replacer == null)
                {
                    replacer = new ColorWrapEditorReplacer()
                    {
                        settings = this
                    };
                    EditorReplacer.AddCustomSS(replacer, sortReplacers);
                }
            }
        }
        private void Remove()
        {
            if (replacer != null)
                EditorReplacer.RemoveCustomSS(replacer);
            replacer = null;
        }

        private void Rebuild()
        {
            Remove();
            Build(true);

            EditorReplacer.DirtySSes();
        }

        public override void OnChangedDisablePref(bool newDisabled)
        {
            base.OnChangedDisablePref(newDisabled);
            if (!newDisabled)
                Rebuild();
        }
        public override void PrefsDeactivateDisabled()
        {
            if (Disabled && Valid)
            {
                SystemPrefsUtility.SetActive(systemKey, false); //base.PrefsDeactivateDisabled();
                Remove();
            }
        }

        public override void OnSystemPriorityChanged()
        {
            Rebuild();
        }
        public override void PostDuplicationInit(string key)
        {
            base.PostDuplicationInit(key);
            Rebuild();
        }


        protected override void OnValidate()
        {
            base.OnValidate();

            if (Selection.Contains(this))
                Rebuild();
        }

        private void OnDestroy()
        {
            Remove();
        }
    }
}
#endif
