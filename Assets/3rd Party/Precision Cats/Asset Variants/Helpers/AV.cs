#if UNITY_EDITOR

#if !ODIN_INSPECTOR
#undef ASSET_VARIANTS_DO_ODIN_PROPERTIES
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using PrecisionCats;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using S = AssetVariants.AVSettings;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
#endif
#if !ASSET_VARIANTS_DO_ODIN_PROPERTIES
using Property = UnityEditor.SerializedProperty;
#endif
using SP = UnityEditor.SerializedProperty;

namespace AssetVariants
{
    internal class AV : IDisposable
    {
        public readonly AssetID assetID;
        public readonly Object asset;
        public AssetImporter Importer { get; private set; }

        public WeakReference<Object> cachedParent;

        public string UserDataKey
        {
            get
            {
                if (assetID.type == AssetID.AssetType.Main)
                    return "AssetVariantData.Main";
                else if (assetID.type == AssetID.AssetType.Importer)
                    return "AssetVariantData.Importer";
                else
                    return "AssetVariantData." + assetID.localId;
            }
        }
        public Object LoadParentAsset()
        {
            if (data.HasParent)
            {
                Object p;
                if (cachedParent != null && cachedParent.TryGetTarget(out p) && p != null)
                    return p;
                p = data.parent.Load();
                cachedParent = new WeakReference<Object>(p);
                return p;
            }
            return null;
        }

        private bool TryRenameOverride(string path, string name, string oldName)
        {
            bool dirty = false;

            string oldPath = path.Substring(0, path.Length - name.Length) + oldName;
            if (overridesCache.FastContainsChildrenPaths(oldPath, true))
            {
                string oldPathDot = oldPath + ".";

                int oldPathL = oldPath.Length;
                for (int i = 0; i < overridesCache.list.Count; i++)
                {
                    var o = overridesCache.list[i];
                    if (o == oldPath || o.StartsWith(oldPathDot, StringComparison.Ordinal))
                    {
                        var newO = path + o.Substring(oldPathL, o.Length - oldPathL);
                        Debug.Log("Renaming override: " + o + "\nto: " + newO + "\nbecause of the FormerlySerializedAsAttribute at:\n" + oldPath + Helper.LOG_END);
                        overridesCache.Remove(o);
                        overridesCache.Add(newO);
                        dirty = true;
                    }
                }
            }

            return dirty;
        }

        #region Renaming FormerlySerializedAs
        private static readonly HashSet<Object> alreadyRenamedOverrides = new HashSet<Object>();
        private bool MightHaveFSA()
        {
            //TODO: move this to shouldRenameOverrides?
            Type type = asset.GetType();
            for (int i = 0; i < S.S.formerlySerializedAsBaseAssetTypes.Length; i++)
            {
                var baseType = AVCommonHelper.FindType(S.S.formerlySerializedAsBaseAssetTypes[i]);
                if (baseType != null && baseType.IsAssignableFrom(type))
                {
                    return true;
                }
            }
            return false;
        }
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
        private void RenameOverrides(PropertyTree odinDst)
        {
#if DEBUG_ASSET_VARIANTS
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            bool dirty = false;

            var odinProp = odinDst.RootProperty();
            if (odinProp != null)
            {
                while (true)
                {
                    var prevProp = odinProp;
                    odinProp = odinProp.NextProperty(true, false); // TODO2: skip InternalOnInspectorGUI?
                    prevProp.Dispose();
                    if (odinProp == null)
                        break;

                    if (SkipDuplicate(odinProp))
                        break;

                    if (odinProp.Info.SerializationBackend == SerializationBackend.Odin)
                    {
                        foreach (var attribute in odinProp.Info.Attributes)
                        {
                            if (attribute is FormerlySerializedAsAttribute)
                            {
                                var fsa = (FormerlySerializedAsAttribute)attribute;
                                //TODO:::: UnityPropertyPath isn't right here! Needs to be override path
                                if (TryRenameOverride(odinProp.UnityPropertyPath, odinProp.Name, fsa.oldName))
                                    dirty = true;
                            }
                        }
                    }
                }
            }

#if DEBUG_ASSET_VARIANTS
            Debug.Log("Odin renaming duration: " + sw.Elapsed.TotalMilliseconds);
#endif

            if (dirty)
                SaveUserDataWithUndo();
        }
#endif
        private void RenameProp(PathState pathState, SP prop, ref bool dirty)
        {
            //Debug.Log("RenameProp: " + prop.propertyPath + " " + prop.hasChildren);

            var path = pathState.Path;

            //TODO:::::::::::: support emulations

            int prevDepth;
            if (!ManagedReferencesHelper.SkipDuplicate(prop, out prevDepth))
            {
                R.args[0] = prop;
                R.args[1] = null; // out Type type
                var fieldInfo = R.getFieldInfoFromPropertyMI.Invoke(null, R.args) as FieldInfo;

                if (fieldInfo != null)
                {
                    foreach (var fsa in fieldInfo.GetCustomAttributes<FormerlySerializedAsAttribute>())
                    {
                        //Debug.Log(prop.propertyPath + " " + fsa.oldName);
                        if (TryRenameOverride(path, prop.name, fsa.oldName))
                            dirty = true;
                    }
                }
                //else
                //Debug.Log("FieldInfo for: " + prop.propertyPath + " is null");

                if (prop.hasChildren)
                {
                    int depth = prop.depth;
                    var childrenIterator = prop.Copy();
                    if (childrenIterator.NextChildSkipArray())
                    {
                        while (depth < childrenIterator.depth)
                        {
                            RenameProp(pathState.Child(childrenIterator.propertyPath), childrenIterator, ref dirty);

                            if (!childrenIterator.Next(false))
                                break;
                        }
                    }
                    childrenIterator.Dispose();
                }
            }

            ManagedReferencesHelper.RestoreSkipDuplicateDepth(prevDepth);
        }
        private void RenameOverrides(SerializedObject dst)
        {
#if DEBUG_ASSET_VARIANTS
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            bool dirty = false;

            var prop = dst.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    RenameProp(PathState.Same(prop.propertyPath), prop, ref dirty);
                }
                while (prop.Next(false));
            }
            prop.Dispose();

#if DEBUG_ASSET_VARIANTS
            Debug.Log("Renaming duration: " + sw.Elapsed.TotalMilliseconds);
#endif

            if (dirty)
                SaveUserDataWithUndo();
        }
        #endregion

        private PropertyFilter srcFilter, dstFilter;

        #region Odin AttributeProcessor
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
        private class RemoveAttributeProcessor : OdinAttributeProcessor
        {
            public static bool active = false;

            public override bool CanProcessSelfAttributes(InspectorProperty property)
            {
                return active;
            }
            public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
            {
                attributes.Clear(); // TODO2:

                //for (int i = attributes.Count - 1; i >= 0; i--)
                //{
                //    Debug.Log(attributes[i]);
                //    //if (typeof(PropertyGroupAttribute).IsAssignableFrom(attributes[i].GetType()))
                //    //    attributes.RemoveAt(i);
                //}
            }
        }
        private static readonly RemoveAttributeProcessor removeAttributeProcessor = new RemoveAttributeProcessor();

        private class SkipAttributeProcessorLocator : OdinAttributeProcessorLocator
        {
            public override List<OdinAttributeProcessor> GetChildProcessors(InspectorProperty parentProperty, MemberInfo member)
            {
                return new List<OdinAttributeProcessor>();
            }
            public override List<OdinAttributeProcessor> GetSelfProcessors(InspectorProperty property)
            {
                return new List<OdinAttributeProcessor>() { removeAttributeProcessor };
            }
        }
        private static readonly SkipAttributeProcessorLocator skipAttributeProcessorLocator = new SkipAttributeProcessorLocator();
