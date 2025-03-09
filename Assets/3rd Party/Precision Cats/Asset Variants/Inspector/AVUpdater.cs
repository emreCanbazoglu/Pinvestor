#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetVariants
{
    internal class AVAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] _paths)
        {
            List<string> paths = new List<string>(_paths);

            int fadCount = 0;
            {
                List<string> paths2 = new List<string>();
                AV.FlushAllDirty(paths2);
                for (int i = 0; i < paths2.Count; i++)
                {
                    if (!paths.Contains(paths2[i]))
                    {
                        paths.Add(paths2[i]);
                        fadCount++;
                    }
                }
            }
            int uddpCount = 0;
            foreach (var path in AV.userDataDirtyPaths) //?
            {
                if (!paths.Contains(path))
                {
                    paths.Add(path);
                    uddpCount++;
                }
            }
            AV.userDataDirtyPaths.Clear();

            if ((fadCount > 0 || uddpCount > 0) && Helper.LogUnnecessary)
                Debug.Log("Saving " + fadCount + " additional assets due to AV.FlushAllDirty(), and " + uddpCount + " due to AV.userDataDirtyPaths" + Helper.LOG_END);

            return paths.ToArray();
        }
    }

    internal class AVUpdater : AssetPostprocessor
    {
        public static int timestamp;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            ObjectChangeEvents.changesPublished += ChangesPublished;

            EditorApplication.update += Update;

            Undo.undoRedoPerformed += () =>
            {
                undoTimestamp = timestamp;
            };

            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                FlushChangedAssets();
            };
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths
#if UNITY_2021_2_OR_NEWER
            , bool didDomainReload
#endif
        )
        {
            bool cancelable = importedAssets.Length > 100;
            int undoGroup = int.MinValue; //TODO: ?
            for (int i = 0; i < importedAssets.Length; i++)
            {
                var path = importedAssets[i];
                if (path.EndsWith(".unity", System.StringComparison.Ordinal) ||
                    path.EndsWith(".prefab", System.StringComparison.Ordinal) ||
                    path.EndsWith(".fbx", System.StringComparison.Ordinal) ||
                    path.EndsWith(".obj", System.StringComparison.Ordinal) ||
                    path.EndsWith(".blend", System.StringComparison.Ordinal) ||
                    path.EndsWith(".dae", System.StringComparison.Ordinal) ||
                    path.EndsWith(".dxf", System.StringComparison.Ordinal) ||
                    path.EndsWith(".ma", System.StringComparison.Ordinal) ||
                    path.EndsWith(".lwo", System.StringComparison.Ordinal) ||
                    path.EndsWith(".jas", System.StringComparison.Ordinal))
                    continue;
                if (cancelable)
                {
                    if (Helper.LogUnnecessary)
                        Debug.Log("Asset Variants OnPostprocessAllAssets:\n" + path + Helper.LOG_END);
                    if (EditorUtility.DisplayCancelableProgressBar("Asset Variants OnPostprocessAllAssets " + i + "/" + importedAssets.Length + ": " + path, System.IO.Path.GetFileName(path), i / (float)importedAssets.Length))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }
                }
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int ii = 0; ii < assets.Length; ii++)
                    TryAddChangedAsset(assets[ii], undoGroup);
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null && importer.GetType() != typeof(AssetImporter))
                    TryAddChangedAsset(importer, undoGroup);
            }
            if (cancelable)
                EditorUtility.ClearProgressBar();
            FlushChangedAssets();
        }

        public static int undoTimestamp = int.MinValue;

        public static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            int undoGroup = Undo.GetCurrentGroup();

            int l = stream.length;
            for (int i = 0; i < l; i++)
            {
                if (stream.GetEventType(i) == ObjectChangeKind.ChangeAssetObjectProperties)
                {
                    ChangeAssetObjectPropertiesEventArgs args;
                    stream.GetChangeAssetObjectPropertiesEvent(i, out args); //Debug.Log(args.guid + " - " + args.instanceId);

                    var guid = args.guid.ToString();
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    for (int ii = 0; ii < assets.Length; ii++)
                    {
                        //Debug.Log("Undo Group: " + undoGroup + " - For: " + assets[ii]);
                        TryAddChangedAsset(assets[ii], undoGroup);
                    }
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer != null && importer.GetType() != typeof(AssetImporter)) //TODO:?
                        TryAddChangedAsset(importer, undoGroup);
                }
            }

            if (!EditorGUIUtility.editingTextField)
                FlushChangedAssets();
        }

        public static int GetUndoGroup(Object asset)
        {
            for (int i = 0; i < changedAssets.Count; i++)
                if (changedAssets[i].asset == asset)
                    return changedAssets[i].undoGroup;
            return Undo.GetCurrentGroup();
        }

        private struct ChangedAsset
        {
            public Object asset;
            public int undoGroup;
        }
        private static readonly List<ChangedAsset> changedAssets = new List<ChangedAsset>();
        private static bool TryAddChangedAsset(Object asset, int undoGroup)
        {
            for (int i = 0; i < changedAssets.Count; i++)
            {
                if (changedAssets[i].asset == asset)
                    return false;
            }
            changedAssets.Add(new ChangedAsset()
            {
                asset = asset,
                undoGroup = undoGroup
            });
            return true;
        }

        private struct ChangedAV
        {
            public AV av;
            public int undoGroup;
        }
        private static readonly List<ChangedAV> changedAVs = new List<ChangedAV>();
        public static void FlushChangedAssets()
        {
            changedAVs.Clear();
            try
            {
                for (int i = 0; i < changedAssets.Count; i++)
                {
                    var ca = changedAssets[i];
                    var av = AV.Open(ca.asset);
                    if (av != null)
                        changedAVs.Add(new ChangedAV() { av = av, undoGroup = ca.undoGroup });
                }

                for (int i = 0; i < AVTargets.all.Count; i++)
                {
                    var avTargets = AVTargets.all[i];
                    if (avTargets.AVs != null)
                    {
                        for (int ii = 0; ii < avTargets.AVs.Length; ii++)
                        {
                            var av = avTargets.AVs[ii];
                            bool contains = false;
                            for (int iii = 0; iii < changedAVs.Count; iii++)
                            {
                                if (changedAVs[iii].av == av)
                                {
                                    contains = true;
                                    break;
                                }
                            }
                            if (contains)
                            {
                                avTargets.ClearPathConversionCaches();
                                avTargets.TryImmediatelyPropagate();
                                break;
                            }
                        }
                    }
                }

                for (int i = 0; i < changedAVs.Count; i++)
                {
                    var cAV = changedAVs[i];
                    if (cAV.av.Valid())
                    {
                        int rt;
                        if (!AV.revertingTimestamps.TryGetValue(cAV.av.asset, out rt))
                            rt = int.MinValue;
                        //Debug.Log(rt + " - " + timestamp);
                        if (rt + 1 < timestamp && undoTimestamp + 1 < timestamp)
                        {
                            cAV.av.TryDirtyImplicit(cAV.undoGroup);
                            cAV.av.childrenNeedUpdate = true;
                        }
                        //else
                        //Debug.LogWarning("!!!");
                    }
                }
            }
            finally
            {
                changedAssets.Clear();

                for (int i = 0; i < changedAVs.Count; i++)
                    changedAVs[i].av.Dispose();
                changedAVs.Clear();
            }
        }

        public static void Update()
        {
            timestamp++;

            if (!EditorGUIUtility.editingTextField)
                FlushChangedAssets();
        }
    }
}
#endif
