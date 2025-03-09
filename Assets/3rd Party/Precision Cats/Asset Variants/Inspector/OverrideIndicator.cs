#if UNITY_EDITOR

#if !ODIN_INSPECTOR
#undef ASSET_VARIANTS_DO_ODIN_PROPERTIES
#endif
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EditorReplacement;
using PrecisionCats;
using Object = UnityEngine.Object;
using S = AssetVariants.AVSettings;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
#endif

namespace AssetVariants
{
    internal class OverrideIndicator : PropertyModifier
    {
        private struct SOCache
        {
            public AVTargets avTargets;
            public bool valid;
            public Dictionary<PropertyKey, bool> shoulds;
        }
        private struct PropertyKey
        {
            public string name, path;
            public PropertyKey(SerializedProperty prop)
            {
                name = prop.name;
                path = prop.propertyPath;
            }
        }
        internal static void ClearCaches()
        {
            caches.list.Clear();
        }
        //TODO: make this faster if serializedobjects just linger together with dead editors?
        private static readonly WeakTable<SerializedObject, SOCache> caches = new WeakTable<SerializedObject, SOCache>();
        private static bool ShouldUseFor(SerializedProperty property, out AVTargets avTargets)
        {
            //if (!Helper.ValidType(property.serializedObject.targetObject.GetType()))
            //{
            //    avTargets = null;
            //    return false;
            //}

            //Debug.Log("ASking for : " + property.propertyPath);

            bool should;

            var so = property.serializedObject;

            SOCache cache;
            if (caches.TryGetValue(so, out cache))
            {
                if (!cache.valid)
                {
                    avTargets = null;
                    return false;
                }

                if (cache.shoulds.TryGetValue(new PropertyKey(property), out should))
                {
                    if (should)
                    {
                        avTargets = cache.avTargets;
                        return true;
                    }
                    else
                    {
                        avTargets = null;
                        return false;
                    }
                }
            }
            else
            {
                cache = new SOCache();

                if (AVFilter.Should(so.targetObject))
                {
                    cache.avTargets = AVTargets.Get(so);
                    cache.valid = cache.avTargets.AVs != null && cache.avTargets.AnyHasParent; //TODO: but what if AVs comes to exist eventually?
                }
                else
                    cache.valid = false;

                if (cache.valid)
                    cache.shoulds = new Dictionary<PropertyKey, bool>();

                caches.Add(so, cache);

                if (!cache.valid)
                {
                    avTargets = null;
                    return false;
                }
            }

            var pt = property.propertyType;
            if (!property.editable || pt == SerializedPropertyType.FixedBufferSize)
            {
                should = false;
                avTargets = null;
            }
            else
            {
                var propertyPath = property.propertyPath;

                var path = cache.avTargets.ConvertToOverridePath(propertyPath, so);

                var pf = cache.avTargets.propertyFilter;

                should = !pf.ShouldIgnore(property.name, path);

                if (should)
                {
                    should = !cache.avTargets.ConsideredWhole(Helper.ParentPropertyPath(propertyPath), so, true);

                    if (should && pt == SerializedPropertyType.ArraySize)
                    {
                        //Removing .Array.size
                        var arrayPath = path.Substring(0, path.Length - 11); //var arrayPath = Helper.ParentPropertyPath(Helper.ParentPropertyPath(path)); // This is fine I believe despite not being a propertyPath
                        should = pf.GetEmulationProperty(arrayPath) == null;
                    }

                    if (should)
                    {
                        var kpps = cache.avTargets.GetPathConversionCache(so).keyPropertyPaths;
                        if (kpps != null)
                            should = !kpps.Contains(propertyPath);
                    }
                }

                ////#if ODIN_INSPECTOR
                ////                && (!imgui || !OdinOverrideDrawer.Active
                ////                || !avTargets.odinButtonedPaths.Contains(property.propertyPath))
                ////#endif
                if (should)
                    avTargets = cache.avTargets;
                else
                    avTargets = null;
            }

            cache.shoulds.Add(new PropertyKey(property), should);

            return should;
        }


        public static void OverridesMaybeChanged()
        {
#if UNITY_2019_1_OR_NEWER
            OverrideButton.dirtyStyles = true;
#endif
        }


        // Context Menu
        public delegate bool ApplyToAncestorEditable<PropType>(PropType dstProp);
        public delegate void ApplyToAncestorOpen<TreeType, PropType>(Object asset, Object ancestorAsset, out TreeType src, out TreeType dst, out PropType srcProp, out PropType dstProp, out bool alreadyCopied);
        public delegate void ApplyToAncestorCopy<PropType>(PropType srcProp, PropType dstProp);
        public delegate void ApplyToAncestorClose<TreeType>(Object ancestorAsset, TreeType src, TreeType dst);
        public static void ApplyToAncestor<TreeType, PropType>(IList<AV> avs, IList<AV> ancestorAVs, string path, ApplyToAncestorEditable<PropType> editable, ApplyToAncestorOpen<TreeType, PropType> open, ApplyToAncestorCopy<PropType> copy, ApplyToAncestorClose<TreeType> close)
        {
            for (int i = 0; i < avs.Count; i++)
            {
                var av = avs[i];
                if (av.UserCount == 0 || !av.data.HasParent)
                {
                    Debug.LogWarning("");
                    continue;
                }

                var ancestorAV = ancestorAVs[i];
                if (ancestorAV == null || ancestorAV.UserCount == 0)
                {
                    Debug.LogWarning("");
                    continue; //return;
                }

                //TODO2: validate ancestory?

                var oc = av.GetOverridesCache();
                oc.Remove(path);
                oc.RemoveChildrenPaths(path);
                av.SaveUserDataWithUndo();

                bool ancestorHasParent = false;

                var ancestorAsset = ancestorAV.asset;

                TreeType src, dst;
                PropType srcProp, dstProp;
                bool alreadyCopied;
                open(av.asset, ancestorAsset, out src, out dst, out srcProp, out dstProp, out alreadyCopied);
                if (alreadyCopied || (srcProp != null && dstProp != null && editable(dstProp)))
                {
                    if (ancestorAV.data.HasParent)
                    {
                        ancestorHasParent = true;

                        var ancestorOC = ancestorAV.GetOverridesCache();
                        ancestorOC.RemoveChildrenPaths(path);
                        ancestorOC.Add(path);
                        ancestorAV.SaveUserDataWithUndo();
                    }

                    if (!alreadyCopied)
                        copy(srcProp, dstProp);
                }
                else
                    Debug.LogError("No editable parent property: " + path + Helper.LOG_END);
                close(ancestorAsset, src, dst);

                // To make absolutely sure it works
                if (ancestorHasParent)
                    ancestorAV.RevertAssetFromParentAsset();

                ancestorAV.PropagateChangesToChildren();
            }

            if (Helper.LogUnnecessary)
                Debug.Log("Applied to ancestor any value/s and override/s for: " + path + Helper.LOG_END);

            OverridesMaybeChanged();
        }

