#if UNITY_EDITOR

#if !ODIN_INSPECTOR
#undef ASSET_VARIANTS_DO_ODIN_PROPERTIES
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace AssetVariants
{
    public static class AVUtility
    {
        public static void CreateOverridesFromDifferences(Object asset)
        {
            using (var av = AV.Open(asset))
            {
                if (av.Valid())
                {
                    av.CreateOverridesFromDifferences();
                }
            }
        }

        public static void ValidateRelations(Object asset)
        {
            using (var av = AV.Open(asset))
            {
                if (av.Valid())
                {
                    av.ValidateRelations();
                }
            }
        }

        public static void PropagateChangesToChildren(Object asset)
        {
            using (var av = AV.Open(asset))
            {
                if (av.Valid())
                {
                    av.PropagateChangesToChildren();
                }
            }
        }

        /*
        /// <summary>
        /// Removes override paths that don't have a SerializedProperty in the asset.
        /// If removeIfMatch=true, it also removes unnecessary overrides for properties that have the same value.
        /// There are also context menu options that you can have call this.
        /// </summary>
        public static void CleanOverrides(Object asset, bool removeIfMatch)
        {
            throw new System.Exception("Not yet properly implemented");

            using (var av = AV.Open(asset))
            {
                if (av == null || av.data.overrides.Count == 0)
                    return;

                SerializedObject serializedObject = new SerializedObject(asset);
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                PropertyTree tree = asset.IsOdinSerializable() ? PropertyTree.Create(asset) : null;
#endif

                SerializedObject parentSO = null;
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                PropertyTree parentTree = null;
#endif
                var parentAsset = av.LoadParentAsset();
                if (parentAsset == null)
                    throw new System.Exception("No parent");
                parentSO = new SerializedObject(parentAsset);
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                if (tree != null)
                    parentTree = PropertyTree.Create(parentAsset);
#endif

                var propertyFilter = PropertyFilter.Get(asset.GetType());
                var parentPropertyFilter = PropertyFilter.Get(parentAsset.GetType());

                var material = asset as Material;
                bool isMaterial = material != null;

                var parentConversion = new PathConversionCache();
                var conversion = new PathConversionCache();

                bool dirty = false;
                for (int ii = 0; ii < av.data.overrides.Count; ii++)
                {
                    string o = av.data.overrides[ii];

                    var propertyPath = conversion.ConvertToPropertyPath(o, serializedObject, propertyFilter);

                    var unityProp = propertyPath != null ? serializedObject.FindProperty(propertyPath) : null;
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    InspectorProperty odinProp = null;
                    if (tree != null)
                    {
                        odinProp = tree.GetPropertyAtUnityPath(o);
                        if (odinProp == null)
                        {
                            string basePath = null;
                            if (o.CustomEndsWith(Helper.ARRAY_SIZE_SUFFIX))
                                basePath = o.Substring(0, o.Length - Helper.ARRAY_SIZE_SUFFIX.Length);
                            else if (o.CustomEndsWith(".DictionarySize"))
                                basePath = o.Substring(0, o.Length - 15);

                            if (basePath != null)
                                odinProp = tree.GetPropertyAtUnityPath(basePath);
                        }
                    }
#endif

                    bool ignore = propertyFilter.ShouldIgnore(null, o);

                    if (!ignore && removeIfMatch)
                    {
                        if (unityProp != null)
                        {
                            var parentUnityProp = parentSO.FindProperty(o);
                            if (parentUnityProp != null && Helper.AreEqual(parentUnityProp, unityProp))
                                ignore = true;
                        }
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                        else if (odinProp != null)
                        {
                            var parentOdinProp = parentTree.GetPropertyAtUnityPath(o);
                            if (parentOdinProp != null && Helper.AreEqual(parentOdinProp, odinProp))
                                ignore = true;
                        }
#endif
                    }

                    bool ignoredUnity = unityProp == null || !unityProp.editable || propertyFilter.ShouldIgnore(unityProp.name, null);
                    bool ignoredMaterial = true;
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    bool ignoredOdin = odinProp == null || !odinProp.Info.IsEditable || propertyFilter.ShouldIgnore(odinProp.Name, null);
#endif

                    if (o[o.Length - 1] == '}') // Might be an emulation's child
                    {
                        if (o[o.Length - 2] == '\"') // Might be a string hashset's child
                        {

                        }

                        var parentPropertyPath = parentConversion.ConvertToPropertyPath(o, parentSO, propertyFilter);
                        var parentUnityProp = parentPropertyPath != null ? parentSO.FindProperty(o) : null;

                    }

                    // This is very simplified, it does not check for subpaths existing, or compare values
                    if (isMaterial && o.StartsWith("M.", System.StringComparison.Ordinal))
                    {
                        var matPath = o.Substring(2);
                        int index = matPath.IndexOf('.');
                        if (index != -1)
                            matPath = matPath.Substring(0, index);
                        //Debug.Log(matPath);
                        ignoredMaterial = !material.HasProperty(matPath);
                    }

                    if (!ignoredUnity)
                    {
                        var pt = unityProp.propertyType;
                        if (pt == SerializedPropertyType.FixedBufferSize)
                            ignore = true;
                        else if (pt == SerializedPropertyType.ArraySize)
                        {
                            // For certain considerWhole particularly string
                            var parent = serializedObject.FindProperty(o.Substring(0, o.Length - Helper.ARRAY_SIZE_SUFFIX.Length));
                            if (parent != null && !Helper.HasChildren(parent))
                                ignore = true;
                        }
                        // TODO2: search upward to know if visible at all?
                        // for example could be ObjectReference!
                        // Start at root then use findrelative!
                    }

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    if (ignore || (ignoredUnity && ignoredMaterial && ignoredOdin))
#else
                    if (ignore || (ignoredUnity && ignoredMaterial))
#endif
                    {
                        Debug.Log("Removed unused override of nonexistent/non-editable/ignored property:\n" + o + "\nIn asset: " + asset.Path() + Helper.LOG_END);
                        av.data.overrides.RemoveAt(ii);
                        if (av.overridesCache != null)
                            av.overridesCache.hashSet.Remove(o);
                        ii--;
                        dirty = true;
                    }

                    if (unityProp != null)
                        unityProp.Dispose();
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    if (odinProp != null)
                        odinProp.Dispose();
#endif
                }

                serializedObject.Dispose();
                if (parentSO != null)
                    parentSO.Dispose();
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                if (tree != null)
                {
                    tree.Dispose();
                    if (parentTree != null)
                        parentTree.Dispose();
                }
#endif

                if (dirty)
                    av.SaveUserDataWithUndo(true, true);
            }
        }
        */
        /// <summary>
        /// This clears all the old overrides, and calls CreateOverridesFromDifferences().
        /// revertAfterward=true means it will call RevertAssetFromParentAsset, to hopefully visually indicate if something went wrong and an override wasn't created when it should have.
        /// </summary>
        public static void CreateFreshOverrides(Object asset, bool revertAfterward)
        {
            using (var av = AV.Open(asset))
            {
                if (av == null || av.data.overrides.Count == 0)
                    return;

                av.data.overrides.Clear();
                av.UpdateOverridesCache();
                av.CreateOverridesFromDifferences();
                if (av.overridesCache != null)
                    av.overridesCache.dirty = true;
                if (revertAfterward && av.data.HasParent)
                    av.RevertAssetFromParentAsset();
            }
        }

        public static void CreateVariant(Object parent)
        {
            string parentPath = AssetDatabase.GetAssetPath(parent);

            if (string.IsNullOrEmpty(parentPath) || !Helper.ValidType(parent.GetType()))
            {
                Debug.LogError("Can't create variant of non-asset" + Helper.LOG_END);
                return;
            }

            string childPath = GetNewPath(parentPath);
            CreateVariant(parent, childPath);
        }
        private static string GetNewPath(string oldPath)
        {
            string directory = Path.GetDirectoryName(oldPath);
            string name = Path.GetFileNameWithoutExtension(oldPath) + " Variant";
            string extension = Path.GetExtension(oldPath);

            string path = Path.Combine(directory, name + extension);

            int counter = 0;
            while (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                counter++;
                path = Path.Combine(directory, name + " " + counter + extension);
            }

            return path;
        }
        public static void CreateVariant(Object parent, string childPath, bool mainOnly = false)
        {
            string parentPath = AssetDatabase.GetAssetPath(parent);
            AssetDatabase.CopyAsset(parentPath, childPath);
            AssetDatabase.Refresh();

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(childPath);
            mainAsset.name = Path.GetFileNameWithoutExtension(childPath); // To avoid Unity's warning, in older versions: "The main object name should match the asset filename. Please fix to avoid errors."

            var childAssets = AssetDatabase.LoadAllAssetsAtPath(childPath);
            for (int i = 0; i < childAssets.Length; i++)
            {
                if (mainOnly && childAssets[i] != mainAsset)
                    continue;

                using (AV childAV = AV.Open(childAssets[i]))
                {
                    if (childAV == null)
                        continue;
                    childAV.data.parent = AssetID.Null;
                    childAV.data.children.Clear();
                    childAV.data.overrides.Clear();
                }
            }

            if (mainOnly)
                ChangeParent(mainAsset, parent);
            else
                ChangeParent(childPath, parentPath);
        }

        public static Object GetParentAsset(Object childAsset)
        {
            using (AV childAV = AV.Open(childAsset))
            {
                if (childAV != null && childAV.Valid())
                    return childAV.LoadParentAsset();
            }
            return null;
        }

        /// <summary>
        /// This will also set parents for the sub-assets whose localIds match.
        /// </summary>
        public static void ChangeParent(string childPath, string parentPath, bool cleanOverrides = false)
        {
            var parentAssets = AssetDatabase.LoadAllAssetsAtPath(parentPath); // TODO2: are these guaranteed to be in the same order?
            var childAssets = AssetDatabase.LoadAllAssetsAtPath(childPath);
            List<AV> parentAVs = new List<AV>();
            for (int i = 0; i < parentAssets.Length; i++)
            {
                var av = AV.Open(parentAssets[i]);
                if (av.Valid())
                    parentAVs.Add(av);
            }
            for (int i = 0; i < childAssets.Length; i++)
            {
                using (AV childAV = AV.Open(childAssets[i]))
                {
                    if (!childAV.Valid() ||
                        !AVCommonHelper.IsEditable(childAV.asset))
                        continue;

                    if (cleanOverrides)
                        childAV.data.overrides.Clear();

                    var childId = childAV.assetID.localId;

                    bool found = false;
                    for (int ii = 0; ii < parentAVs.Count; ii++)
                    {
                        if (parentAVs[ii].assetID.localId == childId)
                        {
                            found = true;
                            childAV.ChangeParent(parentAVs[ii].assetID);
                            if (Helper.LogUnnecessary) //?
                                Debug.Log("Set: " + parentAVs[ii].asset + " as parent for: " + childAV.asset);
                            break;
                        }
                    }
                    if (!found)
                    {
                        Debug.LogError("Could not find parent AV for localId: " + childId + Helper.LOG_END);
                        childAV.ChangeParent(AssetID.Null);
                    }
                }
            }
            for (int i = 0; i < parentAVs.Count; i++)
                parentAVs[i].Dispose();
        }
        public static void ChangeParent(Object childAsset, Object parentAsset, bool cleanOverrides = false)
        {
            using (AV childAV = AV.Open(childAsset))
            using (AV parentAV = AV.Open(parentAsset))
            {
                if (childAV.Valid())
                {
                    if (cleanOverrides)
                        childAV.data.overrides.Clear();

                    if (parentAV.Valid())
                    {
                        childAV.ChangeParent(parentAV.assetID);
                    }
                    else
                    {
                        Debug.Log("Parent AV is not valid. Assigned null as parent instead");
                        childAV.ChangeParent(AssetID.Null);
                    }
                }
            }
        }

        public static void FillRootOverrides(Object variantAsset)
        {
            using (var av = AV.Open(variantAsset))
            {
                if (av.Valid() && av.data.HasParent)
                {
                    var propertyFilter = PropertyFilter.Get(variantAsset.GetType());

                    var serializedObject = new SerializedObject(variantAsset);
                    var prop = serializedObject.GetIterator();
                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            if (!propertyFilter.ShouldIgnore(prop.name, prop.propertyPath))
                            {
                                av.TryCreateOverride(prop.propertyPath);
                            }
                        }
                        while (prop.Next(false));
                    }
                    serializedObject.Dispose();

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    if (variantAsset.IsOdinSerializable())
                    {
                        PropertyTree tree = PropertyTree.Create(variantAsset);

                        var odinProp = tree.GetOdinIterator();
                        if (odinProp.NextVisible(true))
                        {
                            do
                            {
                                if (!Helper.IsUnitySerialized(odinProp.odinProperty)
                                    && !propertyFilter.ShouldIgnore(odinProp.name, odinProp.propertyPath))
                                {
                                    av.TryCreateOverride(odinProp.propertyPath);
                                }
                            }
                            while (odinProp.Next(false));
                        }

                        tree.Dispose();
                    }
#endif

                    av.SaveUserDataWithUndo();
                }
            }
        }

        /// <summary>
        /// Similar to Unity's Reimport All.
        /// This will find every asset variant root and have them propagate to their children.
        /// (Before that it also validates all)
        /// </summary>
        public static void ValidateAll(string[] folders = null)
        {
            var guids = (folders != null) ? AssetDatabase.FindAssets("t:Object", folders) : AssetDatabase.FindAssets("t:Object");

            bool prevForceLog = Helper.forceLog;
            Helper.forceLog = true;
            try
            {
                HashSet<string> alreadyGUIDs = new HashSet<string>();
                for (int i = 0; i < guids.Length; i++)
                {
                    var guid = guids[i];
                    if (!alreadyGUIDs.Add(guid))
                        continue;
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".unity"))
                        continue;

                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    float progress = i / (float)guids.Length;

                    for (int ii = 0; ii < assets.Length; ii++)
                    {
                        var asset = assets[ii];
                        if (asset == null)
                            continue;

                        if (EditorUtility.DisplayCancelableProgressBar("Validate All Asset Variants [1/2]", "Validating: " + asset.name, progress))
                        { Debug.LogError("Cancelled"); return; }

                        using (var av = AV.Open(asset))
                        {
                            if (av != null && av.Valid())
                                av.ValidateRelations();
                        }
                    }
                }

                alreadyGUIDs.Clear();
                for (int i = 0; i < guids.Length; i++)
                {
                    var guid = guids[i];
                    if (!alreadyGUIDs.Add(guid))
                        continue;
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".unity"))
                        continue;

                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    float progress = i / (float)guids.Length;

                    for (int ii = 0; ii < assets.Length; ii++)
                    {
                        var asset = assets[ii];
                        if (asset == null)
                            continue;

                        if (EditorUtility.DisplayCancelableProgressBar("Validate All Asset Variants [2/2]", "Propagation for: " + asset.name, progress))
                        { Debug.LogError("Cancelled"); return; }

                        using (var av = AV.Open(asset))
                        {
                            if (av != null && av.Valid())
                            {
                                if (!av.data.HasParent && av.data.children.Count > 0)
                                    av.PropagateChangesToChildren();
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                Helper.forceLog = prevForceLog;
            }

            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Similar to Unity's Reimport All.
        /// This will find every asset variant root and have them propagate to their children.
        /// (Before that it also validates all)
        /// </summary>
        public static void COFDAll(string[] folders = null)
        {
            var guids = (folders != null) ? AssetDatabase.FindAssets("t:Object", folders) : AssetDatabase.FindAssets("t:Object");

            bool prevForceLog = Helper.forceLog;
            Helper.forceLog = true;
            try
            {
                HashSet<string> alreadyGUIDs = new HashSet<string>();
                for (int i = 0; i < guids.Length; i++)
                {
                    var guid = guids[i];
                    if (!alreadyGUIDs.Add(guid))
                        continue;
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".unity"))
                        continue;

                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    float progress = i / (float)guids.Length;

                    for (int ii = 0; ii < assets.Length; ii++)
                    {
                        var asset = assets[ii];
                        if (asset == null)
                            continue;

                        if (EditorUtility.DisplayCancelableProgressBar("COFD All Asset Variants", "Creating Overrides From Differences for: " + asset.name, progress))
                        { Debug.LogError("Cancelled"); return; }

                        using (var av = AV.Open(asset))
                        {
                            if (av != null && av.Valid() && av.data.HasParent)
                                av.CreateOverridesFromDifferences();
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                Helper.forceLog = prevForceLog;
            }

            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Selects all assets that are a parent and/or child or if it has any overrides.
        /// </summary>
        public static void SelectAll(string[] folders = null)
        {
            var guids = (folders != null) ? AssetDatabase.FindAssets("t:Object", folders) : AssetDatabase.FindAssets("t:Object");

            try
            {
                List<Object> newSelection = new List<Object>();

                HashSet<string> alreadyGUIDs = new HashSet<string>();
                for (int i = 0; i < guids.Length; i++)
                {
                    var guid = guids[i];
                    if (!alreadyGUIDs.Add(guid))
                        continue;
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".unity"))
                        continue;

                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                    float progress = i / (float)guids.Length;

                    for (int ii = 0; ii < assets.Length; ii++)
                    {
                        var asset = assets[ii];
                        if (asset == null)
                            continue;

                        if (EditorUtility.DisplayCancelableProgressBar("Asset Variants Select All (" + newSelection.Count + " found so far)", "Checking: " + asset.name, progress))
                        { Debug.LogError("Cancelled"); return; }

                        using (var av = AV.Open(asset))
                        {
                            if (av != null && av.Valid())
                            {
                                if (av.data.HasParent || av.data.Exists)
                                {
                                    newSelection.Add(asset);
                                    EditorGUIUtility.PingObject(asset);
                                }
                            }
                        }
                    }
                }

                Selection.objects = newSelection.ToArray();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Resources.UnloadUnusedAssets();
        }
    }
}

#endif
