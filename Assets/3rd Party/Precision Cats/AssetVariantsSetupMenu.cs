#if UNITY_EDITOR && ASSET_VARIANTS
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EditorReplacement;
using AssetVariants;
using SystemPrefs;
using System;
using System.Linq;

namespace PrecisionCats
{
    public static class AssetVariantsSetupMenu
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            AssetVariantsSettingsWindow.tabs.Insert(0, new AssetVariantsSetupTab()
            {
                title = new GUIContent("Setup", "Some simple setup options.")
            });
        }

        public class AssetVariantsSetupTab : AssetVariantsSettingsWindow.Tab
        {
            private Vector2 scrollPosition;
            public override void OnGUI(AssetVariantsSettingsWindow window)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                Draw();

                EditorGUILayout.EndScrollView();
            }
        }

        public static float spacing = 10;

        public static void Draw()
        {
#if EDITOR_REPLACEMENT
            if (ERSettings.SIsDefaultOrTemp || AVSettings.SIsDefaultOrTemp)
#else
            if (AVSettings.SIsDefaultOrTemp)
#endif
            {
                EditorGUILayout.HelpBox("You should create/assign your own settings assets first in the ERSettings/AVSettings tabs, otherwise an update to Asset Variants will probably require that you press these setup buttons again.", MessageType.Warning);
                GUILayout.Space(10);
            }

#if EDITOR_REPLACEMENT
            bool er = !ERManager.Disabled;
#endif

#if EDITOR_REPLACEMENT
            if (er)
            {
                if (ERSettings.SIsDefaultOrTemp)
                    EditorGUILayout.HelpBox("There is no custom ERSettings asset set. Modifications to neverUseDefaultMargins will be applied to ERSettings.Default!", MessageType.Warning);
                if (AVSettings.SIsDefaultOrTemp)
                    EditorGUILayout.HelpBox("There is no custom AVSettings asset set. Modifications to defaultRootAlignment will be applied to AVSettings.Default!", MessageType.Warning);
                if (GUILayout.Button(new GUIContent("Setup Minimal", "No color wraps or rainbow, sets the ERSettings asset's neverUseDefaultMargins to false and AVSettings' defaultRootAlignment to true.")))
                {
                    SetupPPSG("Color Wraps 3 - Grayscale", false);

                    SetDefaultMargins(true);

                    FullRebuild();

                    AVCommonHelper.RefreshInspectors(true);
                }
                if (GUILayout.Button(new GUIContent("Setup Grayscale", "Activates the grayscale color wraps preset (Color Wraps 3 - Grayscale). Sets the ERSettings asset's neverUseDefaultMargins to true and AVSettings' defaultRootAlignment to false. No rainbow.")))
                {
                    SetupPPSG("Color Wraps 3 - Grayscale");

                    SetDefaultMargins(false);

                    SelectColorWrapDisablers();

                    FullRebuild();

                    AVCommonHelper.RefreshInspectors(true);
                }
                if (GUILayout.Button(new GUIContent("Setup Random Colorful", "Chooses by random one of the 5 default non-grayscale color wraps presets. Sets the ERSettings asset's neverUseDefaultMargins to true and AVSettings' defaultRootAlignment to false. Yes rainbow.")))
                {
                    string[] names = new string[]
                    {
                    "Color Wraps 1 - Original",
                    "Color Wraps 2 - Sunset",
                    "Color Wraps 4 - Sky",
                    "Color Wraps 5 - Nebulae",
                    "Color Wraps 6 - Color Ring",
                    };
                    SetupPPSG(names[UnityEngine.Random.Range(0, names.Length)]);

                    SetDefaultMargins(false);

                    SelectColorWrapDisablers();

                    FullRebuild();

                    AVCommonHelper.RefreshInspectors(true);
                }
            }
#endif

            GUILayout.Space(spacing);

            const string avWarning = "There is no custom AVSettings asset set. Modifications will be applied to AVSettings.Default!";
            if (AVSettings.SIsDefaultOrTemp)
                EditorGUILayout.HelpBox(avWarning, MessageType.Warning);
            if (GUILayout.Button(new GUIContent("Set for SO and Material only", "Changes a pair of simple whitelist settings for AVEditor. Sets the AVSettings asset's scriptableObjectOnly=true and materialOnly=true.")))
            {
                SetForOnly(true, true);
            }
            if (GUILayout.Button(new GUIContent("Set for SO only", "Changes a pair of simple whitelist settings for AVEditor.\nSets the AVSettings asset's scriptableObjectOnly=true and materialOnly=false.")))
            {
                SetForOnly(true, false);
            }
            if (GUILayout.Button(new GUIContent("Set for Material only", "Changes a pair of simple whitelist settings for AVEditor.\nSets the AVSettings asset's scriptableObjectOnly=false and materialOnly=true.")))
            {
                SetForOnly(false, true);
            }
            if (GUILayout.Button(new GUIContent("Set for All (Default)", "Changes a pair of simple whitelist settings for AVEditor.\nSets the AVSettings asset's scriptableObjectOnly=false and materialOnly=false.")))
            {
                SetForOnly(false, false);
            }

#if EDITOR_REPLACEMENT
            if (er)
            {
                GUILayout.Space(spacing);

                const string bsaper = "BasicSubAssetsParentEditorReplacer";
                if (!SystemPrefsUtility.GetActive(bsaper, false))
                {
                    if (GUILayout.Button("Enable BasicSubAssetsParentEditorReplacer"))
                    {
                        SetDefaultActive(bsaper, true);
                        FullRebuild();
                    }
                }
                else
                {
                    if (GUILayout.Button("Disable BasicSubAssetsParentEditorReplacer"))
                    {
                        SetDefaultActive(bsaper, false);
                        FullRebuild();
                    }
                }
            }
#endif

            GUILayout.Space(spacing);

#if EDITOR_REPLACEMENT
            if (er)
            {
                if (GUILayout.Button("Disable Editor Replacement"))
                {
                    EditorProjectPrefs.SetBool(ERManager.Disable_EditorReplacement_Prefs_Key, true);
                    ERManager.Rebuild();
                }
            }
            else
            {
                if (GUILayout.Button("Enable Editor Replacement"))
                {
                    EditorProjectPrefs.DeleteKey(ERManager.Disable_EditorReplacement_Prefs_Key);
                    ERManager.Rebuild();
                }
            }
#endif

            GUILayout.Space(20);

#if !EDITOR_REPLACEMENT
            if (GUILayout.Button(new GUIContent("Add EDITOR_REPLACEMENT", "Adds the scripting define symbol: EDITOR_REPLACEMENT\nthereby enabling Editor Replacement.")))
                UpdateDefine("EDITOR_REPLACEMENT", true);
#else
            if (GUILayout.Button(new GUIContent("Remove EDITOR_REPLACEMENT", "Removes the scripting define symbol: EDITOR_REPLACEMENT\nthereby disabling Editor Replacement.")))
                UpdateDefine("EDITOR_REPLACEMENT", false);
#endif
        }

        private static void UpdateDefine(string define, bool should)
        {
            var targets = Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(x => x != BuildTargetGroup.Unknown)
                .Where(x => !IsObsolete(x));

            foreach (var target in targets)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

                var list = defines.Split(';', ' ')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                bool dirty = false;
                if (should)
                {
                    if (!list.Contains(define))
                    {
                        list.Add(define);
                        dirty = true;
                    }
                }
                else
                {
                    if (list.Remove(define))
                        dirty = true;
                }

                if (dirty)
                {
                    defines = list.Aggregate((a, b) => a + ";" + b);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                }
            }
        }
        private static bool IsObsolete(BuildTargetGroup group)
        {
            var attrs = typeof(BuildTargetGroup)
                .GetField(group.ToString())
                .GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attrs != null && attrs.Length > 0;
        }

        private static void SetForOnly(bool so, bool material)
        {
            var settings = FindSettings<AVSettings>();
            if (settings != null)
            {
                Undo(settings);
                settings.scriptableObjectOnly = so;
                settings.materialOnly = material;
                settings.OnValidate();
            }
            else
                Debug.LogWarning("");
        }

        private static void FullRebuild()
        {
            //TODO:
            //ColorWrapEditorSettings.FlushRebuildReplacers();
            //ERManager.Rebuild();
        }

        private static bool SetDefaultActive(string name, bool active)
        {
            var state = Find<SystemPrefsState>(name + ".Default");
            if (state == null)
            {
                Debug.LogWarning("?");
                return false;
            }
            state.Disable();
            SystemPrefsUtility.SetActive(state.SystemKey, active);
            Debug.Log((active ? "Activated " : "Deactivated ") + name);
            return true;
        }

        public static void SetupPPSG(string name, bool activateSelf = true)
        {
            var ppsg = Find<SystemPrefsGroup>(name);
            if (ppsg != null)
            {
                if (ppsg.parent != null)
                    for (int i = 0; i < ppsg.parent.children.Count; i++)
                        ppsg.parent.children[i].PrefsDeactivate();
                if (activateSelf)
                {
                    Debug.Log("Set up " + name);
                    ppsg.Enable();
                }
                else
                    ppsg.PrefsDeactivate();
            }
            else
                Debug.LogWarning("");
        }