        public static void TryCreateOverride(AVTargets avTargets, string path, bool keepChildren)
        {
            if (avTargets.AVs != null)
            {
                for (int i = 0; i < avTargets.AVs.Length; i++)
                {
                    var av = avTargets.AVs[i];
                    if (av.UserCount == 0)
                        continue;

                    var oc = av.GetOverridesCache();
                    if (oc.Add(path))
                    {
                        if (!keepChildren)
                            oc.RemoveChildrenPaths(path);
                        av.SaveUserDataWithUndo();

                        OverridesMaybeChanged();
                    }
                }

                if (Helper.LogUnnecessary)
                    Debug.Log("Created override for: " + path + Helper.LOG_END);
            }
        }
        public static void TryRemoveOverride(AVTargets avTargets, string path, bool children)
        {
            if (avTargets.AVs != null)
            {
                for (int i = 0; i < avTargets.AVs.Length; i++)
                {
                    var av = avTargets.AVs[i];
                    if (av.UserCount == 0)
                        continue;

                    var oc = av.GetOverridesCache();
                    if (children ? oc.RemoveChildrenPaths(path) : oc.Remove(path))
                    {
                        av.SaveUserDataWithUndo();
                        if (av.data.HasParent)
                            av.RevertAssetFromParentAsset();

                        OverridesMaybeChanged();
                    }
                }

                if (Helper.LogUnnecessary)
                    Debug.Log((children ? "Removed children overrides of: " : "Removed override at: ") + path + Helper.LOG_END);

                avTargets.TryImmediatelyPropagate();
            }
        }

        public static void TryRevert(AVTargets avTargets, string path)
        {
            if (avTargets.AVs != null)
            {
                for (int i = 0; i < avTargets.AVs.Length; i++)
                {
                    var av = avTargets.AVs[i];
                    if (av.UserCount == 0)
                        continue;

                    var oc = av.GetOverridesCache();
                    oc.Remove(path);
                    oc.RemoveChildrenPaths(path);
                    av.SaveUserDataWithUndo();
                    if (av.data.HasParent)
                        av.RevertAssetFromParentAsset();

                    OverridesMaybeChanged();
                }

                if (Helper.LogUnnecessary)
                    Debug.Log("Reverted: " + path + Helper.LOG_END);

                avTargets.TryImmediatelyPropagate();
            }
        }

        public static void CleanUpOverrides(AVTargets avTargets, string path)
        {
            if (avTargets.AVs != null)
            {
                if (Helper.LogUnnecessary)
                    Debug.Log("Cleaning up overrides in: " + path + Helper.LOG_END);

                for (int i = 0; i < avTargets.AVs.Length; i++)
                {
                    var av = avTargets.AVs[i];
                    if (av.UserCount == 0)
                        continue;

                    var oc = av.GetOverridesCache();
                    oc.Remove(path);
                    oc.RemoveChildrenPaths(path);
                    av.SaveUserDataWithUndo();

                    av.CreateOverridesFromDifferences(); //////TODO: specify path

                    OverridesMaybeChanged();
                }

                avTargets.TryImmediatelyPropagate();
            }
        }

