#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using EditorReplacement;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace AssetVariants
{
    internal static class AssetsMenu
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SharedUserDataAssetsMenu.OnPaste += (Object target, AssetImporter importer) =>
            {
                AVSettings.LoadSettings();
                AV.LoadAndValidateAll(target.GUID());
                AVHeader.InitAllTargets(); //AVTargets.ClearAllConsideredWhole();
                for (int i = 0; i < AVTargets.all.Count; i++)
                {
                    var avTargets = AVTargets.all[i];
                    avTargets.ClearOverridesCaching();
                }
            };
        }

        private static List<Object> LoadAllAssetsOfSelected()
        {
            List<Object> assets = new List<Object>();
            var selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                if (selection[i] == null)
                    continue;
                if (!Helper.ValidType(selection[i].GetType()))
                    continue;
                string path = selection[i].Path();
                if (!string.IsNullOrEmpty(path) && !path.EndsWith(".unity"))
                {
                    assets.AddRange(AssetDatabase.LoadAllAssetsAtPath(path));
                    assets.Add(AssetImporter.GetAtPath(path));
                }
            }
            return assets;
        }
        private static bool SelectionContainsValid()
        {
            var selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                if (selection[i] == null)
                    continue;
                if (Helper.ValidType(selection[i].GetType()))
                    return true;
            }
            return false;
        }

        [MenuItem("Assets/Asset Variants/Select All", false, 1000)]
        private static void SelectAll()
        {
            AVUtility.SelectAll();
        }

        [MenuItem("Assets/Asset Variants/Validate All", false, 1095)]
        private static void ValidateAll()
        {
            if (EditorUtility.DisplayDialog("Validate All Asset Variants", "This can take some time. Are you sure?", "Yes", "No"))
            {
                AVUtility.ValidateAll();
            }
        }

        [MenuItem("Assets/Asset Variants/Validate Selected & Family", false, 1097)]
        private static void ValidateSelectedFamilies()
        {
            ValidateSelection(true);
        }
        [MenuItem("Assets/Asset Variants/Validate Selected & Family", true)]
        private static bool ValidateValidateSelectedFamilies()
        {
            return SelectionContainsValid();
        }

        private static void ValidateSelection(bool families)
        {
            AVSettings.LoadSettings();

            List<AV> avs = new List<AV>();
            try
            {
                var assets = LoadAllAssetsOfSelected();
                for (int i = 0; i < assets.Count; i++)
                {
                    var av = AV.Open(assets[i]);

                    void TryParent()
                    {
                        if (families)
                        {
                            var parent = av.LoadParentAsset();
                            if (parent && !assets.Contains(parent))
                                assets.Add(parent);
                        }
                    }

                    TryParent();
                    av.ValidateRelations();
                    TryParent();

                    avs.Add(av);
                }

                for (int i = 0; i < avs.Count; i++)
                {
                    avs[i].childrenNeedUpdate = true;
                    if (avs[i].data.HasParent)
                        avs[i].RevertAssetFromParentAsset();
                }
            }
            finally
            {
                for (int i = 0; i < avs.Count; i++)
                {
                    avs[i].FlushAnyChangesToChildren();
                    avs[i].Dispose();
                }
            }

            AVHeader.InitAllTargets(); // In case ValidateRelations changed parents
        }

        /*
        [MenuItem("Assets/Asset Variants/Validate Selected", false, 1100)]
        private static void ValidateSelected()
        {
            ValidateSelection(false);
        }
        [MenuItem("Assets/Asset Variants/Validate Selected", true)]
        private static bool ValidateValidateSelected()
        {
            return SelectionContainsValid();
        }
        */

        [MenuItem("Assets/Asset Variants/Create Overrides From Differences for All Asset Variants", false, 1200)]
        private static void COFDAll()
        {
            if (EditorUtility.DisplayDialog("Create Overrides From Differences for All Asset Variants", "This can take some time. Are you sure?", "Yes", "No"))
            {
                AVUtility.COFDAll();
            }
        }

        [MenuItem("Assets/Asset Variants/Create Overrides From Differences for Selected", false, 1201)]
        private static void COFDSelection()
        {
            AVSettings.LoadSettings();

            var assets = LoadAllAssetsOfSelected();
            for (int i = 0; i < assets.Count; i++)
            {
                using (var av = AV.Open(assets[i]))
                {
                    if (av != null && av.Valid() && av.data.HasParent)
                        av.CreateOverridesFromDifferences();
                }
            }
        }
        [MenuItem("Assets/Asset Variants/Create Overrides From Differences for Selected", true)]
        private static bool ValidateCOFDSelection()
        {
            return SelectionContainsValid();
        }

        [MenuItem("Assets/Create/Asset Variant", false, 2000)]
        private static void CreateVariant()
        {
            foreach (var o in Selection.objects)
                AVUtility.CreateVariant(o);
        }
        [MenuItem("Assets/Create/Asset Variant", true)]
        private static bool ValidateCreateVariant()
        {
            foreach (var o in Selection.objects)
                if (o != null && Helper.ValidType(o.GetType()))
                    return true;
            return false;
        }
    }

    internal static class ContextMenu
    {
#if UNITY_2019_1_OR_NEWER //? TODO2:
        public const string CONTEXT_FOLDER = "CONTEXT/Object/Asset Variants/";
#else
        public const string CONTEXT_FOLDER = "CONTEXT/Object/";
#endif
        private const int PRIORITY = 10000;

#if EDITOR_REPLACEMENT
        //TODO::: move this to Editor Replacement?
        [MenuItem(CONTEXT_FOLDER + "Log Editor Type", false, PRIORITY + 0)]
        private static void LogEditorType(MenuCommand command)
        {
            if (command.context == null)
            {
                Debug.LogError("command.context == null");
                return;
            }

            Debug.Log("Editor for " + command.context.GetType() + ":" + Helper.LOG_END);
            var editor = Editor.CreateEditor(command.context);
            var contextType = command.context.GetType();
            var foundEditorType = ERManager.FindCustomEditorTypeByType(contextType, false);
            if (editor != null)
            {
                var editorType = editor.GetType();
                if (editorType != foundEditorType)
                    Debug.LogError(editorType + " != " + foundEditorType + Helper.LOG_END);
                Debug.Log(editorType + Helper.LOG_END);
                Object.DestroyImmediate(editor);
            }
            else
            {
                Debug.LogWarning("?");
                Debug.Log(foundEditorType + Helper.LOG_END);
            }
            var originalEditorType = ERManager.OriginalFindCustomEditorTypeByType(contextType, false);
            if (originalEditorType != foundEditorType)
                Debug.Log("ORIGINAL editor type: " + originalEditorType);
        }
#endif

        private static bool Valid(Object o)
        {
            return o != null && Helper.ValidType(o.GetType());
        }

        [MenuItem(CONTEXT_FOLDER + "Fill Root Overrides", false, PRIORITY + 20)]
        private static void FillRootOverrides(MenuCommand command)
        {
            AVUtility.FillRootOverrides(command.context);
            //OverrideButton.AllUpdateStyle(); //TODO::
        }
        [MenuItem(CONTEXT_FOLDER + "Fill Root Overrides", true)]
        private static bool ValidateFillRootOverrides(MenuCommand command)
        {
            return Valid(command.context);
        }

        [MenuItem(CONTEXT_FOLDER + "Clear All Overrides", false, PRIORITY + 21)]
        private static void ClearAllOverrides(MenuCommand command)
        {
            using (var av = AV.Open(command.context))
            {
                if (av == null)
                    return;
                av.data.overrides.Clear();
                av.overridesCache = null;
                av.SaveUserDataWithUndo(true, true);

                Debug.Log("CLEARED ALL OVERRIDES FOR: " + av.asset.Path() + Helper.LOG_END);

                av.RevertAssetFromParentAsset();
            }
        }
        [MenuItem(CONTEXT_FOLDER + "Clear All Overrides", true)]
        private static bool ValidateClearALlOverrides(MenuCommand command)
        {
            return Valid(command.context);
        }

        //[MenuItem(CONTEXT_FOLDER + "Clean Invalid Overrides", false, PRIORITY + 40)]
        //private static void CleanInvalidOverrides(MenuCommand command)
        //{
        //    AVUtility.CleanOverrides(command.context, false);
        //}
        //[MenuItem(CONTEXT_FOLDER + "Clean Invalid Overrides", true)]
        //private static bool ValidateCleanInvalidOverrides(MenuCommand command)
        //{
        //    return Valid(command.context);
        //}

        //[MenuItem(CONTEXT_FOLDER + "Clean Unused Overrides", false, PRIORITY + 41)]
        //private static void CleanUnusedOverrides(MenuCommand command)
        //{
        //    AVUtility.CleanOverrides(command.context, true);
        //}
        //[MenuItem(CONTEXT_FOLDER + "Clean Unused Overrides", true)]
        //private static bool ValidateCleanUnusedOverrides(MenuCommand command)
        //{
        //    return Valid(command.context);
        //}

        [MenuItem(CONTEXT_FOLDER + "Create Fresh Overrides", false, PRIORITY + 40)]
        private static void CleanInvalidOverrides(MenuCommand command)
        {
            AVUtility.CreateFreshOverrides(command.context, true);
        }
        [MenuItem(CONTEXT_FOLDER + "Create Fresh Overrides", true)]
        private static bool ValidateCleanInvalidOverrides(MenuCommand command)
        {
            return Valid(command.context);
        }

        [MenuItem(CONTEXT_FOLDER + "Create Variant Asset", false, PRIORITY + 100)]
        private static void CreateVariant(MenuCommand command)
        {
            AVUtility.CreateVariant(command.context);
        }
        [MenuItem(CONTEXT_FOLDER + "Create Variant Asset", true)]
        private static bool ValidateCreateVariant(MenuCommand command)
        {
            return Valid(command.context);
        }
    }
}
#endif