#endif
        #endregion

        public void PrepareImplicit()
        {
            AV parentAV;
            if (openAVs.TryGetValue(data.parent, out parentAV))
                parentAV.FlushAnyChangesToChildren();
        }
        public SerializedObject implicitSrc, implicitDst;
        private void EmulationCreateImplicit(string path, List<PropertyKeyPair> a, List<PropertyKeyPair> b, ref bool dirty)
        {
            for (int i = 0; i < a.Count; i++)
            {
                string key = a[i].key;
                if (IndexOfKey(b, key) == -1)
                {
                    var subPath = Helper.KeyPath(path, key);
                    if (!overridesCache.Contains(subPath) && overridesCache.Add(subPath))
                    {
                        if (Helper.LogUnnecessary)
                            Debug.Log("Emulation implicitly created override for: " + subPath + Helper.LOG_END);
                        dirty = true;
                    }
                }
            }
        }
        private void PropCreateImplicit(PathState pathState, SerializedObject src, PropertyFilter filter, SP dstProp, ref SP srcProp, ref bool dirty)
        {
            int prevDepth;
            if (!ManagedReferencesHelper.SkipDuplicate(dstProp, out prevDepth))
            {
                string path = pathState.Path; //Debug.Log(path);

                if (dstProp.editable && !filter.ShouldIgnore(dstProp.name, path))
                {
                    bool hasChildren = Helper.HasChildren(dstProp);

                    if (srcProp == null)
                    {
                        srcProp = src.FindProperty(pathState.findingPropertyPath);
                    }
                    else if (srcProp.propertyPath != pathState.findingPropertyPath)
                    {
                        srcProp.Dispose();
                        srcProp = src.FindProperty(pathState.findingPropertyPath);
                    }

                    if (!data.HasParent || overridesCache.IsOverridden(path))
                        goto End;

                    bool srcExists = srcProp != null;

                    var srcEmulation = srcFilter.GetEmulationProperty(path);
                    var dstEmulation = dstFilter.GetEmulationProperty(path);

                    bool never = S.S.missingSourceImplicitCreationType == S.MissingSourceImplicitCreationType.NeverCreateOverride;
                    if (srcExists && dstEmulation != null)
                    {
                        if (srcEmulation == null)
                            srcEmulation = DefaultEmulationProperty.instance;
                        srcPropertyKeyPairs.Clear();
                        srcEmulation.GetChildren(srcProp, srcPropertyKeyPairs);

                        dstPropertyKeyPairs.Clear();
                        dstEmulation.GetChildren(dstProp, dstPropertyKeyPairs);

                        for (int i = 0; i < srcPropertyKeyPairs.Count; i++)
                        {
                            var srcPair = srcPropertyKeyPairs[i];
                            var key = srcPair.key;
                            var dstID = IndexOfKey(dstPropertyKeyPairs, key);
                            if (dstID != -1)
                            {
                                var dstPair = dstPropertyKeyPairs[dstID];
                                if (srcPair.property is Property childSrcProp && dstPair.property is Property childDstProp)
                                {
                                    var childPathState = pathState.Child(childDstProp.propertyPath, childSrcProp.propertyPath, Helper.LocalKeyPath(key));
                                    var childSrcPropSP = childSrcProp.ToSP();
                                    if (childSrcPropSP != null)
                                        PropCreateImplicit(childPathState, src, filter, childDstProp.ToSP(), ref childSrcPropSP, ref dirty);
                                }
                            }
                        }

                        EmulationCreateImplicit(path, srcPropertyKeyPairs, dstPropertyKeyPairs, ref dirty);
                        EmulationCreateImplicit(path, dstPropertyKeyPairs, srcPropertyKeyPairs, ref dirty);

                        DisposePropertyKeyPairs();
                    }
                    else
                    {
                        if (never ? (srcExists && !hasChildren) : (!srcExists || !hasChildren))
                        {
                            if (!srcExists || !Helper.AreEqual(dstProp, srcProp))
                            {
                                //Debug.Log(dstProp.propertyPath + " srcExists: " + srcExists + " - " + hasChildren);
                                overridesCache.Add(path);
                                dirty = true;
                                if (Helper.LogUnnecessary) //Debug.Log(asset.name + " - " + AVUpdater.timestamp + "Implicitly created override for: " + path + Helper.LOG_END);
                                    Debug.Log("Implicitly created override for: " + path + "\nFor " + asset.GetType() + ": " + asset.name + Helper.LOG_END);
                            }
                        }

                        if (hasChildren && srcExists)
                        {
                            var childDstIterator = dstProp.Copy();
                            if (!childDstIterator.isArray || childDstIterator.Next(true)) // Skip .Array
                            {
                                if (childDstIterator.Next(true)) // Enter children
                                {
                                    int depth = dstProp.depth;
                                    while (depth < childDstIterator.depth)
                                    {
                                        PropCreateImplicit(pathState.Child(childDstIterator.propertyPath), src, filter, childDstIterator, ref srcProp, ref dirty);

                                        if (!childDstIterator.Next(false))
                                            break;
                                        if (srcProp != null && !srcProp.Next(false))
                                        {
                                            srcProp.Dispose();
                                            srcProp = null;
                                        }
                                    }
                                }
                            }
                            childDstIterator.Dispose();
                        }
                    }
                }
            }

        End:
            ManagedReferencesHelper.RestoreSkipDuplicateDepth(prevDepth);
        }
        private void DoCreateImplicitOverrides(int undoGroup) //List<string> specificPaths)
        {
#if DEBUG_ASSET_VARIANTS
            Debug.Log("Searching Implicit" + Helper.LOG_END);
#endif

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (asset is SerializedScriptableObject)
            {
                //TODO: an actual odin implementation
                //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                //sw.Start();
                CreateOverridesFromDifferences();
                //Debug.Log(sw.Elapsed.TotalMilliseconds);
                return;
            }
#endif

            var parentAsset = LoadParentAsset();
            if (parentAsset == null)
            {
                Debug.LogError("parent == null");
                return;
            }

            srcFilter = PropertyFilter.Get(parentAsset.GetType());
            dstFilter = PropertyFilter.Get(asset.GetType());

            if (implicitSrc == null || implicitSrc.targetObject != parentAsset)
                implicitSrc = new SerializedObject(parentAsset);
            else
                implicitSrc.UpdateIfRequiredOrScript();

            if (implicitDst == null || implicitDst.targetObject != asset)
                implicitDst = new SerializedObject(asset);
            else
                implicitDst.UpdateIfRequiredOrScript();

            var filter = PropertyFilter.Get(asset.GetType());

            GetOverridesCache();

            bool dirty = false;

            SP srcProp = null;
            //if (specificPaths != null)
            //{
            //    for (int i = 0; i < specificPaths.Count; i++)
            //    {
            //        //Debug.Log("specificPath: " + specificPaths[i]);
            //        var dstProp = implicitDst.FindProperty(specificPaths[i]);
            //        if (dstProp == null)
            //        {
            //            Debug.LogWarning("");
            //            continue;
            //        }

            //        ManagedReferencesHelper.InitSkipDuplicates();
            //        PropCreateImplicit(implicitSrc, filter, dstProp, ref srcProp);

            //        if (srcProp != null)
            //        {
            //            srcProp.Dispose();
            //            srcProp = null;
            //        }

            //        dstProp.Dispose();
            //    }
            //}
            //else
            {
                ManagedReferencesHelper.InitSkipDuplicates();

                var dstIterator = implicitDst.GetIterator();
                if (dstIterator.NextVisible(true)) //?
                {
                    while (true)
                    {
                        PropCreateImplicit(PathState.Same(dstIterator.propertyPath), implicitSrc, filter, dstIterator, ref srcProp, ref dirty);

                        if (!dstIterator.Next(false))
                            break;
                        if (srcProp != null && !srcProp.Next(false))
                        {
                            srcProp.Dispose();
                            srcProp = null;
                        }
                    }
                }
                dstIterator.Dispose();
            }

            if (dirty)
            {
                SaveUserDataWithUndo(false, true, undoGroup);
                OverrideIndicator.OverridesMaybeChanged();
            }

            if (srcProp != null)
                srcProp.Dispose();
        }
        public void CreateImplicitOverrides(int undoGroup = int.MinValue)
        {
            PrepareImplicit();

            DoCreateImplicitOverrides(undoGroup); // null);

            CustomCreateImplicitOverride ci;
            if (customCreateImplicitOverrides.TryGetValue(asset.GetType(), out ci))
            {
                if (ci != null)
                    ci(this, undoGroup);
            }
        }
        public static readonly Dictionary<Type, CustomCreateImplicitOverride> customCreateImplicitOverrides = new Dictionary<Type, CustomCreateImplicitOverride>();

        public delegate void CustomCreateImplicitOverride(AV av, int undoGroup);

        public void TryDirtyImplicit(int undoGroup)
        {
            if (!data.HasParent)
                return;

            /* //TODO:?
                        if (undoGroup == int.MinValue)
                            Debug.LogError("!");
            */

            if (implicitOverridesDirtyUndoGroup == int.MinValue)
            {
                implicitOverridesDirtyUndoGroup = undoGroup;
                EditorApplication.update += UpdateFlushImplicit;

                FlushImplicit(false);
            }
            else if (undoGroup < implicitOverridesDirtyUndoGroup)
                implicitOverridesDirtyUndoGroup = undoGroup;
        }
        private int implicitOverridesDirtyUndoGroup = int.MinValue;
        private void UpdateFlushImplicit()
        {
            if (implicitOverridesDirtyUndoGroup == int.MinValue)
            {
                EditorApplication.update -= UpdateFlushImplicit;
                Debug.LogWarning("?");
            }
            FlushImplicit(false);
        }
        public double implicitTimestamp = double.MinValue;
        public void FlushImplicit(bool force)
        {
            if (implicitOverridesDirtyUndoGroup != int.MinValue)
            {
                double now = EditorApplication.timeSinceStartup;
                if (force || (now - implicitTimestamp) >= S.S.implicitOverrideCreationInterval)
                {
                    EditorApplication.update -= UpdateFlushImplicit;

                    implicitTimestamp = now;

                    CreateImplicitOverrides(implicitOverridesDirtyUndoGroup);

                    implicitOverridesDirtyUndoGroup = int.MinValue;
                }
            }
        }

        public static readonly WeakTable<Object, int> revertingTimestamps = new WeakTable<Object, int>();

#if ODIN_INSPECTOR
        private void OdinBeforeChange(ref int changes)
        {
#if !ODIN_INSPECTOR_3_1
            if (changes == 0)
                Undo.RegisterCompleteObjectUndo(asset, "RevertAssetFromParentAsset");
#endif
            changes++;
        }
