#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace PrecisionCats
{
    public delegate void OnCreateSettingsEventHandler(string defaultPath, string fullCurrentPath);

    /// <summary>
    /// The non-generic version for its OnCreateSettings to be called from any OnCreateSettings<T>
    /// </summary>
    public static class SettingsBaseEditorGUI
    {
        public static event OnCreateSettingsEventHandler OnCreateSettings;

        internal static void CallOnCreateSettings(string defaultPath, string fullCurrentPath)
        {
            if (OnCreateSettings != null)
                OnCreateSettings(defaultPath, fullCurrentPath);
        }
    }
    public static class SettingsBaseEditorGUI<T> where T : SettingsBase<T>
    {
        public static event OnCreateSettingsEventHandler OnCreateSettings;

        public static void DrawCreationGUI()
        {
            SettingsBase<T>.LoadSettings(!SettingsBase<T>.SIsDefaultOrTemp);

            EditorGUI.BeginChangeCheck();
            var customS = SettingsBase<T>.SIsDefaultOrTemp ? null : SettingsBase<T>.S;
            var newS = EditorGUILayout.ObjectField(new GUIContent("Current Settings"), customS, typeof(T), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (newS == null)
                {
                    EditorProjectPrefs.SetString(SettingsBase<T>.CURRENT_GUID_PREFS_KEY, "");
                    SettingsBase<T>.DirtyS();
                }
                else
                    ((T)newS).SetAsCurrent();
            }

            if (SettingsBase<T>.S == null || SettingsBase<T>.SIsDefaultOrTemp)
            {
                var buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                if (GUI.Button(buttonRect, "Create Settings Asset"))
                {
                    var tn = SettingsBase<T>.typeName;
                    var path = EditorUtility.SaveFilePanelInProject("Create " + tn, tn + ".asset", "asset", "Enter a file name for the new " + tn + ".", "Assets");
                    if (path != "")
                    {
                        ScriptableObject asset;
                        var defaultPath = AssetDatabase.GUIDToAssetPath(SettingsBase<T>.defaultGUID);
                        if (AssetDatabase.CopyAsset(defaultPath, path))
                        {
                            AssetDatabase.Refresh();
                            asset = AssetDatabase.LoadAssetAtPath<T>(path);
                            var importer = AssetImporter.GetAtPath(path);
                            if (importer != null)
                                importer.userData = null; //?
                            //var defaultS = AssetDatabase.LoadAssetAtPath<T>(defaultPath);
                            //if (defaultS != null)
                            //{
                            //    //Undo.RecordObject(defaultS, "Reset");
                            //    var editor = Editor.CreateEditor(defaultS);
                            //    editor.ResetTarget();
                            //    Object.DestroyImmediate(editor);
                            //}
                            //else
                            //    Debug.LogWarning("?");
                        }
                        else
                        {
                            asset = ScriptableObject.CreateInstance(typeof(T));
                            AssetDatabase.CreateAsset(asset, path);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        }

                        if (!string.IsNullOrEmpty(defaultPath))
                        {
                            if (OnCreateSettings != null)
                                OnCreateSettings(defaultPath, path);
                            SettingsBaseEditorGUI.CallOnCreateSettings(defaultPath, path);
                        }

                        EditorGUIUtility.PingObject(asset);
                        Selection.objects = new Object[] { asset };
                        Selection.activeObject = asset;

                        ((T)asset).SetAsCurrent();

                        SettingsBase<T>.DirtyS();
                    }
                }
            }
        }

        public static void DrawGUI(Object target)
        {
            if (((T)target).GUID == SettingsBase<T>.defaultGUID)
            {
                DrawCreationGUI();
                GUILayout.Space(10);
            }
            else
            {
                SettingsBase<T>.DirtyS();

                if (target != SettingsBase<T>.S)
                {
                    GUILayout.Space(10);

                    EditorGUILayout.HelpBox("This is not the current Settings asset", MessageType.Warning);

                    var prevGUIEnabled = GUI.enabled;
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(new GUIContent("Current Settings"), SettingsBase<T>.S, typeof(T), false);
                    GUI.enabled = prevGUIEnabled;

                    GUILayout.Space(5);

                    var buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                    if (GUI.Button(buttonRect, "Set As Current Settings Asset"))
                        ((T)target).SetAsCurrent();

                    GUILayout.Space(10);
                }
            }
        }
    }

    public class SettingsBase<T> : ScriptableObject where T : SettingsBase<T>
    {
        public bool useAsDefaultSettingsAsset = false;

        internal static readonly string typeName = typeof(T).Name;
        public static readonly string CURRENT_GUID_PREFS_KEY = typeName + ".GUID";
        internal static string CurrentGUID => EditorProjectPrefs.GetString(CURRENT_GUID_PREFS_KEY, "");
        internal protected static string defaultGUID;

        public string GUID
        {
            get
            {
                string guid;
                long _;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out _))
                    return guid;
                else
                    return "";
            }
        }

        public void SetAsCurrent()
        {
            var guid = GUID;
            if (GUID == defaultGUID)
            {
                Debug.LogError("Can't set default settings as current settings.");
            }
            else
            {
                EditorProjectPrefs.SetString(CURRENT_GUID_PREFS_KEY, guid);
                DirtyS();
            }
        }

        public virtual void OnSettingsChanged() { }

        private static bool sIsDirty = false;
        public static void DirtyS()
        {
            sIsDirty = true;
        }

        private static bool triedFindingCustomDefaultSettingsAsset = false;

        private static T s;
        public static T S
        {
            get
            {
                if (sIsDirty || ReferenceEquals(s, null))
                    LoadSettings(sIsDirty);
                sIsDirty = false;
                return s;
            }
        }
        public static bool SIsDefaultOrTemp { get; private set; } = false;
        public static void LoadSettings(bool clear = false)
        {
            var prevS = s;

            if (clear)
                s = null;

            if (s == null || SIsDefaultOrTemp)
            {
                var customS = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(CurrentGUID));
                if (customS != null)
                {
                    s = customS;
                    SIsDefaultOrTemp = false;
                }
                else if (s == null)
                {
                    if (!triedFindingCustomDefaultSettingsAsset)
                    {
                        triedFindingCustomDefaultSettingsAsset = true; // So that it won't keep looking if there is none (which would be slow)

                        foreach (var guid in AssetDatabase.FindAssets("t:" + typeName))
                        {
                            var ss = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                            if (ss && ss.useAsDefaultSettingsAsset)
                            {
                                triedFindingCustomDefaultSettingsAsset = false; // (So that if the asset exists the project name can be changed repeatedly)
                                ss.SetAsCurrent();
                                s = ss;
                                break;
                            }
                        }
                    }
                    if (s == null)
                    {
                        s = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(defaultGUID));
                        if (s == null)
                            s = CreateInstance<T>();
                        SIsDefaultOrTemp = true;
                    }
                }
            }

            if (prevS != null && s != prevS)
                s.OnSettingsChanged();
        }
    }
}
#endif