        internal static void AddToContextMenu(AVTargets avTargets, string path, SerializedObject so, GenericMenu menu, bool odin, Action<IList<AV>, IList<AV>> applyToParent)
        {
            menu.AddItem(new GUIContent("Copy Override Path: " + path), false, () =>
            {
                GUIUtility.systemCopyBuffer = path;
            });

            if (avTargets.AnyMissingPath(path))
            {
                menu.AddItem(new GUIContent("Create Override"), false, () =>
                {
                    TryCreateOverride(avTargets, path, false);
                });
                if (avTargets.AnyContainsChildrenPaths(path, false))
                {
                    menu.AddItem(new GUIContent("Create Override Keep Children"), false, () =>
                    {
                        TryCreateOverride(avTargets, path, true);
                    });
                }
            }
            if (avTargets.AnyContainsChildrenPaths(path, false))
            {
                menu.AddItem(new GUIContent("Remove Children Overrides"), false, () =>
                {
                    TryRemoveOverride(avTargets, path, true);
                });
            }
            if (avTargets.AnyContainsPath(path))
            {
                menu.AddItem(new GUIContent("Remove Override"), false, () =>
                {
                    TryRemoveOverride(avTargets, path, false);
                });
            }

            if (!avTargets.AnyContainsChildrenPaths(path, true))
            {
                menu.AddSeparator("");
                return;
            }

            menu.AddItem(new GUIContent("Clean Up Overrides"), false, () =>
            {
                CleanUpOverrides(avTargets, path);
            });

            if (avTargets.AVs.Length == 1)
            {
                var av = avTargets.AVs[0];

                List<Object> ancestors = new List<Object>();
                var parent = av.LoadParentAsset();
                int root = -1;
                while (parent != null)
                {
                    using (var ancestorAV = AV.Open(parent))
                    {
                        if (!ancestorAV.Valid())
                            break;
                        if (!ancestorAV.data.HasParent)
                            root = ancestors.Count;
                        ancestors.Add(parent);
                        parent = ancestorAV.LoadParentAsset();
                    }
                }

                for (int i = 0; i < ancestors.Count; i++)
                {
                    int id = i;
                    var ancestor = ancestors[id];

                    string action = "Apply to " + (id == 0 ? "Parent" : (id == root ? "Root" : "Ancestor"));
                    menu.AddItem(new GUIContent(action + " '" + ancestor.name + "'"), false, () =>
                    {
                        if (av.UserCount == 0)
                        {
                            Debug.LogWarning("");
                            return;
                        }

                        using (var ancestorAV = AV.Open(ancestor))
                        {
                            if (!ancestorAV.Valid())
                                return;

                            for (int ii = 0; ii < id; ii++)
                            {
                                var closerAncestor = ancestors[ii];
                                using (var closerAncestorAV = AV.Open(closerAncestor))
                                {
                                    if (!closerAncestorAV.Valid())
                                        continue;

                                    var oc = closerAncestorAV.GetOverridesCache();
                                    if (oc.Remove(path) | oc.RemoveChildrenPaths(path))
                                        closerAncestorAV.SaveUserDataWithUndo();
                                }
                            }

                            applyToParent(new AV[] { av }, new AV[] { ancestorAV });
                        }
                    });
                }
            }
            else
            {
                menu.AddItem(new GUIContent("Apply to (individual) Parents"), false, () =>
                {
                    if (avTargets.AVs != null)
                    {
                        int l = avTargets.AVs.Length;
                        AV[] ancestorAVs = new AV[l];
                        for (int i = 0; i < l; i++)
                            ancestorAVs[i] = AV.Open(avTargets.AVs[i].LoadParentAsset());
                        applyToParent(avTargets.AVs, ancestorAVs);
                        for (int i = 0; i < l; i++)
                            ancestorAVs[i].Dispose();
                    }
                });
            }

            menu.AddItem(new GUIContent("Revert"), false, () =>
            {
                TryRevert(avTargets, path);
            });

            menu.AddSeparator("");
        }

        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            var so = property.serializedObject;
            var avTargets = AVTargets.Get(so);
            var path = avTargets.ConvertToOverridePath(property.propertyPath, so);
            OnPropertyContextMenu(menu, path, so);
        }
        internal static void OnPropertyContextMenu(GenericMenu menu, string path, SerializedObject so)
        {
            if (!AVFilter.Should(so.targetObject))
                return;
            //TODO:?
            //if (ERManager.Disabled)
            //return;

            var avTargets = AVTargets.Get(so);
            if (avTargets.AVs == null || !avTargets.AnyHasParent)
                return;

            AddToContextMenu(avTargets, path, so, menu, false, (avs, ancestorAVs) =>
            {
                ApplyToAncestor
                (
                    avs, ancestorAVs, path,
                    (SerializedProperty dstProp) => dstProp.editable,
                    (Object asset, Object ancestorAsset, out SerializedObject src, out SerializedObject dst, out SerializedProperty srcProp, out SerializedProperty dstProp, out bool alreadyCopied) =>
                    {
                        src = new SerializedObject(asset); // Doesn't user propertyCopy because that is for all AVs
                        dst = new SerializedObject(ancestorAsset);

                        alreadyCopied = false;

                        var ancestorPF = PropertyFilter.Get(ancestorAsset.GetType());

                        var propertyPath = avTargets.ConvertToPropertyPath(path, src, false);
                        if (propertyPath != null)
                        {
                            alreadyCopied = false;

                            srcProp = src.FindProperty(propertyPath);
                            if (srcProp == null)
                            {
                                Debug.LogWarning("?");
                                dstProp = null;
                                return;
                            }

                            var ancestorPathConversion = new PathConversionCache();

                            var ancestorPropertyPath = ancestorPathConversion.ConvertToPropertyPath(path, dst, ancestorPF); //var propertyPath = av.GetOverridesCache().ConvertToPropertyPath(path, src, avTargets.propertyFilter);
                            if (ancestorPropertyPath != null)
                                dstProp = dst.FindProperty(ancestorPropertyPath);
                            else
                                dstProp = null;

                            if (dstProp == null && (path[path.Length - 1] == '}')) // An emulation property
                            {
                                var parentPath = Helper.ParentOverridePath(path);
                                var ancestorParentPropertyPath = ancestorPathConversion.ConvertToPropertyPath(parentPath, dst, ancestorPF);
                                var parentDstProp = dst.FindProperty(ancestorParentPropertyPath);
                                if (parentDstProp != null)
                                {
                                    if (parentDstProp.isArray)
                                    {
                                        int index = parentDstProp.arraySize;
                                        parentDstProp.InsertArrayElementAtIndex(index);
                                        dstProp = parentDstProp.GetArrayElementAtIndex(index);
                                        if (Helper.LogUnnecessary)
                                            Debug.Log("Inserted new array element at index: " + index +
                                                " in the array: " + ancestorParentPropertyPath +
                                                " inside ancestor asset: " + ancestorAsset + Helper.LOG_END);
                                    }
                                    else
                                        Debug.LogWarning("(parentDstProp.isArray==false) - " + parentPath + Helper.LOG_END);
                                }
                                else
                                    Debug.LogWarning("Couldn't find parentDstProp: " + ancestorParentPropertyPath + " - From: " + parentPath + " - From: " + path);
                            }
                        }
                        else if (path.CustomEndsWith("\"}")) // Seems to be a string HashSet
                        {
                            srcProp = dstProp = null;

                            var parentPath = Helper.ParentOverridePath(path);

                            var ancestorEmulation = ancestorPF.GetEmulationProperty(parentPath) as StringHashSetEmulationProperty;
                            if (ancestorEmulation != null)
                            {
                                var ancestorParentPropertyPath = new PathConversionCache().ConvertToPropertyPath(parentPath, dst, ancestorPF);
                                if (ancestorParentPropertyPath != null)
                                {
                                    var parentDstProp = dst.FindProperty(ancestorParentPropertyPath);
                                    if (parentDstProp.propertyType == SerializedPropertyType.String)
                                    {
                                        int off = parentPath.Length + 3; // Removing the .{" as well
                                        string key = path.Substring(off, path.Length - off - 2); // Removing the "} as well

                                        if (Helper.LogUnnecessary)
                                            Debug.Log("The key found is: \"" + key + "\"" + Helper.LOG_END);

                                        parentDstProp.stringValue += ancestorEmulation.delimiters[0] + key;
                                        alreadyCopied = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("? 2");
                            srcProp = dstProp = null;
                        }
                    },
                    (SerializedProperty srcProp, SerializedProperty dstProp) =>
                    {
                        Helper.CopyValueRecursively(dstProp, srcProp);
                    },
                    (Object parentAsset, SerializedObject src, SerializedObject dst) =>
                    {
                        if (dst.ApplyModifiedProperties())
                            EditorUtility.SetDirty(parentAsset); //?
                        src.Dispose();
                        dst.Dispose();
                    }
                );
            });
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }


        // Indicators
        public static Color GetColor(int overriddenType, bool thinBar, bool forceVisible)
        {
            var sss = S.S.overrideIndicatorStyle;
            if (overriddenType == -1)
                return sss.mixedColor;
            else if (overriddenType == 0)
            {
                if (!forceVisible && thinBar && sss.thinBarsNotOverriddenInvisible)
                    return Color.clear;
                else
                    return sss.notOverriddenColor;
            }
            else if (overriddenType == 1)
                return sss.overriddenColor;
            else //if (overriddenType == 2)
                return sss.childrenOverriddenColor;
        }

        public static GUIStyle invisibleStyle = null;
        public static bool DrawIndicator(AVTargets avTargets, bool openBold, ref Rect rect2, bool drawnAfter, out Color color, string path, bool thinBar, bool forceVisible, SerializedObject pathSO, bool useMargins = true)
        {
            if (pathSO != null)
                path = avTargets.ConvertToOverridePath(path, pathSO);

            bool prevBold = default(bool);

            var sss = S.S.overrideIndicatorStyle;

            var rect = rect2;
            if (useMargins)
            {
                var margins = thinBar ? sss.thinBarMargins : sss.margins;
                rect.y += margins.top;
                rect.height -= margins.top + margins.bottom;
            }
            if (drawnAfter)
                rect2 = rect;

            int overriddenType = avTargets.GetOverriddenType(path);

            if (openBold)
            {
                prevBold = (bool)getBoldDefaultFontMI.Invoke(null, null);
                R.arg[0] = overriddenType != 0;
                setBoldDefaultFontMI.Invoke(null, R.arg);
            }

            color = GetColor(overriddenType, thinBar, forceVisible);

            if (!thinBar)
            {
                if (ReferenceEquals(invisibleStyle, null))
                    invisibleStyle = new GUIStyle();

                bool guiChanged = GUI.changed;
                if (GUI.Button(rect, GUIContent.none, invisibleStyle))
                {
                    GUI.changed = guiChanged; // Might as well do this

                    avTargets.ToggleOverriddenState(path, overriddenType == 1);

                    //TODO:::
                    //#if UNITY_2019_1_OR_NEWER
                    //                    OverrideButton.AllUpdateStyle();
                    //#endif
                }
            }

            //TODO::
            if (!drawnAfter && color.a != 0)
                EditorGUI.DrawRect(rect, color);

            return prevBold;
        }
        public static void CloseBold(bool prevBold)
        {
            R.arg[0] = prevBold;
            setBoldDefaultFontMI.Invoke(null, R.arg);
        }

        private static readonly MethodInfo setBoldDefaultFontMI = typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont", R.nps);
        private static readonly MethodInfo getBoldDefaultFontMI = typeof(EditorGUIUtility).GetMethod("GetBoldDefaultFont", R.nps);


        public override string SystemKey => "OverrideIndicator";

#if UNITY_2019_1_OR_NEWER && false
        private static readonly Queue<VisualElement> visualElementQueue = new Queue<VisualElement>();
        private static OverrideContainer FindChildOC(VisualElement parent)
        {
            visualElementQueue.Clear();
            for (int i = 0; i < parent.childCount; i++)
                visualElementQueue.Enqueue(parent[i]);

            while (visualElementQueue.Count > 0)
            {
                var ve = visualElementQueue.Dequeue();
                if (ve is OverrideContainer)
                {
                    visualElementQueue.Clear();
                    return (OverrideContainer)ve;
                }
                for (int i = 0; i < ve.childCount; i++)
                    visualElementQueue.Enqueue(ve[i]);
            }

            visualElementQueue.Clear();
            return null;
        }
#endif


        // Drawing
        public override void Draw(Rect position, SerializedProperty property)
        {
#if ODIN_INSPECTOR
            var depth = property.depth;
            if (OdinOverrideDrawer.findIgnoreDepth)
            {
                OdinOverrideDrawer.findIgnoreDepth = false;
                OdinOverrideDrawer.ignoreDepth = depth;
            }
            if (depth == OdinOverrideDrawer.ignoreDepth)
                return;
#endif

            //EditorGUI.DrawRect(position, Color.red);
            //Debug.Log(property.hasVisibleChildren + " " + property.propertyPath);

            //if (CurrentlyIMGUIParentWhole)
            //{
            //    ChildrenOnGUI(position, property, label);
            //    return;
            //}

            AVTargets avTargets;
            if (ShouldUseFor(property, out avTargets))
            {
                if (avTargets != null)
                {
                    //EditorGUI.DrawRect(position, Color.green);

                    var sss = S.S.overrideIndicatorStyle;

                    bool forRawView = RawViewWindow.CurrentlyDrawing;
                    bool thinBar = !forRawView && property.propertyType != SerializedPropertyType.ArraySize;
                    var margins = thinBar ? sss.thinBarMargins : sss.margins;
                    float buttonWidth = thinBar ? sss.thinBarWidth : (forRawView ? sss.rawViewWidth : sss.width);

                    Rect buttonRect = EditorGUI.IndentedRect(position);
                    buttonRect.width = buttonWidth;
                    buttonRect.x -= buttonWidth + margins.right;
                    buttonRect.height += EditorGUIUtility.standardVerticalSpacing;

                    bool forceVisible = false; //TODO: def.forceRootThinBarsVisible && property.depth == 0;
                    DrawIndicator(avTargets, S.S.boldOverriddenIMGUI, ref buttonRect, false, out _, property.propertyPath, thinBar, forceVisible, property.serializedObject);
                }
                else
                {
                    Debug.LogError("?");
                }
            }
        }

#if UNITY_2019_1_OR_NEWER
        public override bool ShouldModify(SerializedObject serializedObject)
        {
            //TODO: also check the avs/avtargets anyhasparent?
            return AVFilter.Should(serializedObject.targetObject);
        }
        public override void Modify(PropertyField propertyField, SerializedProperty prop)
        {
            AVTargets avTargets;
            should = ShouldUseFor(prop, out avTargets);
            if (should)
            {
                if (avTargets != null)
                {
                    //propertyField.style.backgroundColor = Color.blue * 0.5f;
                    var ob = CreateButton(propertyField, prop, true, null);
                    propertyField.Add(ob);
                }
            }
        }
        private static bool should; //TODO: THEY are done sequentially right?

        private OverrideButton CreateButton(VisualElement element, SerializedProperty property, bool allowThinBars, Action<OverrideButton> onGeometryChanged)
        {
            AVTargets avTargets;
            if (!ShouldUseFor(property, out avTargets))
                return null;
            string path = avTargets.ConvertToOverridePath(property.propertyPath, property.serializedObject);
            var button = new OverrideButton(element, avTargets, path, allowThinBars, onGeometryChanged);
            button.UpdateStyle();
            return button;
        }

        public override bool NeedToApplyStyling => true;
        public delegate bool ShouldSkipSubFields(VisualElement textStyleElement, VisualElement element, VisualElement rootElement, SerializedProperty prop, bool outer);
        public List<ShouldSkipSubFields> shouldSkipSubFields = new List<ShouldSkipSubFields>();
        public override void ApplyStyling(VisualElement textStyleElement, VisualElement element, VisualElement rootElement, SerializedProperty prop, bool outer)
        {
            if (!should)
                return;

            OverrideButton ob;

            if (outer)
            {
                if (!S.S.boldOverriddenUIToolkit)
                    return;

                ob = OverrideButton.FindChild((PropertyField)rootElement.parent); //??
            }
            else //if (!WrapperPropertyField.CurrentlyUITParentWhole)
            {
                for (int i = 0; i < shouldSkipSubFields.Count; i++)
                {
                    var sssf = shouldSkipSubFields[i];
                    if (sssf(textStyleElement, element, rootElement, prop, outer))
                        return;
                }

                ob = CreateButton(element, prop, S.S.overrideIndicatorStyle.useThinBarsForUITCompositeChildren, (_ob) =>
                {
                    var rewb = rootElement.worldBound;
                    var ewb = element.worldBound;
                    _ob.style.left = ewb.xMin - rewb.xMin - _ob.fullWidth;
                    _ob.style.top = ewb.yMin - rewb.yMin;
                });
                rootElement.Add(ob);

                // Delayed to avoid "Assertion failed" error
                rootElement.schedule.Execute(() =>
                {
                    rootElement.style.overflow = Overflow.Visible; //TODO2: does this ever create a problem?
                });

                if (!S.S.boldOverriddenUIToolkit)
                    return;
            }

            if (ob != null)
            {
                ob.textStyleElement = textStyleElement;
                ob.UpdateStyle();
            }
        }
#endif
    }

#if UNITY_2019_1_OR_NEWER
    internal class OverrideButton : Button
    {
        public OverrideButton parentButton;

        public VisualElement textStyleElement;

        public readonly AVTargets avTargets;
        public readonly string propertyPath;
        public readonly bool thinBar;
        public readonly float fullWidth;

        private static StyleSheet boldStyleSheet = null;

        public static bool dirtyStyles = false;
        public static void AllUpdateStyle()
        {
            for (int i = 0; i < all.Count; i++)
                all[i].UpdateStyle();
        }

        public void UpdateStyle()
        {
            //return;

            int overriddenType = avTargets.GetOverriddenType(propertyPath);
            Color c = OverrideIndicator.GetColor(overriddenType, thinBar, false);
            style.backgroundColor = c;
            style.visibility = c.a != 0 ? Visibility.Visible : Visibility.Hidden; //Is this done internally anyway?

            if (textStyleElement != null)
            {
                if (overriddenType != 0)
                {
                    if (boldStyleSheet == null)
                        boldStyleSheet = Resources.Load<StyleSheet>("OverrideDrawerBold");
                    if (boldStyleSheet != null)
                    {
                        if (!textStyleElement.styleSheets.Contains(boldStyleSheet))
                            textStyleElement.styleSheets.Add(boldStyleSheet);
                    }
                    else
                        Debug.LogWarning("");
                }

                const string boldClassName = "override-drawer--bold"; //BindingExtensions.prefabOverrideUssClassName;
                textStyleElement.EnableInClassList(boldClassName, overriddenType != 0);
            }
        }
        public static void UpdateChildStyles(VisualElement ve)
        {
            for (int i = 0; i < ve.childCount; i++)
            {
                var child = ve[i];
                if (child.GetType() == typeof(OverrideButton))
                    ((OverrideButton)child).UpdateStyle();
                else
                    UpdateChildStyles(child);
            }
        }
        public void UpdateAllStyles()
        {
            UpdateStyle();

            if (parentButton != null)
                parentButton.UpdateAllStyles();
        }

        public static OverrideButton FindChild(PropertyField pf)
        {
            for (int i = 0; i < pf.childCount; i++)
                if (pf[i] is OverrideButton)
                    return (OverrideButton)pf[i];
            return null;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            all.Add(this);

            parentButton = null;
            var parentPF = parent.GetFirstAncestorOfType<PropertyField>();
            if (parentPF != null)
                parentButton = FindChild(parentPF);
        }
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            all.Remove(this);
        }
        private static readonly List<OverrideButton> all = new List<OverrideButton>();

        [InitializeOnLoadMethod]
        private static void Init()
        {
            Undo.undoRedoPerformed += () =>
            {
                AllUpdateStyle();
            };

            EditorApplication.update += () =>
            {
                if (dirtyStyles)
                {
                    dirtyStyles = false;
                    AllUpdateStyle();
                }
            };
        }

        // TODO:: is the problem with Unity caused by my screen scaling?
        // TODO: find if this is ever not needed in a version
        public static float alignmentFix = -0.333333f; //TODO: is this even the right value?

        private void UpdateHeight(VisualElement element)
        {
            //var verticalPadding = element.style.paddingTop.value.value + element.style.paddingBottom.value.value;
            style.height = element.worldBound.height; // + verticalPadding; //TODO:: why should it add padding? shouldn't it subtract it or something?
        }
        public OverrideButton(VisualElement element, AVTargets avTargets, string propertyPath, bool allowThinBars, Action<OverrideButton> onGeometryChanged)
        {
            this.avTargets = avTargets;
            this.propertyPath = propertyPath;

            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;
            style.borderBottomWidth = 0;
            style.borderTopWidth = 0;

            style.borderTopLeftRadius = 0;
            style.borderTopRightRadius = 0;
            style.borderBottomLeftRadius = 0;
            style.borderBottomRightRadius = 0;

            style.paddingLeft = 0;
            style.paddingRight = 0;
            style.paddingTop = 0;
            style.paddingBottom = 0;

            var sss = S.S.overrideIndicatorStyle;

            styleSheets.Clear();

            thinBar = allowThinBars && sss.useThinBarsUIToolkit;

            style.position = Position.Absolute;

            var margins = thinBar ? sss.thinBarMargins : sss.margins;

            style.marginLeft = margins.left;
            style.marginRight = margins.right;
            style.marginTop = margins.top;
            style.marginBottom = margins.bottom;

            float w;
            if (thinBar)
                w = sss.thinBarWidth;
            else
                w = sss.width;
            style.width = w;

            fullWidth = w + margins.left + margins.right;

            //style.backgroundColor = Color.white;

            float verticalMargin = margins.top + margins.bottom;

            //style.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing - verticalMargins;

            style.left = fullWidth + alignmentFix + PropertyModifier.alignmentL;
            style.top = 0;

            name = "For element: " + element.GetType().Name + " - " + element.name;

            element.style.marginLeft = element.resolvedStyle.marginLeft + fullWidth;
            if (onGeometryChanged != null)
                onGeometryChanged(this);
            UpdateHeight(element);
            element.RegisterCallback<GeometryChangedEvent>((e) =>
            {
                if (onGeometryChanged != null)
                    onGeometryChanged(this);
                UpdateHeight(element);
            });

            if (thinBar)
            {
                clickable.activators.Clear();
            }
            else
            {
                clickable.activators.Add(S.allowSubOverridesMAF);
#if UNITY_2019_3_OR_NEWER
                clicked += () =>
#else
                // Although is clickable.clicked even comparable whatsoever to this.clicked?
                clickable.clicked += () =>
#endif
                {
                    int overriddenType = avTargets.GetOverriddenType(propertyPath);
                    avTargets.ToggleOverriddenState(propertyPath, overriddenType == 1);

                    //TODO:?
                    if (overriddenType != 1 && !S.currentlyAllowSubOverrides())
                        UpdateChildStyles(parent);
                    else
                        UpdateStyle();
                    if (parentButton != null)
                        parentButton.UpdateAllStyles();
                };
            }

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
    }
#endif

#if ODIN_INSPECTOR
    [DrawerPriority(value: float.MaxValue)]
    public sealed class OdinOverrideDrawer : OdinDrawer, IDefinesGenericMenuItems //OdinValueDrawer<object>,
    {
        /// <summary>
        /// This is a list of paths that only exist in Odin, but say they are SerializationBackend.Unity
        /// even though they are not serialized by Unity/the path is not representative of the InspectorProperty.
        /// Thankfully they are absolute paths, never local to a class field.
        /// </summary>
        public static readonly List<string> odinFakePaths = new List<string>()
        {
            "toggleableAssets",
            "$ROOT" // Array.data[]ROOT in UnityPropertyPath
        };

        internal static GUIStyle marginsStyle = null;

        public void PopulateGenericMenu(InspectorProperty property, GenericMenu menu)
        {
            var o = property.Tree.WeakTargets[0] as Object;
            if (o == null || !AVFilter.Should(o))
                return;

            var so = Property.SerializationRoot.Tree.UnitySerializedObject;
            if (so == null)
                return;

            var avTargets = AVTargets.Get(so);
            if (avTargets.AVs == null || !avTargets.AnyHasParent)
                return;

            var path = avTargets.GetOverridePath(Property, so);

            OverrideIndicator.AddToContextMenu(avTargets, path, so, menu, true, (avs, ancestorAVs) =>
            {
                OverrideIndicator.ApplyToAncestor
                (
                    avs, ancestorAVs, path,
                    (InspectorProperty dstProp) => dstProp.Info.IsEditable,
                    (Object asset, Object parentAsset, out PropertyTree src, out PropertyTree dst, out InspectorProperty srcProp, out InspectorProperty dstProp, out bool alreadyCopied) =>
                    {
                        alreadyCopied = false;
                        src = PropertyTree.Create(asset);
                        dst = PropertyTree.Create(parentAsset);
                        srcProp = src.GetPropertyAtUnityPath(path);
                        dstProp = dst.GetPropertyAtUnityPath(path);
                    },
                    (InspectorProperty srcProp, InspectorProperty dstProp) =>
                    {
                        dstProp.ValueEntry.WeakValues[0] = Helper.DeepCopy(srcProp.GetWeakValue());
                    },
                    (Object parentAsset, PropertyTree src, PropertyTree dst) =>
                    {
                        Undo.RecordObject(parentAsset, "Apply To Ancestor");
                        dst.ApplyChanges();
                        src.Dispose();
                        dst.Dispose();
                    }
                );
            });
        }

        internal static bool disabled = false;

        private static PropertyTree currentTree;
        private static bool canDrawTree;
        public override bool CanDrawProperty(InspectorProperty property) //protected override bool CanDrawValueProperty(InspectorProperty property)
        {
            if (!Valid(property))
                return false;
            if (property.Tree != currentTree)
            {
                currentTree = property.Tree;
                var o = property.Tree.WeakTargets[0] as Object;
                canDrawTree = o != null && AVFilter.Should(o);
            }
            return canDrawTree;
        }

        internal static bool findIgnoreDepth = false;
        internal static int ignoreDepth = -1;

        private static bool Valid(InspectorProperty property)
        {
            return property.Info.IsEditable &&
                property.Info.PropertyType == PropertyType.Value &&
                !odinFakePaths.Contains(property.Path);
        }

        private static List<string> relativePaths = new List<string>(), fullPaths = new List<string>();

        //private static void RemoveChildrenProperties(OverrideTree.OverrideNode node, InspectorProperty property, AVTargets avTargets, SerializedObject so, bool odinSerialized)
        //{
        //    var children = property.Children;
        //    for (int i = 0; i < children.Count; i++)
        //    {
        //        var child = children[i];
        //        var pt = child.Info.PropertyType;
        //        if (pt == PropertyType.Group)
        //        {
        //            RemoveChildrenProperties(node, child, avTargets, so, odinSerialized);
        //        }
        //        else if (pt == PropertyType.Value)
        //        {
        //            var childPath = GetOverridePath(child.UnityPropertyPath, avTargets, so, odinSerialized, false);
        //            if (childPath != null)
        //            {
        //                for (int ii = 0; ii < node.children.Count; ii++)
        //                {
        //                    var childNode = node.children[ii];
        //                    if (!childNode.hasDrawn && childNode.fullPath == childPath)
        //                        childNode.hasDrawn = true;
        //                }
        //            }
        //        }
        //    }
        //}

        protected override void DrawPropertyLayout(GUIContent label)
        {
            bool prevGUIEnabled = GUI.enabled;

            //Debug.Log(Property.ChildResolver.GetType());

            // (BTW Raw View is never handled with OdinWrapperDrawer)

            if (!Valid(Property))
            {
                CallNextDrawer(label);
                return;
            }

            //Debug.Log(Property.PrefabModificationPath);

            var so = Property.SerializationRoot.Tree.UnitySerializedObject;
            if (so == null)
            {
                CallNextDrawer(label);
                return;
            }

            var avTargets = AVTargets.Get(so);

            var sb = Property.Info.SerializationBackend;

            bool odinSerialized = sb == SerializationBackend.Odin;

            string path = avTargets.GetOverridePath(Property, so, odinSerialized);

            //Debug.Log(path);

            if (avTargets.propertyFilter.ShouldIgnore(Property.Name, path))
            {
                CallNextDrawer(label);
                return;
            }

            if (disabled)
            {
                CallNextDrawer(label);
                return;
            }

#if !ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (odinSerialized)
            {
                CallNextDrawer(label);
                return;
            }
#endif

            if (sb == SerializationBackend.None)
            {
                //Debug.Log("what? " + path);

                //if (Property.ParentType == typeof(RectInt))
                //    Debug.Log("yep... odin");
                CallNextDrawer(label);
                return;
            }

            // Skip temporary "add dictionary entry" properties (which oddly are "Unity" SerializationBackend)
            if (Property.ParentType.InheritsFrom(typeof(TempKeyValuePair<,>)))
            {
                CallNextDrawer(label);
                return;
            }

            if (avTargets.AVs == null || !avTargets.AnyHasParent)
            {
                CallNextDrawer(label);
                return;
            }

            //if (Property.Info.TypeOfValue.InheritsFrom(typeof(System.Array)))
            //    Debug.Log(Property.UnityPropertyPath + " " + Property.Children.Count);

            // Dictionary keys should have no override button
            if (Property.Index == 0 && Property.ParentType.InheritsFrom(typeof(EditableKeyValuePair<,>))) //|| Property.ParentType.InheritsFrom(typeof(HashSet<>))
            {
                bool prevDisabled = disabled;
                disabled = true;
                CallNextDrawer(label); // Draw key without an override button
                disabled = prevDisabled;
                return;
            }

            //Debug.Log(path + " " + Property.Path);

            S s = S.S;
            var sss = s.overrideIndicatorStyle;

            bool thinBar = sss.useThinBarsOdin;

            float width = thinBar ? sss.thinBarWidth : sss.width;
            var margins = thinBar ? sss.thinBarMargins : sss.margins;
            if (marginsStyle == null)
            {
                marginsStyle = new GUIStyle();
                marginsStyle.margin = new RectOffset(Mathf.CeilToInt(margins.left + width + margins.right), 0, 0, 0);
            }
            Rect rect = EditorGUILayout.BeginVertical(marginsStyle);
            rect = EditorGUI.IndentedRect(rect);

            // Draws override button
            Rect buttonRect = rect;
            buttonRect.x += margins.left;
            buttonRect.width = width;
            Color buttonColor;
            //buttonRect.height += EditorGUIUtility.standardVerticalSpacing; //?
            //buttonRect.x += margins.left;
            //buttonRect.width -= margins.left + margins.right;
            bool openBold = S.S.boldOverriddenIMGUI;
            GUI.enabled = true;
            bool prevBold = OverrideIndicator.DrawIndicator(avTargets, true, ref buttonRect, true, out buttonColor, path, thinBar, false, null, false); //? //TODO: instead of "thinBar, false" use the ??
            GUI.enabled = prevGUIEnabled;

            // Size override button
            string sizePath = null;
            //bool isDictionary = false;
            if (Property.IsCollectionWithSize())
                sizePath = path + Helper.ARRAY_SIZE_SUFFIX;
            else if (Helper.IsDictionary(Property))
            {
                //isDictionary = true;
                sizePath = path + ".DictionarySize";
            }

            Color sizeButtonColor = default(Color);
            Rect sizeButtonRect = default(Rect);
            if (sizePath != null)
            {
                sizeButtonRect = rect;
                sizeButtonRect.x = sizeButtonRect.xMax - sss.collectionSizeWidth + sss.odinCollectionSizeXShift;
                sizeButtonRect.width = sss.collectionSizeWidth;
                sizeButtonRect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                GUI.enabled = true;
                OverrideIndicator.DrawIndicator(avTargets, false, ref sizeButtonRect, true, out sizeButtonColor, sizePath, false, true, null, false);
                GUI.enabled = prevGUIEnabled;
            }

            // Draw the actual property
            findIgnoreDepth = true;
            try
            {
                CallNextDrawer(label);
            }
            finally
            {
                findIgnoreDepth = false;
                ignoreDepth = -1;
            }

            if (openBold)
                OverrideIndicator.CloseBold(prevBold);

            GUILayout.EndVertical();

            if (buttonColor.a != 0)
                EditorGUI.DrawRect(buttonRect, buttonColor);
            if (sizeButtonColor.a != 0)
                EditorGUI.DrawRect(sizeButtonRect, sizeButtonColor);

            var node = avTargets.odinOverrideTree.Get(path, avTargets.AVs);
            if (node != null)
            {
                node.hasDrawn = true;

                if (Property.Expanded())
                {
                    bool drawUndrawn = false;
                    if (s.odinUndrawnOverridesMode == S.OdinUndrawnOverridesMode.EveryProperty)
                        drawUndrawn = true;
                    else if (s.odinUndrawnOverridesMode == S.OdinUndrawnOverridesMode.DictionariesAndHashSets)
                    {
#if ODIN_INSPECTOR_3_1
                        drawUndrawn = Property.ChildResolver.IsCollection && (Property.IsDictionary() || Property.IsHashSet()); // The IsCollection is just for optimization as I'm guessing it's cached
#else
                        drawUndrawn = Property.IsDictionary() || Property.IsHashSet();
#endif
                    }
                    if (drawUndrawn)
                    {
                        //if (!node.hasInitialized)
                        //{
                        //    node.hasInitialized = true;

                        //    if (S.S.odinUndrawnUseRemoveChildrenInitialization)
                        //        RemoveChildrenProperties(node, Property, avTargets, so, odinSerialized);
                        //}

                        fullPaths.Clear();
                        relativePaths.Clear();
                        for (int i = 0; i < node.children.Count; i++)
                        {
                            var ch = node.children[i];
                            if (!ch.hasDrawn && ch.overriden && ch.fullPath != sizePath)
                            {
                                fullPaths.Add(ch.fullPath);
                                relativePaths.Add(ch.relativePath);
                            }
                        }
                        if (fullPaths.Count > 0)
                        {
                            GUI.enabled = true;
                            EditorGUI.indentLevel++;
                            float height = RawViewDrawer.GetUndrawnHeight(relativePaths, 2);
                            rect = EditorGUILayout.GetControlRect(true, height);
                            RawViewDrawer.DrawUndrawn(avTargets, rect, so, fullPaths, fullPaths, 2);
                            EditorGUI.indentLevel--;
                            GUI.enabled = prevGUIEnabled;
                        }
                    }
                }
                else if (Property.PreviousExpanded())
                {
                    node.ResetDrawn();
                    node.hasDrawn = true;
                }
            }
        }
    }
#endif
}

#endif