#endif
        private void RevertProp(PathState pathState, Property srcProp, Property parentDstProp, ref Property dstPropGuess, out bool found, bool odinPass, bool enteredArray, ref int changes, Property dstProp = null)
        {
            //if (odinPass)
            //    Debug.Log("RevertProp Odin: " + srcProp.odinProperty.UnityPropertyPath); // + " - " + srcProp.odinProperty.Path);

            int prevDepth;
            if (SkipDuplicate(srcProp, odinPass, out prevDepth))
            {
                // Not quite right, but good enough
                found = true;
                goto Exit;
            }

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (odinPass && srcProp.odinProperty.Info.PropertyType != PropertyType.Value)
            {
                Debug.LogWarning("Something went wrong: " + srcProp.odinProperty.Info.PropertyType);
                found = true;
                goto Exit;
            }
#endif

            found = false;

            string path = pathState.Path;

            if (overridesCache.Contains(path))
            {
                found = true;
                goto Exit;
            }
            string name = srcProp.name;
            if (srcFilter.ShouldIgnore(name, path, enteredArray) || dstFilter.ShouldIgnore(name, path, enteredArray))
                goto Exit;

            if (parentDstProp != null)
                dstProp = parentDstProp.FindChild(pathState, srcProp, dstPropGuess);
            if (dstProp != null && dstProp.editable)
            {
                bool currentlyInvalid = false;
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                currentlyInvalid = Helper.CurrentlyInvalid(srcProp, dstProp, odinPass);
#endif

                //Debug.Log("Found Dst: " + path + " - currentlyInvalid: " + currentlyInvalid);

                found = true;

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                bool dstHasChildren = Helper.HasChildren(dstProp);
                bool dstCanHaveChildren = dstHasChildren ||
                    (odinPass && !dstProp.odinIsSize && dstProp.odinProperty.IsCollection()); // Dictionary and HashSet need .IsCollection

                // So that a null collection will be copied as a whole
                if (dstCanHaveChildren && odinPass)
                {
                    if (srcProp.odinProperty.GetWeakValue() == null)
                        dstCanHaveChildren = false;
                    else if (dstProp.odinProperty.GetWeakValue() == null)
                        dstCanHaveChildren = false;
                }

                bool matchesType = dstProp.CompareType(srcProp);
#else
                bool dstCanHaveChildren = Helper.HasChildren(dstProp);
                bool matchesType = Helper.PropertyTypesMatch(dstProp.propertyType, srcProp.propertyType); //TODO2: compare .type and .arrayElementType?
#endif

                var srcEmulation = (odinPass || currentlyInvalid) ? null : srcFilter.GetEmulationProperty(path);
                var dstEmulation = (odinPass || currentlyInvalid) ? null : dstFilter.GetEmulationProperty(path);
                if (dstEmulation != null)
                {
                    if (srcEmulation == null)
                        srcEmulation = DefaultEmulationProperty.instance;
                    srcPropertyKeyPairs.Clear();
                    srcEmulation.GetChildren(srcProp, srcPropertyKeyPairs);

                    dstPropertyKeyPairs.Clear();
                    dstEmulation.GetChildren(dstProp, dstPropertyKeyPairs);

                    for (int i = 0; i < srcPropertyKeyPairs.Count; i++)
                    {
                        var srcPair = srcPropertyKeyPairs[i];
                        var key = srcPair.key;
                        var dstID = IndexOfKey(dstPropertyKeyPairs, key);
                        if (dstID != -1)
                        {
                            var dstPair = dstPropertyKeyPairs[dstID];
                            if (srcPair.keySP != null && dstPair.keySP != null && // Checks for key existing because there's nothing to gain when diving into HashSet elements; If the key matches, there intrinsically are no sub differences
                                srcPair.property is Property childSrcProp && dstPair.property is Property childDstProp)
                            {
                                bool foundChild;
                                var childPathState = pathState.Child(childSrcProp.propertyPath, childDstProp.propertyPath, Helper.LocalKeyPath(key));
                                //Debug.Log(childPathState.Path + " " + childPathState.iterationPropertyPath + " " + childPathState.findingPropertyPath);
                                Property discard = null;
                                RevertProp(childPathState, childSrcProp, null, ref discard, out foundChild, odinPass, enteredArray, ref changes, childDstProp);
                                if (!foundChild)
                                    Debug.LogError("How? " + childPathState.ToString());
                            }
                        }
                    }

                    // Adds to dst if src is missing in dst
                    addProperties.Clear();
                    for (int i = 0; i < srcPropertyKeyPairs.Count; i++)
                    {
                        string key = srcPropertyKeyPairs[i].key;
                        if (IndexOfKey(dstPropertyKeyPairs, key) == -1)
                            if (!overridesCache.Contains(Helper.KeyPath(path, key)))
                                addProperties.Add(srcPropertyKeyPairs[i].property);
                    }

                    // Removes from dst if dst is missing in src
                    removeKeys.Clear();
                    for (int i = 0; i < dstPropertyKeyPairs.Count; i++)
                    {
                        string key = dstPropertyKeyPairs[i].key;
                        if (IndexOfKey(srcPropertyKeyPairs, key) == -1)
                            if (!overridesCache.Contains(Helper.KeyPath(path, key)))
                                removeKeys.Add(key);
                    }

                    //for (int i = 0; i < addProperties.Count; i++)
                    //    Debug.Log("Add: " + addProperties[i]);
                    //for (int i = 0; i < removeKeys.Count; i++)
                    //    Debug.Log("Remove: " + removeKeys[i]);

                    dstEmulation.ModifyChildren(dstProp, removeKeys, addProperties);

                    changes += addProperties.Count + removeKeys.Count;

                    addProperties.Clear();
                    removeKeys.Clear();

                    DisposePropertyKeyPairs();
                }
                else if (!currentlyInvalid && !dstCanHaveChildren && matchesType) // Children might have their own overrides, so children are copied one by one
                {
                    if (odinPass)
                    {
                        #region Copies Data
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                        if (srcProp.odinIsSize)
                        {
                            int size = srcProp.odinProperty.Children.Count;
                            int dstSize = dstProp.odinProperty.Children.Count;
                            if (dstSize != size)
                            {
                                OdinBeforeChange(ref changes);

                                // Resizes
                                var dstValues = dstProp.odinProperty.ValueEntry.WeakValues;

                                var dstType = dstProp.odinProperty.Info.TypeOfValue;
                                if (dstType.IsArray)
                                {
                                    // TODO2: this doesn't need undo right?
                                    dstValues[0] = Helper.ResizeArray(dstValues[0], size);
                                }
                                else
                                {
#if ODIN_INSPECTOR_3_1
                                    dstProp.odinProperty.RecordForUndo("Resize Array/List/Queue/Stack");
#endif

                                    // List<>, Queue<>, or Stack<>
                                    var collection = (ICollection)dstValues[0];

                                    var type = collection.GetType();

                                    if (collection.Count < size)
                                    {
                                        object newElement = null;
                                        foreach (var o in collection)
                                            newElement = o; // Finds the last element
                                        if (newElement == null)
                                        {
                                            Type elementType = type.GetGenericArguments()[0];
                                            if (elementType.IsValueType)
                                                newElement = Activator.CreateInstance(elementType);
                                            else
                                                newElement = null;
                                        }

                                        //if (type.InheritsFrom(typeof(List<>)))
                                        if (typeof(IList).IsAssignableFrom(type))
                                        {
                                            var list = (IList)collection;
                                            while (list.Count < size)
                                                list.Add(newElement);
                                        }
                                        else
                                        {
                                            object[] arg = new object[1] { newElement };

                                            MethodInfo grow = null;
                                            if (type.InheritsFrom(typeof(Queue<>)))
                                                // This will however shift the indices in the editor
                                                grow = type.GetMethod("Enqueue", BindingFlags.Instance | BindingFlags.Public);
                                            else //if (type.InheritsFrom(typeof(Stack<>)))
                                                grow = type.GetMethod("Push", BindingFlags.Instance | BindingFlags.Public);

                                            while (collection.Count < size)
                                                grow.Invoke(collection, arg);
                                        }
                                    }
                                    else
                                    {
                                        //if (type.InheritsFrom(typeof(List<>)))
                                        if (typeof(IList).IsAssignableFrom(type))
                                        {
                                            var list = (IList)collection;
                                            while (list.Count > size)
                                                list.RemoveAt(list.Count - 1);
                                        }
                                        else
                                        {
                                            MethodInfo shrink = null;
                                            if (type.InheritsFrom(typeof(Queue<>)))
                                                // This will however shift the indices in the editor
                                                shrink = type.GetMethod("Dequeue", BindingFlags.Instance | BindingFlags.Public);
                                            else //if (type.InheritsFrom(typeof(Stack<>)))
                                                shrink = type.GetMethod("Pop", BindingFlags.Instance | BindingFlags.Public);

                                            while (collection.Count > size)
                                                shrink.Invoke(collection, null);
                                        }
                                    }

                                    dstProp.odinProperty.ChildResolver.ForceUpdateChildCount();
                                }
                            }
                        }
                        else
                        {
                            //Debug.Log("RevertProp Odin: " + srcProp.odinProperty.UnityPropertyPath); // + " - " + srcProp.odinProperty.Path);

                            // TODO2: does this work?
                            if (!Helper.AreEqual(dstProp.odinProperty, srcProp.odinProperty)) //?
                            {
                                //Debug.Log(dstProp.odinProperty.GetWeakValue() + " - " + srcProp.odinProperty.GetWeakValue());

                                OdinBeforeChange(ref changes);

                                dstProp.odinProperty.ValueEntry.WeakValues[0] =
                                    Helper.DeepCopy(srcProp.odinProperty.GetWeakValue()); //dstProp.property.ValueEntry.WeakSmartValue = srcProp.property.ValueEntry.WeakSmartValue; // I'm pretty sure this is a shallow copy // Is this a deep copy or shallow copy?
                            }
                            //else
                            //Debug.Log("Same: " + dstProp.odinProperty.GetWeakValue() + " " + srcProp.odinProperty.GetWeakValue());
                        }
#endif
                        #endregion
                    }
                    else
                    {
                        if (path.CustomEndsWith(Helper.ARRAY_SIZE_SUFFIX))
                        {
                            // Prevents crash caused by invalid iterator
                            int srcCount = srcProp.ToSP().intValue;
                            int dstCount = dstProp.ToSP().intValue;

                            if (srcCount != dstCount)
                            {
                                SerializedProperty arraySP;
                                bool disposeArraySP;

                                if (parentDstProp != null)
                                {
                                    arraySP = parentDstProp.ToSP();
                                    disposeArraySP = false;
                                }
                                else
                                {
                                    // This only occurs from RevertPropertiesFromParentAsset
                                    // which sends null as parentDstProp
                                    var arrayPath = Helper.GetArrayPath(path);
                                    arraySP = dstProp.ToSP().serializedObject.FindProperty(arrayPath);
                                    disposeArraySP = true;
                                }

                                changes += Mathf.Abs(srcCount - dstCount);

                                arraySP.arraySize = srcCount;

                                if (disposeArraySP)
                                    arraySP.Dispose();
                            }
                        }
                        else
                        {
                            if (Helper.CopyValueRecursively(dstProp.ToSP(), srcProp.ToSP()))
                                changes++;
                        }
                    }
                }
                else
                {
                    bool copyChildren = dstCanHaveChildren
                        && Helper.HasChildren(srcProp);

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    bool dictionaryHashSetDirty = false;

                    #region Removing from/Adding to Odin HashSet
                    bool isHashSet = odinPass && Helper.IsHashSet(dstProp.odinProperty);
                    if (isHashSet)
                        copyChildren = false;
                    if (matchesType && !currentlyInvalid && isHashSet)
                    {
                        var collection = dstProp.odinProperty.GetWeakValue();

                        MethodInfo hashSetRemove = collection.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public);
                        MethodInfo hashSetAdd = collection.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

                        GetDictionaryHashSetElements(pathState, srcProp.odinProperty.Children, dstProp.odinProperty.Children, false);

                        // Adding
                        for (int i = 0; i < srcElements.Count; i++)
                        {
                            var srcE = srcElements[i];
                            bool foundChild = false;
                            for (int ii = 0; ii < dstElements.Count; ii++)
                            {
                                if (srcE.overridePath == dstElements[ii].overridePath)
                                {
                                    foundChild = true;
                                    break;
                                }
                            }
                            if (!foundChild && !overridesCache.Contains(srcE.overridePath))
                            {
                                if (!dictionaryHashSetDirty)
                                {
                                    dictionaryHashSetDirty = true;
                                    OdinBeforeChange(ref changes);
#if ODIN_INSPECTOR_3_1
                                    dstProp.odinProperty.RecordForUndo("Remove HashSet Element");
#endif
                                }

                                var srcElement = srcE.property.GetWeakValue();
                                hashSetAdd.Invoke(collection, new object[] { Helper.DeepCopy(srcElement) });
                            }
                        }

                        // Removing
                        for (int i = 0; i < dstElements.Count; i++)
                        {
                            var dstE = dstElements[i];
                            bool foundChild = false;
                            for (int ii = 0; ii < srcElements.Count; ii++)
                            {
                                if (dstE.overridePath == srcElements[ii].overridePath)
                                {
                                    foundChild = true;
                                    break;
                                }
                            }
                            if (!foundChild && !overridesCache.Contains(dstE.overridePath))
                            {
                                if (!dictionaryHashSetDirty)
                                {
                                    dictionaryHashSetDirty = true;
                                    OdinBeforeChange(ref changes);
#if ODIN_INSPECTOR_3_1
                                    dstProp.odinProperty.RecordForUndo("Remove HashSet Element");
#endif
                                }

                                object element = dstE.property.GetWeakValue();
                                hashSetRemove.Invoke(collection, new object[] { element });
                            }
                        }
                    }
                    #endregion

                    #region Removing from Odin Dictionary
                    bool fixDictionaryMismatch = false;
                    if (matchesType && odinPass && !currentlyInvalid && Helper.IsDictionary(dstProp.odinProperty))
                    {
                        string sizePath = path + ".DictionarySize";
                        fixDictionaryMismatch = !overridesCache.Contains(sizePath);
                        if (fixDictionaryMismatch)
                        {
                            var dictionary = dstProp.odinProperty.GetWeakValue() as IDictionary;

                            var dstChildrenIterator = dstProp.Copy();
                            int depth = dstChildrenIterator.depth;
                            Property childSrcPropGuess = srcProp.Copy();
                            if (!childSrcPropGuess.Next(true))
                                childSrcPropGuess = null;
                            if (dstChildrenIterator.Next(true))
                            {
                                List<object> toRemove = new List<object>(); // I believe delaying the removals like this might only be necessary in older Odin versions, possibly.

                                while (depth < dstChildrenIterator.depth)
                                {
                                    var childPath = dstChildrenIterator.propertyPath; // For odin pass, override path is always the same as (unity) property path
                                    //Debug.Log("Dictionary Child Path: " + childPath);
                                    var childPathState = pathState.Child(childPath);
                                    var childSrcProp = srcProp.FindChild(childPathState, dstChildrenIterator, childSrcPropGuess);
                                    if (childSrcProp == null && !overridesCache.Contains(childPath))
                                    {
                                        if (!dictionaryHashSetDirty)
                                        {
                                            dictionaryHashSetDirty = true;
                                            OdinBeforeChange(ref changes);
#if ODIN_INSPECTOR_3_1
                                            dstProp.odinProperty.RecordForUndo("Remove Dictionary Element");
#endif
                                        }

                                        object element = dstChildrenIterator.odinProperty.GetWeakValue();
                                        element = element.GetType().GetField("Key").GetValue(element);
                                        toRemove.Add(element);
                                    }
                                    childSrcPropGuess = childSrcProp;

                                    if (!dstChildrenIterator.Next(false))
                                        break;
                                    if (childSrcPropGuess != null && !childSrcPropGuess.Next(false))
                                        childSrcPropGuess = null;
                                }

                                for (int i = 0; i < toRemove.Count; i++)
                                    dictionary.Remove(toRemove[i]);
                            }
                            if (childSrcPropGuess != null)
                                childSrcPropGuess.Dispose();
                            dstChildrenIterator.Dispose();
                        }
                    }
                    #endregion

                    if (dictionaryHashSetDirty)
                    {
                        dstProp.odinProperty.ChildResolver.ForceUpdateChildCount();
                        dstProp.odinProperty.RefreshSetup();
                        EditorUtility.SetDirty(asset);
                    }
#endif

                    if (copyChildren && (matchesType || !S.S.propertyTypesNeedToMatch))
                    {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                        if (odinPass ? dstProp.odinProperty.IsCollection() : dstProp.unityProperty.isArray) enteredArray = true;
#else
                        if (dstProp.isArray) enteredArray = true;
#endif

                        using (var srcChildrenIterator = srcProp.Copy())
                        {
                            int depth = srcChildrenIterator.depth;

                            if (srcChildrenIterator.NextChildSkipArray())
                            {
                                var childDstPropGuess = dstPropGuess;
                                if (childDstPropGuess != null)
                                {
                                    childDstPropGuess = childDstPropGuess.Copy();
                                    if (!childDstPropGuess.NextChildSkipArray())
                                        childDstPropGuess = null;
                                }

                                do
                                {
                                    bool foundChild;
                                    var srcChildPropertyPath = srcChildrenIterator.propertyPath;
                                    var childPathState = pathState.Child(srcChildPropertyPath);
                                    RevertProp(childPathState, srcChildrenIterator, dstProp, ref childDstPropGuess, out foundChild, odinPass, enteredArray, ref changes);

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                                #region Dictionary add missing element
                                if (fixDictionaryMismatch && !foundChild && !overridesCache.Contains(childPathState.Path))
                                {
                                    object srcValue = srcChildrenIterator.odinProperty.GetWeakValue();

                                    // TODO2: find out the proper Odin way to append elements?

#if ODIN_INSPECTOR_3_1
                                    dstProp.odinProperty.RecordForUndo("Add missing element from Dictionary/HashSet");
#endif
                                    OdinBeforeChange(ref changes);

                                    var collection = dstProp.odinProperty.GetWeakValue();

                                    // SrcValue is EditableKeyValuePair
                                    var kvPairType = srcValue.GetType();
                                    var kvKeyPI = kvPairType.GetField("Key", BindingFlags.Public | BindingFlags.Instance);
                                    var kvValuePI = kvPairType.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
                                    var dictionary = (IDictionary)collection;
                                    dictionary.Add(Helper.DeepCopy(kvKeyPI.GetValue(srcValue)),
                                        Helper.DeepCopy(kvValuePI.GetValue(srcValue)));

                                    dstProp.odinProperty.ChildResolver.ForceUpdateChildCount();
                                    EditorUtility.SetDirty(asset);
                                }
                                #endregion
#endif

                                    if (!srcChildrenIterator.Next(false))
                                        break;
                                    if (childDstPropGuess != null && !childDstPropGuess.Next(false))
                                        childDstPropGuess = null;
                                }
                                while (depth < srcChildrenIterator.depth);

                                if (childDstPropGuess != null)
                                    childDstPropGuess.Dispose();
                            }
#if !ASSET_VARIANTS_DO_ODIN_PROPERTIES //TODO: add .serializedObject to Property
                            else
                                Debug.LogWarning("Something unexpected happened at: " + srcProp.propertyPath + " on: " + srcProp.serializedObject + Helper.LOG_END);
#endif
                        }
                    }
                }
            }
            else
            {
                //Debug.Log((dstProp != null) + " " + dstProp.editable + " " + pathState.ToString())
                found = false;
            }

        Exit:

            ManagedReferencesHelper.RestoreSkipDuplicateDepth(prevDepth);

            dstPropGuess = dstProp;
        }
        public bool RevertAssetFromParentAsset(AV selfParent = null) //List<string> propertyPaths = null,
        {
            if (asset == null)
            {
                Debug.LogWarning("Asset is null!" + Helper.LOG_END);
            }

            if (S.disablePropagation(asset))
            {
                Debug.LogWarning("Propagation is disabled for: " + asset.name + Helper.LOG_END);
                return false;
            }

            //if (propertyPaths != null && propertyPaths.Count == 0)
            //    return false;

            if (!this.Valid())
                return false;

            var parentAsset = LoadParentAsset();
            if (parentAsset == null || parentAsset is DefaultAsset)
            {
                if (selfParent == null || selfParent.assetID != data.parent)
                {
                    Debug.LogError("RevertAssetFromParentAsset called for child asset with no parent or a reimporting/DefaultAsset parent. Child in question: " + asset.Path() + Helper.LOG_END);
                    return false;
                }
                parentAsset = selfParent.asset;
                Debug.LogWarning("\n");
            }

            GetOverridesCache();
            srcFilter = PropertyFilter.Get(parentAsset.GetType());
            dstFilter = PropertyFilter.Get(asset.GetType());

            bool renameOverrides = S.shouldRenameOverrides(asset) && MightHaveFSA() && !alreadyRenamedOverrides.Contains(asset); // Prevents redundant checks until domain is reloaded or undo
            alreadyRenamedOverrides.Add(asset);

            int changes = 0;

            bool _;

            bool alreadyRecordedUndo = false;

            revertingTimestamps.Remove(asset);
            revertingTimestamps.Add(asset, AVUpdater.timestamp);

            #region Odin Pass
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (asset.IsOdinSerializable())
            {
                RemoveAttributeProcessor.active = true;
                RawPropertyResolver.active = true;

                var odinSrc = PropertyTree.Create(parentAsset);
                var odinDst = PropertyTree.Create(asset);
                odinSrc.AttributeProcessorLocator = skipAttributeProcessorLocator;
                odinDst.AttributeProcessorLocator = skipAttributeProcessorLocator; //odinSrc.StateUpdaterLocator //odinSrc.SetSearchable //odinSrc.SetUpForValidation(); //odinDst.SetUpForValidation();

                if (renameOverrides)
                {
                    InitSkipDuplicates();
                    RenameOverrides(odinDst);
                }

#if ODIN_INSPECTOR_3_1
                bool prevRUFC = odinDst.RecordUndoForChanges;
                odinDst.RecordUndoForChanges = true;
#endif

                //if (propertyPaths == null)
                {
                    InitSkipDuplicates();

                    var outerSrcIterator = odinSrc.GetOdinIterator();
                    var outerDstIterator = odinDst.GetOdinIterator();
                    Property dstPropGuess = null;
                    if (outerSrcIterator.NextVisible(true, true))
                    {
                        while (true)
                        {
                            // Originally I thought that Odin could serialize fields serializable only by Odin that are children of unity serialized fields.
                            // But that is not the case.
                            // Just skips the entire property if it's serialized by Unity.
                            if (!Helper.IsUnitySerialized(outerSrcIterator.odinProperty))
                                RevertProp(PathState.Same(outerSrcIterator.propertyPath), outerSrcIterator, outerDstIterator, ref dstPropGuess, out _, true, false, ref changes);

                            if (!outerSrcIterator.Next(false))
                                break;
                            if (dstPropGuess != null && !dstPropGuess.Next(false))
                                dstPropGuess = null;
                        }
                    }
                    outerSrcIterator.Dispose();
                    outerDstIterator.Dispose();
                    if (dstPropGuess != null)
                        dstPropGuess.Dispose();
                }
                /*
                else
                {
                    for (int i = propertyPaths.Count - 1; i >= 0; i--)
                    {
                        //if (IsOverridden(propertyPaths[i]))
                        //    continue;

                        string path = propertyPaths[i];
                        bool isSize = path.CustomEndsWith(Helper.ARRAY_SIZE_SUFFIX);
                        if (isSize)
                            path = Helper.GetArrayPath(path);

                        InitSkipDuplicates();

                        using (var odinSrcProp = odinSrc.GetPropertyAtUnityPath(path))
                        using (var odinDstProp = odinDst.GetPropertyAtUnityPath(path))
                        {
                            if (odinSrcProp != null &&
                                odinDstProp != null && !Helper.IsUnitySerialized(odinDstProp))
                            {
                                Property srcProp = new Property()
                                {
                                    odinProperty = odinSrcProp,
                                    odinIsSize = isSize,
                                };
                                Property dstProp = new Property()
                                {
                                    odinProperty = odinDstProp,
                                    odinIsSize = isSize,
                                };
                                Property dstPropGuess = null;

                                int subChanges = 0;

                                bool enteredArray = false; // I don't know if it is or not but it doesn't matter all that much
                                RevertProp(, srcProp, null, ref dstPropGuess, out _, true, enteredArray, ref subChanges, dstProp: dstProp);

                                if (dstPropGuess != null)
                                    dstPropGuess.Dispose();

                                if (subChanges > 0)
                                    if (Helper.LogUnnecessary)
                                        Debug.Log("RevertAssetFromParentAsset called for InspectorProperty: " + propertyPaths[i] + Helper.LOG_END);
                                changes += subChanges;

                                propertyPaths.RemoveAt(i);
                            }
                        }
                    }
                }
                */

#if ODIN_INSPECTOR_3_1
                if (changes > S.REGULAR_RECORD_UNDO_MAX_CHANGES)
                {
                    odinDst.RecordUndoForChanges = false;
                    Undo.RegisterCompleteObjectUndo(asset, "RevertAssetFromParentAsset");
                    alreadyRecordedUndo = true;
                    odinDst.ApplyChanges();
                }
                else
                {
                    odinDst.RecordUndoForChanges = true;
                    alreadyRecordedUndo = odinDst.ApplyChanges();
                }
                odinDst.RecordUndoForChanges = prevRUFC;
#else
                alreadyRecordedUndo = changes > 0;
                odinDst.ApplyChanges();
#endif

                odinSrc.Dispose();
                odinDst.Dispose();

                RawPropertyResolver.active = false;
                RemoveAttributeProcessor.active = false;
            }
#endif
            #endregion

            #region Unity Pass
            var src = new SerializedObject(parentAsset);
            var dst = new SerializedObject(asset);

            if (renameOverrides)
            {
                InitSkipDuplicates();
                RenameOverrides(dst);
            }

            //if (propertyPaths == null)
            {
                InitSkipDuplicates();

                var outerSrcIterator = src.GetUnityIterator();
                var outerDstIterator = dst.GetUnityIterator();
                Property dstPropGuess = null;
                if (outerSrcIterator.NextVisible(true))
                {
                    while (true)
                    {
                        RevertProp(PathState.Same(outerSrcIterator.propertyPath), outerSrcIterator, outerDstIterator, ref dstPropGuess, out _, false, false, ref changes);

                        if (!outerSrcIterator.Next(false))
                            break;
                        if (dstPropGuess != null && !dstPropGuess.Next(false))
                            dstPropGuess = null;
                    }
                }
                outerSrcIterator.Dispose();
                outerDstIterator.Dispose();
                if (dstPropGuess != null)
                    dstPropGuess.Dispose();
            }
            /*
            else
            {
                for (int i = propertyPaths.Count - 1; i >= 0; i--)
                {
                    string path = propertyPaths[i];

                    InitSkipDuplicates();

                    using (var unitySrcProp = src.FindProperty(path))
                    using (var unityDstProp = dst.FindProperty(path))
                    {
                        if (unitySrcProp != null && unityDstProp != null)
                        {
                            //TODO: clean up
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                            Property srcProp = new Property() { unityProperty = unitySrcProp };
                            Property dstProp = new Property() { unityProperty = unityDstProp };
                            Property dstPropGuess = null;
#else
                            SerializedProperty srcProp = unitySrcProp;
                            SerializedProperty dstProp = unityDstProp;
                            SerializedProperty dstPropGuess = null;
#endif

                            int subChanges = 0;

                            bool enteredArray = false; // I don't know if it is or not but it doesn't matter all that much
                            RevertProp(, srcProp, null, ref dstPropGuess, out _, false, enteredArray, ref subChanges, dstProp: dstProp);

                            if (dstPropGuess != null)
                                dstPropGuess.Dispose();

                            if (subChanges > 0)
                                if (Helper.LogUnnecessary)
                                    Debug.Log("RevertAssetFromParentAsset called for SerializedProperty: " + path + Helper.LOG_END);
                            changes += subChanges;

                            propertyPaths.RemoveAt(i);
                        }
                    }
                }
            }
            */

            if (changes > S.REGULAR_RECORD_UNDO_MAX_CHANGES || alreadyRecordedUndo)
            {
                if (!alreadyRecordedUndo)
                    Undo.RegisterCompleteObjectUndo(asset, "RevertAssetFromParentAsset");
                if (dst.ApplyModifiedPropertiesWithoutUndo() && asset != null)
                    EditorUtility.SetDirty(asset); //?
            }
            else
            {
                if (dst.ApplyModifiedProperties() && asset != null)
                    EditorUtility.SetDirty(asset);
            }

            src.Dispose();
            dst.Dispose();
            #endregion

            CustomRevertAssetFromParentAsset crafpa;
            if (customRevertAssetFromParentAsset.TryGetValue(asset.GetType(), out crafpa))
            {
                if (crafpa != null && crafpa(this, parentAsset))
                    changes++;
            }

            if (changes > 0)
            {
                childrenNeedUpdate = true;

                if (savingPaths != null)
                    savingPaths.Add(assetID.guid.ToPath()); //?
            }

            return changes > 0;
        }
        public delegate bool CustomRevertAssetFromParentAsset(AV av, Object parentAsset);
        public static Dictionary<Type, CustomRevertAssetFromParentAsset> customRevertAssetFromParentAsset = new Dictionary<Type, CustomRevertAssetFromParentAsset>();

        private bool updatingFromParent = false;
        public void UpdateAssetFromParentAsset(AV selfParent = null)
        {
            if (updatingFromParent)
            {
                Debug.LogError("Possible cyclical variant dependency found at asset of path: " + asset.Path() + Helper.LOG_END);
                return;
            }

            updatingFromParent = true;
            {
                RevertAssetFromParentAsset(selfParent); //null, 
                if (childrenNeedUpdate)
                    PropagateChangesToChildren();
            }
            updatingFromParent = false;
        }