#if EDITOR_REPLACEMENT
        public static void SetDefaultMargins(bool active)
        {
            var settings = FindSettings<ERSettings>();
            if (settings != null)
            {
                Undo(settings);
                settings.neverUseDefaultMargins = !active;
            }
            else
                Debug.LogWarning("");
        }
#endif

        public static void SelectColorWrapDisablers()
        {
            //TODO:?
            // (Unity seems to be broken though)
            //Selection.objects = FindAll<SystemPrefsGroup>("t:SystemPrefsGroup").ToArray();
        }

        private static void Undo(UnityEngine.Object o)
        {
            UnityEditor.Undo.RecordObject(o, "Setup");
            EditorUtility.SetDirty(o);
        }
        private static T Find<T>(string search) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets(search);
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return null;
        }
        private static T FindSettings<T>() where T : SettingsBase<T>
        {
            // TODO: make it create instead of use default
            SettingsBase<T>.DirtyS();
            return SettingsBase<T>.S;
        }
        //private static List<T> FindAll<T>(string search) where T : Object
        //{
        //    var guids = AssetDatabase.FindAssets(search);
        //    List<T> list = new List<T>();
        //    for (int i = 0; i < guids.Length; i++)
        //    {
        //        var t = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        //        if (t != null)
        //            list.Add(t);
        //    }
        //    return list;
        //}
    }
}
#endif
