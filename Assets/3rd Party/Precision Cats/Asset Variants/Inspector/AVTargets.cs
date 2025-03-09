#if UNITY_EDITOR

#if !ODIN_INSPECTOR
#undef ASSET_VARIANTS_DO_ODIN_PROPERTIES
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;
using S = AssetVariants.AVSettings;
#if ODIN_INSPECTOR
using Sirenix.Utilities;
using Sirenix.OdinInspector.Editor;
#endif

namespace AssetVariants
{
    internal class AVTargets
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                for (int i = 0; i < all.Count; i++)
                    all[i].users.list.Clear();
                CleanupUsers();
            };

            //////// To prevent implicit overrides from being created before it can flush changes
            //////Selection.selectionChanged += () =>
            //////{
            //////    CleanupUsers();
            //////};

            EditorApplication.update += Update;

            Undo.undoRedoPerformed += () =>
            {
                for (int i = 0; i < all.Count; i++)
                {
                    all[i].rvUndrawnPathsDirty = true;

#if ODIN_INSPECTOR
                    all[i].odinOverrideTree.dirty = true;
#endif
                }
            };
        }

        private readonly WeakList<SerializedObject> users = new WeakList<SerializedObject>();
        private static readonly FieldInfo nativeObjectPtrFI = typeof(SerializedObject).GetField("m_NativeObjectPtr", R.npi);
        public static void CleanupUsers()
        {
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var avTargets = all[i];

                var users = avTargets.users.list;
                for (int ii = users.Count - 1; ii >= 0; ii--)
                {
                    var user = users[ii];
                    SerializedObject so;
                    if (!user.TryGetTarget(out so) || (IntPtr)nativeObjectPtrFI.GetValue(so) == IntPtr.Zero)
                        users.RemoveAt(ii);
                }

                if (avTargets.users.list.Count == 0)
                {
                    all.RemoveAt(i);

                    if (avTargets.AVs != null)
                    {
                        for (int ii = 0; ii < avTargets.AVs.Length; ii++)
                            avTargets.AVs[ii].Dispose(); // Decrements UserCount
                        avTargets.AVs = null;
                    }
                }
            }
        }

        public void UpdateAnyHasParent()
        {
            AnyHasParent = false;
            if (AVs != null)
            {
                for (int i = 0; i < AVs.Length; i++)
                {
                    if (AVs[i].data.HasParent)
                    {
                        AnyHasParent = true;
                        break;
                    }
                }
            }
        }

        public static AVTargets Get(SerializedObject so, bool reloadNullAVs = false)
        {
            AVTargets avTargets = null;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (a.users.Contains(so))
                {
                    if (a.AVs != null || !reloadNullAVs)
                        return a;

                    avTargets = a;
                    break;
                }
            }

            var targets = so.targetObjects;

            if (avTargets == null)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    if (AVCommonHelper.TargetsAreEqual(all[i].targets, targets))
                    {
                        avTargets = all[i];
                        break;
                    }
                }

                if (avTargets == null)
                {
                    avTargets = new AVTargets(targets, null);
                    all.Add(avTargets);
                }

                avTargets.users.Add(so);
            }

            if (avTargets.AVs == null)
            {
                if (EditorApplication.isUpdating)
                    AV.skipValidateRelations = true;

                var avs = new AV[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    avs[i] = AV.Open(targets[i]); // Increments UserCount
                    if (avs[i] == null)
                    {
                        for (int ii = 0; ii < i; ii++)
                            avs[ii].Dispose();
                        avs = null;
                        break;
                    }

                    avs[i].ValidateRelations();
                }
                avTargets.AVs = avs;

                avTargets.UpdateAnyHasParent();

                AV.skipValidateRelations = false;
            }

            return avTargets;
        }

        public bool AnyHasParent { get; set; }

        public bool AnyMissingPath(string path)
        {
            for (int i = 0; i < AVs.Length; i++)
            {
                if (!AVs[i].GetOverridesCache().Contains(path))
                    return true;
            }
            return false;
        }
        public bool AnyContainsPath(string path)
        {
            for (int i = 0; i < AVs.Length; i++)
            {
                if (AVs[i].GetOverridesCache().Contains(path))
                    return true;
            }
            return false;
        }
        public bool AnyContainsChildrenPaths(string path, bool includeSelf)
        {
            for (int i = 0; i < AVs.Length; i++)
            {
                var oc = AVs[i].GetOverridesCache();
                if (oc.FastContainsChildrenPaths(path, includeSelf))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// (RV = Raw View)
        /// </summary>
        public void TryInitRVUndrawnPaths()
        {
            if (!rvUndrawnPathsDirty)
                return;
            rvUndrawnPathsDirty = false;

            rvUnclaimedPaths.Clear();
            rvClaimedUndrawnPaths.Clear();

            for (int i = 0; i < AVs.Length; i++)
            {
                var oc = AVs[i].GetOverridesCache();
                for (int ii = 0; ii < oc.list.Count; ii++)
                {
                    var p = oc.list[ii];
                    if (!rvUnclaimedPaths.Contains(p))
                        rvUnclaimedPaths.Add(p);
                }
            }
        }
        public bool rvUndrawnPathsDirty = true;
        public List<string> rvUnclaimedPaths = new List<string>();
        public Dictionary<string, List<string>> rvClaimedUndrawnPaths = new Dictionary<string, List<string>>();
        public List<string> RVPrepare(string path, bool forceRemoveFromParent)
        {
            if (!rvClaimedUndrawnPaths.TryGetValue(path, out List<string> undrawn))
            {
                undrawn = new List<string>();
                rvClaimedUndrawnPaths[path] = undrawn;
                rvUnclaimedPaths.Remove(path); // Not really necessary though?
                forceRemoveFromParent = true;
            }
            if (forceRemoveFromParent)
            {
                // Can happen if you expand a foldout
                var parentPath = Helper.ParentOverridePath(path, true);
                if (parentPath != null)
                {
                    List<string> parentUndrawn;
                    if (rvClaimedUndrawnPaths.TryGetValue(parentPath, out parentUndrawn))
                    {
                        parentUndrawn.Remove(path);
                        //if (parentUndrawn.Remove(path))
                        //Debug.LogError("Had to remove twice");
                    }
                    //else
                    //Debug.LogError(parentPath);
                }
            }
            return undrawn;
        }
        public void RVClaim(string path, List<string> src, List<string> dst)
        {
            if (src.Count > 0)
            {
                string start = path + ".";
                for (int i = 0; i < src.Count; i++)
                {
                    var p = src[i];
                    if (p.StartsWith(start, StringComparison.Ordinal))
                    {
                        src.RemoveAt(i);
                        i--;
                        dst.Add(p);
                    }
                }
            }
        }

        /// <summary>
        /// Used for knowing if a property is (or is a child of) a property that is considered whole.
        /// I believe this is only ever necessary for querying a property in isolation,
        /// as normally HasChildren() is called when determining if to enter the children properties,
        /// but in the case of OverrideIndicator it can't reliably get skipped if the parent is whole or grandparent or great grandparent etc. is whole;
        /// It doesn't have much control over its sequence and properties can even be skipped altogether in custom Editors,
        /// so there querying must be done on a per property basis, no other state/context is known.
        /// </summary>
        public bool ConsideredWhole(string path, SerializedObject so, bool initializeIfNotAlready = true)
        {
            if (path == null)
                return false;

            if (initializeIfNotAlready && !filledPropertyPathsConsideredWhole)
            {
                FillPropertyPathsConsideredWhole(so);
                return propertyPathsConsideredWhole[path];
            }

            if (!propertyPathsConsideredWhole.TryGetValue(path, out bool consideredWhole))
            {
                // FindProperty() is slow but I believe unavoidable. Shouldn't happen too often if FillPathsConsideredWhole() is called to initialize the Dictionary.
                //TODO: is there really no internal serializedProperty.GetParentProperty() or similar?
                var prop = so.FindProperty(path);
                if (prop != null)
                {
                    consideredWhole = FindConsideredWhole(prop, path);
                    prop.Dispose();
                }
                else
                    Debug.LogWarning(path);
            }
            return consideredWhole;
        }
        private bool FindConsideredWhole(SerializedProperty prop, string propertyPath)
        {
            bool consideredWhole;
            if (!Helper.HasChildren(prop, false))
                consideredWhole = true;
            else
                consideredWhole = ConsideredWhole(Helper.ParentPropertyPath(propertyPath), prop.serializedObject);
            propertyPathsConsideredWhole[propertyPath] = consideredWhole;
            return consideredWhole;
        }
        public readonly Dictionary<string, bool> propertyPathsConsideredWhole = new Dictionary<string, bool>();
        private bool filledPropertyPathsConsideredWhole = false;
        public void FillPropertyPathsConsideredWhole(SerializedObject so)
        {
            filledPropertyPathsConsideredWhole = true;

            var prop = so.GetIterator();
            if (prop.NextVisible(true))
            {
                string parentPath = null;
                while (true)
                {
                    var path = prop.propertyPath;

                    //Debug.Log(path + " // " + parentPath);

                    if (!propertyPathsConsideredWhole.ContainsKey(path))
                    {
                        propertyPathsConsideredWhole[path] = (parentPath != null && propertyPathsConsideredWhole[parentPath])
                            || !Helper.HasChildren(prop, false);
                    }

                    int preDepth = prop.depth;

                    if (!prop.Next(true))
                        break;

                    int postDepth = prop.depth;

                    if (preDepth + 1 == postDepth)
                        parentPath = path;
                    else
                        parentPath = Helper.ParentPropertyPath(prop.propertyPath);
                }
            }
            prop.Dispose();
        }
        public static void ClearAllConsideredWhole()
        {
            for (int i = 0; i < all.Count; i++)
            {
                all[i].propertyPathsConsideredWhole.Clear();
                all[i].filledPropertyPathsConsideredWhole = false;
            }
        }

        internal static readonly List<AVTargets> all = new List<AVTargets>();

        private AVTargets(Object[] targets, AV[] avs)
        {
            this.targets = targets;

            AVs = avs;

            var targetType = targets[0].GetType();
            propertyFilter = PropertyFilter.Get(targetType);
        }
        ~AVTargets()
        {
            int userCount = users.list.Count;
            if (userCount != 0)
                Debug.LogError("AVTargets - (userCount != 0) - Implementation bug, fix it Steffen - userCount: " + userCount + Helper.LOG_END);
        }

        public readonly Object[] targets;
        public AV[] AVs { get; private set; }
        public readonly PropertyFilter propertyFilter;

        public bool Contains(AV av)
        {
            for (int i = 0; i < AVs.Length; i++)
                if (AVs[i] == av)
                    return true;
            return false;
        }

#if ODIN_INSPECTOR
        public OverrideTree odinOverrideTree = new OverrideTree();
#endif

        public void ClearOverridesCaching()
        {
#if ODIN_INSPECTOR
            odinOverrideTree.dirty = true;
            //Debug.Log("Dirtied tree");
#endif
            ClearPathConversionCaches();
            rvUndrawnPathsDirty = true;
        }

#if ODIN_INSPECTOR
        public string GetOverridePath(InspectorProperty prop, SerializedObject so, bool neverNull = true)
        {
            if (prop.Info.SerializationBackend == SerializationBackend.Odin)
            {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                string discard;
                return OdinGetOverridePath(prop, out discard);
#else
                return null;
#endif
            }
            else
                return ConvertToOverridePath(prop.UnityPropertyPath, so, neverNull);
        }
#endif

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
        public Dictionary<string, string> odinPropertyPathToOverridePath = new Dictionary<string, string>();
        /// <summary>
        /// For Odin serialized, i.e. cannot be emulations
        /// </summary>
        public string OdinGetOverridePath(InspectorProperty prop, out string propertyPath)
        {
            propertyPath = prop.UnityPropertyPath;

            string overridePath;
            if (odinPropertyPathToOverridePath.TryGetValue(propertyPath, out overridePath))
                return overridePath;

            var parent = prop.Parent;
            if (parent != null)
            {
                string parentPropertyPath;
                var parentOverridePath = OdinGetOverridePath(parent, out parentPropertyPath);

                if (parentPropertyPath != "Array.data[]ROOT")
                {
                    //Debug.Log(propertyPath + " " + parentPropertyPath);
                    var localPath = propertyPath.Substring(parentPropertyPath.Length); // Including the '.'

                    var parentType = prop.ParentType;
                    if (parentType != null && parentType.InheritsFrom(typeof(HashSet<>)))
                        localPath = Helper.GetHashSetLocalOverridePath(prop);

                    overridePath = parentOverridePath + localPath;
                }
                else
                    overridePath = propertyPath;
            }
            else
                overridePath = propertyPath;

            odinPropertyPathToOverridePath[propertyPath] = overridePath;
            return overridePath;
        }
#endif

        public void ClearPathConversionCaches()
        {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            odinPropertyPathToOverridePath.Clear();
#endif

            for (int i = 0; i < pathConversionCaches.list.Count; i++)
                pathConversionCaches.list[i].value.ClearConversions();
        }
        public WeakTable<SerializedObject, PathConversionCache> pathConversionCaches = new WeakTable<SerializedObject, PathConversionCache>();
        public PathConversionCache GetPathConversionCache(SerializedObject so)
        {
            PathConversionCache pcc;
            if (pathConversionCaches.TryGetValue(so, out pcc))
                return pcc;
            pcc = new PathConversionCache();
            pathConversionCaches.Add(so, pcc);
            return pcc;
        }
        public string ConvertToOverridePath(string propertyPath, SerializedObject so, bool neverNull = true)
        {
            //return propertyPath;
            var path = GetPathConversionCache(so).ConvertToOverridePath(propertyPath, so, propertyFilter);
            if (path == null && neverNull)
                path = propertyPath;
            return path;
        }
        public string ConvertToPropertyPath(string overridePath, SerializedObject so, bool neverNull = true)
        {
            //return overridePath;
            var propertyPath = GetPathConversionCache(so).ConvertToPropertyPath(overridePath, so, propertyFilter);
            if (propertyPath == null && neverNull)
                propertyPath = overridePath;
            return propertyPath;
        }

        private static readonly List<AV> descendantAVs = new List<AV>();
        private static void FindDescendantChain(AV descendantAV, AV av)
        {
            if (descendantAV.UserCount > 0)
            {
                var search = descendantAV;
                search.IncrementUserCount();
                while (search.Valid() && search != av)
                {
                    // Remove previous duplicates
                    while (descendantAVs.Remove(search)) { }

                    descendantAVs.Add(search);

                    search = AV.Open(search.LoadParentAsset());
                    if (search == null)
                    {
                        Debug.LogError("Traversed through " + descendantAV.asset.name + "'s ancestors and couldn't find " + av.asset.name + Helper.LOG_END);
                        return;
                    }
                }
                search.Dispose();
            }
            else
                Debug.LogWarning("?");
        }
        public void ImmediatelyPropagate()
        {
            descendantAVs.Clear();

            for (int i = 0; i < AVs.Length; i++)
            {
                var av = AVs[i];

                var descendants = av.GetDescendants();

                for (int ii = 0; ii < all.Count; ii++)
                {
                    var avTargets = all[ii];
                    if (avTargets != this)
                    {
                        for (int iii = 0; iii < avTargets.targets.Length; iii++)
                        {
                            if (descendants.Contains(avTargets.targets[iii]))
                                FindDescendantChain(avTargets.AVs[iii], av);
                        }
                    }
                }

                av.childrenNeedUpdate = true; // (Just for safety in case this is called elsewhere)
            }

            // Inverse order so that parents revert before children
            for (int i = descendantAVs.Count - 1; i >= 0; i--)
            {
                var descendantAV = descendantAVs[i];

                // This is fine because it's a descendant of an AV that certainly is childrenNeedUpdate=true.
                // This prevents dAV from immediately propagating to most/all of its descendants if it's only a middleman from av to descendantAV, with no previous UserCount.
                // That means only the necessary sequences of descendants get updated for now.
                bool prevChildrenNeedUpdate = descendantAV.childrenNeedUpdate;
                {
                    descendantAV.RevertAssetFromParentAsset();
                }
                descendantAV.childrenNeedUpdate = prevChildrenNeedUpdate;

                descendantAV.Dispose();
            }

            if ((descendantAVs.Count > 0) && Helper.LogUnnecessary)
                //Debug.Log(AVs[0].asset.name + " - " + AVUpdater.timestamp + "Immediately propagated to " + descendantAVs.Count + " necessary descendants." + Helper.LOG_END);
                Debug.Log("Immediately propagated to " + descendantAVs.Count + " necessary descendants." + Helper.LOG_END);

            descendantAVs.Clear();
        }
        public void TryImmediatelyPropagate()
        {
            if (S.S.immediatelyPropagateToOpenDescendants)
                ImmediatelyPropagate();
        }

        private static void Update()
        {
            
// #if DEBUG_ASSET_VARIANTS
//             string log = "";
//             for (int i = 0; i < all.Count; i++)
//             {
//                 if (i != 0) log += " + ";
//                 log += all[i].users.list.Count;
//             }
//             Debug.Log(users.Count + " = " + log + Helper.LOG_END);
// #endif

            CleanupUsers();

            for (int i = 0; i < all.Count; i++)
            {
                var avTargets = all[i];
                if (avTargets.AVs != null)
                {
                    for (int ii = 0; ii < avTargets.AVs.Length; ii++)
                        avTargets.AVs[ii].FlushOverridesCacheDirty();
                }
            }
        }

        public void SaveAllUserData()
        {
            Profiler.BeginSample("SaveUserDataWithUndo");
            for (int i = 0; i < AVs.Length; i++)
                AVs[i].SaveUserDataWithUndo();
            Profiler.EndSample();
        }

        public int GetOverriddenType(string path)
        {
            int overrideCount = 0;
            bool mixed = false;
            for (int i = 0; i < AVs.Length; i++)
            {
                if (!AVs[i].data.HasParent)
                {
                    mixed = true;
                    break;
                }
                if (AVs[i].GetOverridesCache().Contains(path))
                    overrideCount++;
                if (overrideCount != 0 && overrideCount != (i + 1))
                {
                    mixed = true;
                    break;
                }
            }

            if (mixed)
            {
                return -1;
            }
            else if (overrideCount == 0)
            {
                for (int i = 0; i < AVs.Length; i++)
                {
                    if (AVs[i].overridesCache.parentPaths.Contains(path))
                        return 2;
                }
                return 0;
            }
            else
            {
                return 1;
            }
        }
        public void ToggleOverriddenState(string path, bool overridden)
        {
            if (overridden)
            {
                if (Helper.LogUnnecessary)
                    Debug.Log("Removing override for: " + path + Helper.LOG_END);
            }
            else
            {
                if (Helper.LogUnnecessary)
                    Debug.Log("Creating override for: " + path + Helper.LOG_END);
            }

            for (int i = 0; i < AVs.Length; i++)
            {
                if (AVs[i].data.HasParent)
                {
                    var oc = AVs[i].GetOverridesCache();

                    oc.Remove(path);

                    if (!overridden) // Invert the current state
                    {
                        // Creating a new override
                        if (!S.currentlyAllowSubOverrides())
                            oc.RemoveChildrenPaths(path);
                        oc.Add(path);
                    }
                }
                else
                    Debug.LogWarning("Tried to set override state of property in non-variant asset." + Helper.LOG_END);
            }

            SaveAllUserData();

            if (RawViewWindow.CurrentlyDrawing)
                OverrideIndicator.OverridesMaybeChanged();

            if (overridden)
            {
                // If was overridden but no longer is, needs to revert
                Profiler.BeginSample("RevertAssetFromParentAsset");
                for (int i = 0; i < AVs.Length; i++)
                {
                    var av = AVs[i];
                    if (av.asset == null)
                    {
                        Debug.LogError(av.assetID.guid.ToPath() + " couldn't RevertAssetFromParentAsset() because its asset is reimporting." + Helper.LOG_END);
                        continue;
                    }
                    if (av.data.HasParent)
                        av.RevertAssetFromParentAsset(); //EditorUtility.SetDirty(targets[i]);

                    av.childrenNeedUpdate = true;
                }
                Profiler.EndSample();

                TryImmediatelyPropagate();

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); //TODO: does this need to be in this if scope?
            }
        }

        //TODO: remove?
        public bool TryCreateImplicitOverride(string path)
        {
            bool dirty = false;

            for (int i = 0; i < AVs.Length; i++)
            {
                if (AVs[i].data.HasParent)
                {
                    var oc = AVs[i].GetOverridesCache();
                    if (!oc.IsOverridden(path))
                    {
                        oc.Add(path);
                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                if (Helper.LogUnnecessary)
                    Debug.Log("Implicitly created override for: " + path + Helper.LOG_END);

                SaveAllUserData();
            }

            return dirty;
        }
    }
}
#endif