#if DEBUG_ASSET_VARIANTS
        private static bool timingPropagateChangesToChildren = false;
#endif
        public void PropagateChangesToChildren() //RecursivelyApplyChangesToChildren
        {
#if DEBUG_ASSET_VARIANTS
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            bool timing = !timingPropagateChangesToChildren;
            if (timing)
                sw.Start();
            timingPropagateChangesToChildren = true;
#endif

            childrenNeedUpdate = false; // Needs to be on top
            for (int i = 0; i < data.children.Count; i++)
            {
                using (AV childAV = Open(data.children[i], null))
                {
                    Debug.Log("Calling UpdateAssetFromParentAsset()" + Helper.LOG_END);
                    if (!childAV.Valid())
                        Debug.LogError("Could not call UpdateAssetFromParentAsset() for an invalid child." + Helper.LOG_END);
                    else
                        childAV.UpdateAssetFromParentAsset(this);
                }
            }

#if DEBUG_ASSET_VARIANTS
            if (timing)
            {
                timingPropagateChangesToChildren = false;
                Debug.Log("Root PropagateChangesToChildren duration: " + sw.Elapsed.TotalMilliseconds);
            }
#endif
        }
        public void FlushAnyChangesToChildren()
        {
            if (childrenNeedUpdate)
            {
                PropagateChangesToChildren();
            }
        }

        /// <summary>
        /// This updates AVTargets.rvUndrawnPathsDirty to true when the overridesCache.dirty=true
        /// </summary>
        public void FlushOverridesCacheDirty()
        {
            if (overridesCache != null && overridesCache.dirty)
            {
                overridesCache.dirty = false;

                for (int i = 0; i < AVTargets.all.Count; i++)
                {
                    var avTargets = AVTargets.all[i];
                    if (avTargets.Contains(this))
                        avTargets.ClearOverridesCaching();
                }
            }
        }

        public void FlushDirty()
        {
            FlushImplicit(true);
            if (UnsavedUserData)
                SaveUserDataWithUndo(true, false);
            FlushAnyChangesToChildren();
        }

        #region Skipping Duplicates
        private static void InitSkipDuplicates()
        {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            odinVisitedObjects.Clear();
#endif
            ManagedReferencesHelper.InitSkipDuplicates();
        }

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
        private static bool SkipDuplicate(InspectorProperty prop)
        {
#if !ODIN_INSPECTOR_3
            //prop.Path=="InjectedMember"
            //TODO:::: what is that?
            if (prop.ValueEntry == null)
                return false;
#endif
            var value = prop.GetWeakValue();
            if (value == null)
                return false;
            if (value.GetType().IsValueType) //?
                return false;
            return !odinVisitedObjects.Add(value);
        }

        private static readonly HashSet<object> odinVisitedObjects = new HashSet<object>(); //TODO: contain the InspectorProperty instead?
        private static bool SkipDuplicate(Property prop, bool odinPass, out int prevDepth)
        {
            if (odinPass)
            {
                prevDepth = default(int);
                return SkipDuplicate(prop.odinProperty);
            }
            else
                return ManagedReferencesHelper.SkipDuplicate(prop.ToSP(), out prevDepth);
        }
