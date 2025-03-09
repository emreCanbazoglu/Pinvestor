#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PrecisionCats;
using SystemPrefs;

namespace AssetVariants
{
    public class AssetVariantsSettingsWindow : EditorWindow
    {
        [MenuItem("Window/Asset Variants", priority = 1000000)]
        private static void Init()
        {
            AssetVariantsSettingsWindow window = (AssetVariantsSettingsWindow)GetWindow(typeof(AssetVariantsSettingsWindow), false, "Asset Variants");
            window.Show();
        }

        [SerializeField]
        private AssetVariantsCurrentTab currentTab = new AssetVariantsCurrentTab();

        public virtual void OnGUI()
        {
            Draw(this, ref currentTab);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            Undo.undoRedoPerformed += OnUndo;
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
        }
        private void OnUndo()
        {
            Repaint();
        }

        public static float tabHeight = 22;
        public static float postTabSpacing = 10;

        public static List<Tab> tabs = new List<Tab>()
        {
            new SystemPrefsTab()
            {
                title = new GUIContent("System Prefs", "The same as Window/System Prefs.\n\n" + SystemPrefsWindow.help)
            },
#if EDITOR_REPLACEMENT
            new SettingsTab<EditorReplacement.ERSettings>()
            {
                title = new GUIContent("ERSettings", "The settings for Editor Replacement (and Asset Variants).")
            },
#endif
            new SettingsTab<AVSettings>()
            {
                title = new GUIContent("AVSettings", "The settings for Asset Variants.")
            },
        };

        public abstract class Tab
        {
            public GUIContent title;

            public virtual void Open() { }
            public virtual void Close() { }
            public abstract void OnGUI(AssetVariantsSettingsWindow window);
        }
        public abstract class EditorTab : Tab
        {
            protected Editor editor;

            private Vector2 scrollPosition;

            protected virtual bool DrawTop() { return true; }
            protected abstract Object Target { get; }

            public override void Close()
            {
                if (editor != null)
                    DestroyImmediate(editor);
                editor = null;
            }
            public override void OnGUI(AssetVariantsSettingsWindow window)
            {
                var prevHM = EditorGUIUtility.hierarchyMode;
                EditorGUIUtility.hierarchyMode = true;
                try
                {
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                    if (DrawTop())
                    {
                        var t = Target;

                        if (editor != null && editor.target != t)
                        {
                            DestroyImmediate(editor);
                            editor = null;
                        }
                        if (editor == null)
                            editor = Editor.CreateEditor(t);

                        bool margins = editor.UseDefaultMargins();
                        if (margins)
                            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                        editor.OnInspectorGUI();
                        if (margins)
                            EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.EndScrollView();
                }
                finally
                {
                    EditorGUIUtility.hierarchyMode = prevHM;
                }
            }
        }
        public class SettingsTab<T> : EditorTab where T : SettingsBase<T>
        {
            protected override bool DrawTop()
            {
                SettingsBaseEditorGUI<T>.DrawCreationGUI();
                GUILayout.Space(10);

                return !SettingsBase<T>.SIsDefaultOrTemp;
            }
            protected override Object Target => SettingsBase<T>.S;
        }

        [SerializeField]
        private int spwSerializedTabID = -1;

        public class SystemPrefsTab : Tab
        {
            protected SystemPrefsWindow spw;

            public override void Close()
            {
                if (spw != null)
                    DestroyImmediate(spw);
                spw = null;
            }
            public override void OnGUI(AssetVariantsSettingsWindow window)
            {
                if (spw == null)
                {
                    spw = (SystemPrefsWindow)CreateInstance(typeof(SystemPrefsWindow));
                    if (window != null)
                    {
                        var id = window.spwSerializedTabID;
                        if (id >= 0 && id < SystemPrefsWindow.tabs.Count)
                            spw.SwitchToTab(SystemPrefsWindow.tabs[id]);
                    }
                }

                spw.OnGUI();

                if (spw.needsRepaint)
                {
                    spw.needsRepaint = false;
                    if (window != null)
                        window.Repaint();
                }

                if (window != null)
                    window.spwSerializedTabID = spw.SerializedTabID;
            }
        }

        private static GUIContent[] titles = new GUIContent[0];

        public static Color erDisabledColor = Color.red;
        public static Vector2 erDisabledExpand = new Vector2(2, 2);

        public static void CloseAll()
        {
            for (int i = 0; i < tabs.Count; i++)
                tabs[i].Close();
        }
        public static void Draw(AssetVariantsSettingsWindow window, ref AssetVariantsCurrentTab currentTab)
        {
            if (currentTab.tab == null && currentTab.id != -1)
            {
                if (currentTab.id >= 0 && currentTab.id < tabs.Count)
                {
                    currentTab.tab = tabs[currentTab.id];
                    currentTab.tab.Open();
                }
                currentTab.id = -1;
            }

            //#if EDITOR_REPLACEMENT
            //            if (EditorReplacement.ERManager.Disabled)
            //            {
            //                var erdr = AVCommonHelper.GetFlatRect();
            //                erdr.height = tabHeight;
            //                erdr.position -= erDisabledExpand;
            //                erdr.size += 2 * erDisabledExpand;
            //                EditorGUI.DrawRect(erdr, erDisabledColor);
            //            }
            //#endif

            if (tabs.Count == 0)
            {
                Debug.LogWarning("?");
                return;
            }

            var prevTab = currentTab.tab;

            if (titles.Length != tabs.Count)
                titles = new GUIContent[tabs.Count];
            for (int i = 0; i < tabs.Count; i++)
                titles[i] = tabs[i].title;
            var tabID = Mathf.Max(0, tabs.IndexOf(currentTab.tab));
            currentTab.tab = tabs[GUILayout.Toolbar(tabID, titles, GUILayout.Height(tabHeight))];

            if (currentTab.tab != prevTab)
            {
                if (prevTab != null)
                    prevTab.Close();
                currentTab.tab.Open();
            }

            GUILayout.Space(postTabSpacing);

            currentTab.id = tabs.IndexOf(currentTab.tab);

            currentTab.tab.OnGUI(window);
        }
    }

    [System.Serializable]
    public class AssetVariantsCurrentTab
    {
        internal AssetVariantsSettingsWindow.Tab tab = null;
        [SerializeField]
        internal int id = -1;
    }

    internal class AssetVariantsSettingsProvider : SettingsProvider
    {
        public AssetVariantsSettingsProvider(string path, SettingsScope scope) : base(path, scope) { }

        public override void OnDeactivate()
        {
            AssetVariantsSettingsWindow.CloseAll();
        }

        [SerializeField]
        private AssetVariantsCurrentTab currentTab = new AssetVariantsCurrentTab();

        public override void OnGUI(string searchContext)
        {
            AssetVariantsSettingsWindow.Draw(null, ref currentTab);
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            return new AssetVariantsSettingsProvider("Project/Asset Variants", SettingsScope.Project);
        }
    }
}
#endif