#else
        private static bool SkipDuplicate(SerializedProperty prop, bool _, out int prevDepth)
        {
            return ManagedReferencesHelper.SkipDuplicate(prop, out prevDepth);
        }
#endif
        #endregion

        private static readonly List<PropertyKeyPair> srcPropertyKeyPairs = new List<PropertyKeyPair>();
        private static readonly List<PropertyKeyPair> dstPropertyKeyPairs = new List<PropertyKeyPair>();
        private static int IndexOfKey(List<PropertyKeyPair> keyPairs, string key)
        {
            for (int i = 0; i < keyPairs.Count; i++)
            {
                if (keyPairs[i].key == key)
                    return i;
            }
            return -1;
        }
        private static readonly List<string> removeKeys = new List<string>();
        private static readonly List<object> addProperties = new List<object>();
        private static void DisposePropertyKeyPairs()
        {
            PropertyKeyPair.Dispose(srcPropertyKeyPairs);
            PropertyKeyPair.Dispose(dstPropertyKeyPairs);
        }

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
        private struct DHSElement
        {
            public string overridePath;
            public InspectorProperty property;
        }
        private static readonly List<DHSElement> srcElements = new List<DHSElement>();
        private static readonly List<DHSElement> dstElements = new List<DHSElement>();
        private static void GetDictionaryHashSetElements(PathState pathState, PropertyChildren srcChildren, PropertyChildren dstChildren, bool isDictionary)
        {
            srcElements.Clear();
            for (int i = 0; i < srcChildren.Count; i++)
            {
                var srcChild = srcChildren[i];
                //if (isDictionary)
                //Debug.Log(srcChild.Name + " - " + srcChild.UnityPropertyPath);
                string path;
                if (isDictionary)
                    path = pathState.Path + pathState.GetLocalPath(srcChild.UnityPropertyPath); // Helper.LocalOverridePath(srcChild.UnityPropertyPath)
                else
                    path = pathState.Path + Helper.GetHashSetLocalOverridePath(srcChild);
                //Debug.Log(path);
                srcElements.Add(new DHSElement() { overridePath = path, property = srcChild }); //TODO: use .Path instead, for performance?
            }
            dstElements.Clear();
            for (int i = 0; i < dstChildren.Count; i++)
            {
                var dstChild = dstChildren[i];
                //if (isDictionary)
                //Debug.Log(dstChild.Name + " - " + dstChild.UnityPropertyPath);
                string path;
                if (isDictionary)
                    path = pathState.Path + pathState.GetLocalPath(dstChild.UnityPropertyPath); // Helper.LocalOverridePath(srcChild.UnityPropertyPath)
                else
                    path = pathState.Path + Helper.GetHashSetLocalOverridePath(dstChild);
                //Debug.Log(path);
                dstElements.Add(new DHSElement() { overridePath = path, property = dstChild });
            }
        }
#endif

        private bool COFDDoProp(PathState pathState, Property srcProp, Property parentDstProp, ref Property dstPropGuess, List<string> differences, out bool missing, bool odinPass, bool enteredArray, Property dstProp = null)
        {
            // If there is no similarity (no shared property values)
            // then an outer override can be made. Otherwise, overrides are created for each different child property.
            bool hasSimilarity;

            int prevDepth;
            if (SkipDuplicate(srcProp, odinPass, out prevDepth))
            {
                // (Not quite right, but good enough)
                hasSimilarity = false;
                missing = false;
                goto Exit;
            }

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (odinPass && srcProp.odinProperty.Info.PropertyType != PropertyType.Value)
            {
                Debug.LogWarning("Something went wrong: " + srcProp.odinProperty.Info.PropertyType);
                hasSimilarity = false;
                missing = false;
                goto Exit;
            }
#endif

            string path = pathState.Path;

            if (overridesCache.Contains(path))
            {
                hasSimilarity = false;
                missing = false;
                goto Exit;
            }
            string name = srcProp.name;
            if (srcFilter.ShouldIgnore(name, path, enteredArray) || dstFilter.ShouldIgnore(name, path, enteredArray))
            {
                hasSimilarity = false;
                missing = false;
                goto Exit;
            }

            if (parentDstProp != null)
                dstProp = parentDstProp.FindChild(pathState, srcProp, dstPropGuess);
            if (dstProp != null && dstProp.editable)
            {
                bool currentlyInvalid = false;
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                // This might never happen, where an invalid has a valid child.
                currentlyInvalid = Helper.CurrentlyInvalid(srcProp, dstProp, odinPass);
#endif

                missing = false;

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                bool dstHasChildren = Helper.HasChildren(dstProp);
                bool dstCanHaveChildren = dstHasChildren ||
                    (odinPass && !dstProp.odinIsSize && dstProp.odinProperty.IsCollection()); // Dictionary and HashSet need .IsCollection

                // So that a null collection will be copied as a whole
                if (dstCanHaveChildren && odinPass)
                {
                    if (srcProp.odinProperty.GetWeakValue() == null)
                        dstCanHaveChildren = false;
                    else if (dstProp.odinProperty.GetWeakValue() == null)
                        dstCanHaveChildren = false;
                }

                bool matchesType = dstProp.CompareType(srcProp);
#else
                bool dstCanHaveChildren = Helper.HasChildren(dstProp);
                bool matchesType = Helper.PropertyTypesMatch(dstProp.propertyType, srcProp.propertyType); //TODO2: compare .type and .arrayElementType?
#endif

                bool dstIsCollectionOrHasVisibleChildren = odinPass ? dstCanHaveChildren : (dstProp.ToSP().hasVisibleChildren || dstProp.ToSP().isArray); //? //TODO2: in newer versions is the .Array.size now not a visible child? //InvalidOperationException: 'vertices' is an array so it cannot be read with boxedValue. UnityEditor.SerializedProperty.get_structValue()

                var srcEmulation = (odinPass || currentlyInvalid) ? null : srcFilter.GetEmulationProperty(path);
                var dstEmulation = (odinPass || currentlyInvalid) ? null : dstFilter.GetEmulationProperty(path); //Debug.Log(srcEmulation + " // " + dstEmulation + " /// " + path);
                if (dstEmulation != null)
                {
                    if (srcEmulation == null)
                        srcEmulation = DefaultEmulationProperty.instance;
                    srcPropertyKeyPairs.Clear();
                    srcEmulation.GetChildren(srcProp, srcPropertyKeyPairs);

                    dstPropertyKeyPairs.Clear();
                    dstEmulation.GetChildren(dstProp, dstPropertyKeyPairs);

                    List<string> subDifferences = new List<string>();
                    bool anyKeysMatch = false;
                    bool anySerializedProperties = false;
                    bool anySimilarSerializedProperties = false;

                    for (int i = 0; i < srcPropertyKeyPairs.Count; i++)
                    {
                        var srcPair = srcPropertyKeyPairs[i];
                        var key = srcPair.key;
                        var dstID = IndexOfKey(dstPropertyKeyPairs, key);
                        if (dstID != -1)
                        {
                            anyKeysMatch = true;

                            var dstPair = dstPropertyKeyPairs[dstID];
                            if (srcPair.keySP != null && dstPair.keySP != null && // Checks for key existing because there's nothing to gain when diving into HashSet elements; If the key matches, there intrinsically are no sub differences
                                srcPair.property is Property childSrcProp && dstPair.property is Property childDstProp)
                            {
                                anySerializedProperties = true;

                                bool missingChild;
                                var childPathState = pathState.Child(childSrcProp.propertyPath, childDstProp.propertyPath, Helper.LocalKeyPath(key));
                                Property discard = null;
                                if (COFDDoProp(childPathState, childSrcProp, null, ref discard, subDifferences, out missingChild, odinPass, enteredArray, childDstProp))
                                    anySimilarSerializedProperties = true;
                                if (missingChild)
                                    Debug.LogError("How? 2");
                            }
                        }
                    }

                    // I feel like this is good, right?
                    bool childrenHaveSimilarity;
                    if (anySerializedProperties)
                        childrenHaveSimilarity = anySimilarSerializedProperties;
                    else // It's a HashSet
                        childrenHaveSimilarity = anyKeysMatch;

                    if (childrenHaveSimilarity)
                    {
                        differences.AddRange(subDifferences);

                        // Checks if dst is missing any from src
                        for (int i = 0; i < srcPropertyKeyPairs.Count; i++)
                        {
                            string key = srcPropertyKeyPairs[i].key;
                            if (IndexOfKey(dstPropertyKeyPairs, key) == -1)
                                differences.Add(Helper.KeyPath(path, key));
                        }
                        // Checks if src is missing any from dst
                        for (int i = 0; i < dstPropertyKeyPairs.Count; i++)
                        {
                            string key = dstPropertyKeyPairs[i].key;
                            if (IndexOfKey(srcPropertyKeyPairs, key) == -1)
                                differences.Add(Helper.KeyPath(path, key));
                        }

                        hasSimilarity = true;
                    }
                    else
                    {
                        bool allDifferencesAreOverridden = true;
                        for (int i = 0; i < srcPropertyKeyPairs.Count; i++)
                        {
                            string key = srcPropertyKeyPairs[i].key;
                            if (IndexOfKey(dstPropertyKeyPairs, key) == -1)
                            {
                                if (!overridesCache.Contains(Helper.KeyPath(path, key)))
                                {
                                    allDifferencesAreOverridden = false;
                                    break;
                                }
                            }
                        }
                        if (allDifferencesAreOverridden)
                        {
                            for (int i = 0; i < dstPropertyKeyPairs.Count; i++)
                            {
                                string key = dstPropertyKeyPairs[i].key;
                                if (IndexOfKey(srcPropertyKeyPairs, key) == -1)
                                {
                                    if (!overridesCache.Contains(Helper.KeyPath(path, key)))
                                    {
                                        allDifferencesAreOverridden = false;
                                        break;
                                    }
                                }
                            }
                        }

                        // No keys are the same, might as well override the whole property
                        if (!allDifferencesAreOverridden) //srcPropertyKeyPairs.Count > 0 || dstPropertyKeyPairs.Count > 0)
                            differences.Add(path);
                        hasSimilarity = false;
                    }

                    DisposePropertyKeyPairs();
                }
                else if (!currentlyInvalid && (!dstCanHaveChildren || (!dstIsCollectionOrHasVisibleChildren && S.S.createParentOverrideIfChildrenAreInvisible))
                    && (matchesType || !S.S.propertyTypesNeedToMatch))
                {
                    //Debug.Log(path + " Doing Single " + dstCanHaveChildren + " " + dstIsCollectionOrHasVisibleChildren + " " + S.S.createParentOverrideIfChildrenAreInvisible);

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    //if (odinPass)
                    //    Debug.Log(srcProp.odinProperty.Path + " -- " + dstProp.odinProperty.Path + " -- " + pathState.iterationOriginalPath);
#endif
                    if (Helper.AreEqual(srcProp, dstProp))
                    {
                        hasSimilarity = true;
                    }
                    else
                    {
                        differences.Add(path);
                        hasSimilarity = false;
                    }
                }
                else
                {
                    List<string> subDifferences = new List<string>();
                    bool childrenHaveSimilarity = false;

                    bool canCopyChildren = dstCanHaveChildren && Helper.HasChildren(srcProp);

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    if (odinPass)
                    {
                        bool isHashSet = Helper.IsHashSet(srcProp.odinProperty);
                        if (isHashSet)
                            canCopyChildren = false;
                        bool isDictionary = false;
                        if (!srcProp.odinIsSize && dstProp.CompareType(srcProp))
                            isDictionary = Helper.IsDictionary(srcProp.odinProperty);
                        else
                            isHashSet = false;
                        if (isHashSet || (isDictionary && !overridesCache.Contains(path + ".DictionarySize")))
                        {
                            //Debug.Log(srcChildren.Count + " -- " + dstChildren.Count);
                            //for (int i = 0; i < srcChildren.Count; i++)
                            //    Debug.Log(srcChildren[i].Path);
                            //for (int i = 0; i < dstChildren.Count; i++)
                            //    Debug.Log(dstChildren[i].Path);

                            GetDictionaryHashSetElements(pathState, srcProp.odinProperty.Children, dstProp.odinProperty.Children, isDictionary);

                            for (int i = 0; i < srcElements.Count; i++)
                            {
                                var srcE = srcElements[i];
                                bool found = false;
                                for (int ii = 0; ii < dstElements.Count; ii++)
                                {
                                    if (srcE.overridePath == dstElements[ii].overridePath)
                                    {
                                        found = true;
                                        childrenHaveSimilarity = true;
                                        break;
                                    }
                                }
                                if (!found && !overridesCache.Contains(srcE.overridePath))
                                    subDifferences.Add(srcE.overridePath);
                            }

                            for (int i = 0; i < dstElements.Count; i++)
                            {
                                var dstE = dstElements[i];
                                bool found = false;
                                for (int ii = 0; ii < srcElements.Count; ii++)
                                {
                                    if (dstE.overridePath == srcElements[ii].overridePath)
                                    {
                                        found = true;
                                        childrenHaveSimilarity = true;
                                        break;
                                    }
                                }
                                if (!found && !overridesCache.Contains(dstE.overridePath))
                                    subDifferences.Add(dstE.overridePath);
                            }

                            //Debug.Log(path + " - Dictionary/HashSet: " + subDifferences.Count + " // " + srcElements.Count + " -- " + dstElements.Count);
                        }
                    }
#endif

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
                    if (odinPass ? dstProp.odinProperty.IsCollection() : dstProp.unityProperty.isArray) enteredArray = true;
#else
                    if (dstProp.isArray) enteredArray = true;
#endif

                    if (canCopyChildren)
                    {
                        using (var srcChildrenIterator = srcProp.Copy())
                        {
                            int depth = srcChildrenIterator.depth;

                            if (srcChildrenIterator.NextChildSkipArray())
                            {
                                var childDstPropGuess = dstPropGuess;
                                if (childDstPropGuess != null)
                                {
                                    childDstPropGuess = childDstPropGuess.Copy();
                                    if (!childDstPropGuess.NextChildSkipArray())
                                        childDstPropGuess = null;
                                }

                                do
                                {
                                    bool missingChild;
                                    if (COFDDoProp(pathState.Child(srcChildrenIterator.propertyPath), srcChildrenIterator, dstProp, ref childDstPropGuess, subDifferences, out missingChild, odinPass, enteredArray))
                                        childrenHaveSimilarity = true;

                                    if (!srcChildrenIterator.Next(false))
                                        break;
                                    if (childDstPropGuess != null && !childDstPropGuess.Next(false))
                                        childDstPropGuess = null;
                                }
                                while (depth < srcChildrenIterator.depth);

                                if (childDstPropGuess != null)
                                    childDstPropGuess.Dispose();
                            }
#if !ASSET_VARIANTS_DO_ODIN_PROPERTIES //TODO: add .serializedObject to Property
                            else
                                Debug.LogWarning("Something unexpected happened at: " + srcProp.propertyPath + " on: " + srcProp.serializedObject + Helper.LOG_END);
#endif
                        }
                    }

                    //Debug.Log(path + " Sub Differences: " + subDifferences.Count);

                    if (!currentlyInvalid && !childrenHaveSimilarity)
                    {
                        if (subDifferences.Count > 0)
                            differences.Add(path);
                        hasSimilarity = false;
                    }
                    else
                    {
                        differences.AddRange(subDifferences);
                        hasSimilarity = childrenHaveSimilarity;
                    }
                }
            }
            else
            {
                missing = true;
                hasSimilarity = false;
            }

        Exit:

            ManagedReferencesHelper.RestoreSkipDuplicateDepth(prevDepth);

            return hasSimilarity;
        }
        public void CreateOverridesFromDifferences()
        {
            var parentAsset = LoadParentAsset();
            if (parentAsset == null)
            {
                Debug.LogWarning("CreateOverridesFromDifferences called for parentless asset: " + asset.Path() + Helper.LOG_END);
                return;
            }

            bool dirty = true;

            GetOverridesCache();
            srcFilter = PropertyFilter.Get(parentAsset.GetType());
            dstFilter = PropertyFilter.Get(asset.GetType());

            List<string> allDifferences = new List<string>();

            #region Unity Pass
            var src = new SerializedObject(parentAsset);
            var dst = new SerializedObject(asset);

            InitSkipDuplicates();

            var outerSrcIterator = src.GetUnityIterator();
            var outerDstIterator = dst.GetUnityIterator();
            Property dstPropGuess = null;
            bool _;
            if (outerSrcIterator.NextVisible(true))
            {
                while (true)
                {
                    COFDDoProp(PathState.Same(outerSrcIterator.propertyPath), outerSrcIterator, outerDstIterator, ref dstPropGuess, allDifferences, out _, false, false);

                    if (!outerSrcIterator.Next(false))
                        break;
                    if (dstPropGuess != null && !dstPropGuess.Next(false))
                        dstPropGuess = null;
                }
            }
            outerSrcIterator.Dispose();
            outerDstIterator.Dispose();
            if (dstPropGuess != null)
                dstPropGuess.Dispose();

            src.Dispose();
            dst.Dispose();
            #endregion

            #region Odin Pass
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (asset.IsOdinSerializable())
            {
                InitSkipDuplicates();

                RemoveAttributeProcessor.active = true;
                RawPropertyResolver.active = true;

                var odinSrc = PropertyTree.Create(parentAsset);
                var odinDst = PropertyTree.Create(asset);
                odinSrc.AttributeProcessorLocator = skipAttributeProcessorLocator;
                odinDst.AttributeProcessorLocator = skipAttributeProcessorLocator;

                outerSrcIterator = odinSrc.GetOdinIterator();
                outerDstIterator = odinDst.GetOdinIterator();
                dstPropGuess = null;
                if (outerSrcIterator.NextVisible(true, true))
                {
                    while (true)
                    {
                        if (!Helper.IsUnitySerialized(outerSrcIterator.odinProperty))
                            COFDDoProp(PathState.Same(outerSrcIterator.propertyPath), outerSrcIterator, outerDstIterator, ref dstPropGuess, allDifferences, out _, true, false);

                        if (!outerSrcIterator.Next(false))
                            break;
                        if (dstPropGuess != null && !dstPropGuess.Next(false))
                            dstPropGuess = null;
                    }
                }
                outerSrcIterator.Dispose();
                outerDstIterator.Dispose();
                if (dstPropGuess != null)
                    dstPropGuess.Dispose();

                odinSrc.Dispose();
                odinDst.Dispose();

                RawPropertyResolver.active = false;
                RemoveAttributeProcessor.active = false;
            }
#endif
            #endregion

            CustomCreateOverridesFromDifferences ccofd;
            if (customCreateOverridesFromDifferences.TryGetValue(asset.GetType(), out ccofd))
            {
                if (ccofd != null)
                    ccofd(this, parentAsset, allDifferences);
            }

            for (int i = 0; i < allDifferences.Count; i++)
            {
                if (!overridesCache.Contains(allDifferences[i]))
                {
                    if (Helper.LogUnnecessary)
                        Debug.Log("Created override from differences: " + allDifferences[i] + "\nFor " + asset.GetType() + ": " + asset.name + Helper.LOG_END);

                    overridesCache.Add(allDifferences[i]);
                    dirty = true;
                }
            }

            if (dirty)
                SaveUserDataWithUndo();
        }
        public delegate void CustomCreateOverridesFromDifferences(AV av, Object parentAsset, List<string> allDifferences);
        public static Dictionary<Type, CustomCreateOverridesFromDifferences> customCreateOverridesFromDifferences = new Dictionary<Type, CustomCreateOverridesFromDifferences>();

        [Serializable]
        public class Data
        {
            public AssetID parent = AssetID.Null;
            public List<AssetID> children = new List<AssetID>();
            public List<string> overrides = new List<string>();

            public bool HasParent => !string.IsNullOrEmpty(parent.guid);

            public bool HasParentlessOverrides => !HasParent && overrides.Count > 0;

            public bool Exists => HasParent || children.Count > 0 || overrides.Count > 0;

            public string ToJSON()
            {
                if (Exists)
                    return JsonUtility.ToJson(this);
                else
                    return "";
            }
        }
        public Data data;

        internal OverridesCache overridesCache = null;
        public OverridesCache GetOverridesCache()
        {
            if (overridesCache == null)
            {
                overridesCache = new OverridesCache();
                overridesCache.Init(data.overrides);
            }
            return overridesCache;
        }
        public void UpdateOverridesCache()
        {
            if (overridesCache != null)
                overridesCache.Init(data.overrides);
        }
        public bool IsOverridden(string path)
        {
            if (overridesCache != null)
                return overridesCache.Contains(path);
            else
                return data.overrides.Contains(path);
        }
        public bool TryCreateOverride(string path)
        {
            if (overridesCache != null)
            {
                return overridesCache.Add(path);
            }
            else
            {
                if (data.overrides.Contains(path))
                    return false;
                data.overrides.Add(path);
                return true;
            }
        }
        public bool RemoveOverride(string path)
        {
            if (overridesCache != null)
                return overridesCache.Remove(path);
            else
                return data.overrides.Remove(path);
        }

        private HashSet<Object> descendants = null;
        public HashSet<Object> GetDescendants()
        {
            if (descendants == null)
            {
                descendants = new HashSet<Object>();
                FillDescendants(descendants);
            }

            return descendants;
        }
        private void FillDescendants(HashSet<Object> descendants)
        {
            for (int i = 0; i < data.children.Count; i++)
            {
                var child = data.children[i].Load();
                if (child == null)
                {
                    Debug.LogWarning("?");
                    continue;
                }

                descendants.Add(child);

                using (var childAV = Open(child))
                {
                    if (childAV.Valid())
                        childAV.FillDescendants(descendants);
                }
            }
        }
        public void ClearDescendants()
        {
            //TODO: AVEditor should call this?

            descendants = null;
        }
        public static void ClearAllDescendants()
        {
            foreach (var kv in openAVs)
                kv.Value.ClearDescendants();
        }

        public bool childrenNeedUpdate = false; // If the properties of the asset have changed and need to be applied to the children

        public void TryAddChild(AssetID newChild)
        {
            using (AV newChildAV = Open(newChild, null))
            {
                if (newChildAV == null)
                    return;

                if (newChildAV.data.parent != assetID)
                {
                    newChildAV.data.parent = assetID;
                    newChildAV.SaveUserDataWithUndo();

                    if (Helper.LogUnnecessary)
                        Debug.Log("Added child: " + newChildAV.asset + Helper.LOG_END);
                }
            }

            if (!data.children.Contains(newChild))
            {
                data.children.Add(newChild);
                SaveUserDataWithUndo();

                HierarchyChanged();
            }
        }
        public void TryRemoveChild(AssetID oldChild)
        {
            using (AV oldChildAV = Open(oldChild, null))
            {
                if (oldChildAV == null)
                {
                    if (data.children.Remove(oldChild))
                        SaveUserDataWithUndo();
                }
                else
                {
                    if (oldChildAV.data.parent == assetID)
                    {
                        oldChildAV.data.parent = AssetID.Null;
                        oldChildAV.SaveUserDataWithUndo();

                        if (Helper.LogUnnecessary)
                            Debug.Log("Removed child: " + oldChildAV.asset + Helper.LOG_END);
                    }

                    if (data.children.Remove(oldChild))
                        SaveUserDataWithUndo();

                    HierarchyChanged();
                }
            }
        }

        public void HierarchyChanged()
        {
            descendants = null;
            OverrideIndicator.ClearCaches();
            AVHeader.InitAllTargets();
        }

        private static readonly List<AV> cyclicalSearch = new List<AV>();
        private void SearchCyclical()
        {
            for (int i = 0; i < data.children.Count; i++)
            {
                var childAV = Open(data.children[i], null);
                if (!cyclicalSearch.Contains(childAV))
                {
                    cyclicalSearch.Add(childAV);
                    childAV.SearchCyclical();
                }
                else
                    childAV.Dispose();
            }
        }
        private static void ClearCyclical()
        {
            for (int i = 0; i < cyclicalSearch.Count; i++)
                cyclicalSearch[i].Dispose();
            cyclicalSearch.Clear();
        }

        public void ChangeParent(AssetID newParent)
        {
            if (newParent == data.parent)
                return;

            if (newParent == assetID)
            {
                Debug.LogError("Can't set Parent to itself: " + assetID + Helper.LOG_END);
                return;
            }

            if (data.HasParent)
            {
                using (AV oldParentAV = Open(data.parent, null))
                {
                    if (oldParentAV != null)
                        oldParentAV.TryRemoveChild(assetID);
                }
            }

            using (AV newParentAV = Open(newParent, null))
            {
                if (newParentAV != null)
                {
                    ClearCyclical();
                    SearchCyclical();
                    if (cyclicalSearch.Contains(newParentAV))
                    {
                        data.parent = AssetID.Null;
                        SaveUserDataWithUndo();
                        Debug.LogError("Cyclical dependencies found. Did not set parent of: " + assetID.Load().Path() + "\nto: " + newParent.Load().Path() + Helper.LOG_END);
                    }
                    else
                    {
                        if (Helper.LogUnnecessary)
                            Debug.Log("Changed parent from: " + data.parent.Load() + " to: " + newParentAV.asset + Helper.LOG_END);

                        data.parent = newParent;
                        SaveUserDataWithUndo();
                        newParentAV.TryAddChild(assetID);
                        CreateOverridesFromDifferences();
                    }
                    ClearCyclical();
                }
                else
                {
                    data.parent = AssetID.Null;
                    SaveUserDataWithUndo();
                }
            }

            cachedParent = null;
            ClearAllDescendants(); // Easiest and safest to just clear all
        }
        internal static bool skipValidateRelations;
        public void ValidateRelations()
        {
            if (skipValidateRelations)
                return;

            if (EditorApplication.isUpdating)
            {
                Debug.LogWarning("EditorApplication.isUpdating==true. Shouldn't call ValidateRelations() now.");
                return;
            }

            // This makes sure all relations make sense

            bool dirty = false;

            if (data.parent == assetID)
            {
                Debug.LogWarning("Can't set Parent to itself" + Helper.LOG_END);
                data.parent = AssetID.Null;
                data.children.Remove(assetID);
                dirty = true;
            }
            else if (data.HasParent)
            {
                Type selfType = asset.GetType();
                Object parentAsset = LoadParentAsset();
                if (parentAsset == null)
                {
                    Debug.LogWarning(asset.Path() + "'s parent: " + data.parent + " does not exist, it could not be loaded. Parent is now set to null." + Helper.LOG_END);
                    data.parent = AssetID.Null;
                    dirty = true;
                }
                else
                {
                    Type parentType = parentAsset.GetType();
                    if (S.S.directInheritanceRequired && !parentType.IsAssignableFrom(selfType) && !selfType.IsAssignableFrom(parentType))
                    {
                        Debug.LogWarning("Parent type: " + parentType + " is not convertible to or from variant type: " + selfType + "\nIf you want to allow this, set AssetVariants.Settings.DIRECT_INHERITANCE_REQUIRED to false. Parent is now set to null." + Helper.LOG_END);
                        data.parent = AssetID.Null;
                        dirty = true;
                    }
                    else
                    {
                        using (AV parentAV = Open(data.parent, null))
                        {
                            parentAV.TryAddChild(assetID);
                        }
                    }
                }
            }

            for (int i = 0; i < data.children.Count; i++)
            {
                if (data.children[i].Load() == null) // TODO2: optimize?
                {
                    Debug.LogWarning(asset.Path() + " has a nonexistent child of AssetID: " + data.children[i] + "\nRemoved reference to it." + Helper.LOG_END);
                    data.children.RemoveAt(i);
                    i--;
                    dirty = true;
                }
                else
                {
                    using (AV childAV = Open(data.children[i], null))
                    {
                        if (childAV.data.parent != assetID)
                        {
                            Debug.LogWarning("Child " + data.children[i] + " does not have " + assetID + " as Parent" + Helper.LOG_END);
                            data.children.RemoveAt(i);
                            i--;
                            dirty = true;
                        }
                    }
                }
            }

            if (dirty)
                SaveUserDataWithUndo(); // TODO2: Should this skip recording undo?
        }

        public void LoadUserData()
        {
            if (Importer == null)
            {
                Importer = AssetImporter.GetAtPath(assetID.guid.ToPath());
                if (Importer == null)
                {
                    Debug.LogError("Importer == null");
                    return;
                }
            }

            SharedUserDataUtility.ValidateOwnership validateOwnership;
            string userData = Importer.GetUserData(UserDataKey, out validateOwnership);

            if (validateOwnership == null) // Asset Variants has never been used without SharedUserDataUtility, no need to use validateOwnership
            {
                data = JsonUtility.FromJson<Data>(userData);
                if (data == null)
                    data = new Data();
            }
            else
            {
                // If validateOwnership exists then the userData cannot be for Asset Variants.
                data = new Data();
            }

            UpdateOverridesCache();
        }

        internal static HashSet<string> userDataDirtyPaths = new HashSet<string>(); // This might be a bad solution but it has been so long since I wrote this code so idk

        public bool SaveUserDataWithUndo(bool force = false, bool undo = true, int undoGroup = int.MinValue)
        {
            if (!force && S.S.onlySaveUserDataOnLeave)
            {
                UnsavedUserData = true;
                return false;
            }
            UnsavedUserData = false;

            if (force)
                if (Helper.LogUnnecessary)
                    Debug.Log("Forced save of Asset Variants userData" + Helper.LOG_END);

            string newUserData = data.ToJSON();
            if (Importer.SetUserData(UserDataKey, newUserData, undo ? "Modify Asset Variants Data" : null))
            {
                userDataDirtyPaths.Add(Importer.assetPath);

                if (undoGroup != int.MinValue)
                {
                    //Debug.Log("Collapsing: " + undoGroup + " -- Current: " + Undo.GetCurrentGroup());
                    Undo.FlushUndoRecordObjects();
                    Undo.CollapseUndoOperations(undoGroup);
                }
                EditorUtility.SetDirty(Importer); // I don't know if this is necessary
                return true;
            }

            return false;
        }
        private bool _unsavedUserData = false;
        private bool UnsavedUserData
        {
            get
            {
                if (_unsavedUserData)
                    return true;
                if (overridesCache != null)
                    return overridesCache.userDataDirty;
                return false;
            }
            set
            {
                if (value)
                {
                    _unsavedUserData = true;
                }
                else
                {
                    _unsavedUserData = false;
                    if (overridesCache != null)
                        overridesCache.userDataDirty = false;
                }
            }
        }

        public static void LoadAndValidateAll(string guid)
        {
            List<AV> avs = new List<AV>();
            foreach (var kv in openAVs)
            {
                if (kv.Key.guid == guid)
                    avs.Add(kv.Value);
            }
            for (int i = 0; i < avs.Count; i++)
            {
                avs[i].LoadUserData();
                avs[i].ValidateRelations();
            }
        }

        public static AV Open(Object asset)
        {
            return Open(asset.ToAssetID(), asset);
        }
        public static AV Open(AssetID assetID, Object asset)
        {
            AV openAV;
            if (openAVs.TryGetValue(assetID, out openAV))
            {
                openAV.UserCount++;
                return openAV;
            }
            var importer = AssetImporter.GetAtPath(assetID.guid.ToPath());
            if (importer == null)
                return null;
            if (asset == null)
            {
                asset = assetID.Load();
                if (asset == null)
                    return null;
            }
            AV av = new AV(assetID, asset, importer);
            openAVs.Add(assetID, av);
            av.UserCount = 1;
            av.LoadUserData();
            return av;
        }
        private static readonly Dictionary<AssetID, AV> openAVs = new Dictionary<AssetID, AV>();

        internal void IncrementUserCount()
        {
            UserCount++;
        }

        public int UserCount { get; private set; } = 0;
        private AV(AssetID assetID, Object asset, AssetImporter importer)
        {
            this.assetID = assetID;
            this.asset = asset;
            this.Importer = importer;
        }
        public void Dispose()
        {
            UserCount--;
            if (UserCount == 0)
            {
                FlushDirty();

                //if (!EditorApplication.isUpdating)
                //{
                //    if (Helper.IsDirty(importer) && S.S.saveAndReimportOnLeave)
                //    {
                //        importer.SaveAndReimport(); // (Note that this calls the Editor's OnDisable())
                //        if (Helper.LogUnnecessary)
                //            Debug.Log("AssetVariant SaveAndReimport() called automatically");
                //    }
                //}

                if (UserCount == 0)
                    openAVs.Remove(assetID);

                if (implicitSrc != null)
                {
                    implicitSrc.Dispose();
                    implicitSrc = null;
                }
                if (implicitDst != null)
                {
                    implicitDst.Dispose();
                    implicitDst = null;
                }
            }
        }

        private static void UndoRedoPerformed()
        {
            alreadyRenamedOverrides.Clear();

            ClearAllDescendants();

            foreach (var kv in openAVs)
            {
                if (!kv.Value.UnsavedUserData)
                {
                    try
                    {
                        kv.Value.LoadUserData();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
        }

        static List<string> savingPaths = null; // For AVAssetModificationProcessor to know which paths to add to the list (if that's even allowed)
        internal static void FlushAllDirty(List<string> savingPaths)
        {
            var prevSavingPaths = AV.savingPaths;
            try
            {
                AV.savingPaths = savingPaths;
                List<AV> all = new List<AV>(); // To prevent InvalidOperationException: Collection was modified; enumeration operation may not execute.
                foreach (var kv in openAVs)
                    all.Add(kv.Value);
                for (int i = 0; i < all.Count; i++)
                    all[i].FlushDirty();
                all.Clear();
            }
            finally
            {
                AV.savingPaths = prevSavingPaths;
            }
        }


        [InitializeOnLoadMethod]
        private static void Init()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;

            EditorApplication.playModeStateChanged += (c) =>
            {
                //Debug.Log(c);
                if (c == PlayModeStateChange.ExitingEditMode)
                    FlushAllDirty(null);
            };
        }

        ~AV()
        {
            if (UserCount != 0)
                Debug.LogError("(UserCount != 0) - Implementation bug, fix it Steffen - UserCount: " + UserCount + "\n" + asset.name + Helper.LOG_END);
        }
    }
}

#endif
