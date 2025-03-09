#if UNITY_EDITOR //!UNITY_2022_1_OR_NEWER // 2022.1+ Already have material variants
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EditorReplacement;
using Object = UnityEngine.Object;
using S = AssetVariants.AVSettings;
using MOD = AssetVariants.MaterialOverrideDrawer;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace AssetVariants
{
    //TODO: move this?
    internal sealed class MaterialOverrideDrawer : MaterialPropertyDrawer
    {
        public static bool forRawView = false;

        public static AVTargets avTargets = null; // MaterialPropertyDrawer doesn't have UI Toolkit support as far as I can see. Having a static state like this is fine.

        public MaterialPropertyDrawer original = null;

        private float GetBasePropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            float h;
            if (original != null)
                h = original.GetPropertyHeight(prop, label, editor);
            else
                h = MaterialEditor.GetDefaultPropertyHeight(prop); //return base.GetPropertyHeight(prop, label, editor);
            return h;
        }
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            float h = GetBasePropertyHeight(prop, label, editor);

            if (forRawView)
            {
                string path = AVMaterialEditor.PATH_PREFIX + prop.name;

                switch (prop.type)
                {
                    case MaterialProperty.PropType.Color:
                        avTargets.rvUnclaimedPaths.Remove(path + ".r");
                        avTargets.rvUnclaimedPaths.Remove(path + ".g");
                        avTargets.rvUnclaimedPaths.Remove(path + ".b");
                        avTargets.rvUnclaimedPaths.Remove(path + ".a");
                        break;
                    case MaterialProperty.PropType.Vector:
                        avTargets.rvUnclaimedPaths.Remove(path + ".x");
                        avTargets.rvUnclaimedPaths.Remove(path + ".y");
                        avTargets.rvUnclaimedPaths.Remove(path + ".z");
                        avTargets.rvUnclaimedPaths.Remove(path + ".w");
                        break;
                    case MaterialProperty.PropType.Texture:
                        if (original == null)
                        {
                            avTargets.rvUnclaimedPaths.Remove(path + ".texture");
                            avTargets.rvUnclaimedPaths.Remove(path + ".tiling");
                            avTargets.rvUnclaimedPaths.Remove(path + ".tiling.x");
                            avTargets.rvUnclaimedPaths.Remove(path + ".tiling.y");
                            avTargets.rvUnclaimedPaths.Remove(path + ".offset");
                            avTargets.rvUnclaimedPaths.Remove(path + ".offset.x");
                            avTargets.rvUnclaimedPaths.Remove(path + ".offset.y");
                        }
                        break;
                }

                var undrawn = avTargets.RVPrepare(path, false);
                EditorGUI.indentLevel++;
                avTargets.RVClaim(path, avTargets.rvUnclaimedPaths, undrawn);
                h += RawViewDrawer.GetUndrawnHeight(undrawn, 0);
                EditorGUI.indentLevel--;
            }

            return h;
        }

        private void AddChannelSpacing(ref Rect rect, int i, bool labelExists)
        {
            var sss = S.S.overrideIndicatorStyle;

            if (i == 0)
            {
                if (labelExists)
                {
                    rect.x += rect.width;
                    rect.x += sss.materialChannelsFirstSpacing;
                }
                else
                    rect.x -= rect.width * 4 + sss.materialChannelsFirstSpacing + sss.materialChannelsSpacing * 3;
            }
            else
            {
                rect.x += rect.width;
                rect.x += sss.materialChannelsSpacing;
            }
        }

        private void DrawTextureScaleOffset(Rect rect, Rect xRect, Rect yRect, string path, string scaleOffset)
        {
            const SerializedObject so = null;

            Color _;

            string path2 = path + scaleOffset;
            OverrideIndicator.DrawIndicator(avTargets, false, ref rect, false, out _, path2, false, true, so);

            string path3;

            path3 = path2 + ".x";
            OverrideIndicator.DrawIndicator(avTargets, false, ref xRect, false, out _, path3, false, true, so);

            path3 = path2 + ".y";
            OverrideIndicator.DrawIndicator(avTargets, false, ref yRect, false, out _, path3, false, true, so);
        }

        private void Draw(Rect position, MaterialProperty prop, string label1, GUIContent label2, MaterialEditor editor)
        {
            if (forRawView && !RawDrawer.currentlyDrawNiceNames)
                label1 = prop.name;

            if (original != null)
            {
                if (label2 == null)
                    original.OnGUI(position, prop, label1, editor);
                else
                    original.OnGUI(position, prop, label2, editor);
            }
            else
            {
                if (label2 == null)
                    editor.DefaultShaderProperty(position, prop, label1);
                else
                    editor.DefaultShaderProperty(position, prop, label2.text);
            }
        }
        private void OnGUI(Rect position, MaterialProperty prop, string label1, GUIContent label2, MaterialEditor editor)
        {
            RawViewDrawer.currentWidth = position.width;

            if (avTargets != null && avTargets.AVs != null && avTargets.AnyHasParent)
            {
                var sss = S.S.overrideIndicatorStyle;

                float buttonWidth = sss.width;
                float rootButtonWidth;
                if (forRawView)
                    rootButtonWidth = sss.rawViewWidth;
                else
                    rootButtonWidth = buttonWidth;

                if (forRawView)
                    position.height = GetBasePropertyHeight(prop, label1, editor);

                ////if (!useRootAlignment)
                ////{
                ////    float w = sss.margins.left + rootButtonWidth + sss.margins.right;
                ////    position.x += w;
                ////    position.width -= w;
                ////}

                // TODO2:
                bool labelExists = position.width > 200 || position.x < 100; // Arbitrary //label1 != null || (label2 != GUIContent.none && label2 != null); //bool labelExists = (label1 == null) ? (label2 != GUIContent.none) : !string.IsNullOrEmpty(label1);

                var rect = EditorGUI.IndentedRect(position);

                var buttonRect = rect;
                buttonRect.width = rootButtonWidth;
                buttonRect.x -= buttonRect.width + sss.margins.right;

                const SerializedObject so = null;

                string path = AVMaterialEditor.PATH_PREFIX + prop.name;
                bool openBold = S.S.boldOverriddenMaterial;
                bool prevBold = OverrideIndicator.DrawIndicator(avTargets, openBold, ref buttonRect, false, out _, path, false, true, so);

                if (forRawView)
                    avTargets.rvUnclaimedPaths.Remove(path);

                //GUI.DrawTexture(position, Texture2D.grayTexture);

                switch (prop.type)
                {
                    case MaterialProperty.PropType.Range:
                    case MaterialProperty.PropType.Float:
#if UNITY_2021_1_OR_NEWER
                    case MaterialProperty.PropType.Int:
#endif
                        break;

                    case MaterialProperty.PropType.Color:
                        var r = buttonRect;
                        r.width = buttonWidth;
                        AddChannelSpacing(ref r, 0, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".r", false, true, so);
                        AddChannelSpacing(ref r, 1, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".g", false, true, so);
                        AddChannelSpacing(ref r, 2, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".b", false, true, so);
                        AddChannelSpacing(ref r, 3, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".a", false, true, so);
                        break;
                    case MaterialProperty.PropType.Vector:
                        r = buttonRect;
                        r.width = buttonWidth;
                        AddChannelSpacing(ref r, 0, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".x", false, true, so);
                        AddChannelSpacing(ref r, 1, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".y", false, true, so);
                        AddChannelSpacing(ref r, 2, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".z", false, true, so);
                        AddChannelSpacing(ref r, 3, labelExists);
                        OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".w", false, true, so);
                        break;

                    case MaterialProperty.PropType.Texture:
                        if (original == null)
                        {
                            //const float kSpacingSubLabel = 4; // From EditorGUI

                            float fieldWidth = EditorGUIUtility.fieldWidth;

                            rect = position;
                            rect.height -= EditorGUIUtility.standardVerticalSpacing; // I don't know why this is needed

                            r = rect;
                            r.x = (rect.xMax - fieldWidth) - (buttonWidth + sss.margins.right);
                            r.width = buttonWidth;
                            OverrideIndicator.DrawIndicator(avTargets, false, ref r, false, out _, path + ".texture", false, true, so);

                            float kLineHeight = EditorGUIUtility.singleLineHeight;
                            float totalLineHeight = kLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            rect.width -= fieldWidth;
                            EditorGUI.indentLevel++;
                            rect = EditorGUI.IndentedRect(rect);
                            EditorGUI.indentLevel--;
                            const float labelWidth = 65;
                            float controlStartX = rect.x + labelWidth;
                            rect.y = rect.yMax - 2 * totalLineHeight;
                            rect.height = kLineHeight;
                            Rect xRect = rect;
                            xRect.width += (xRect.x - controlStartX);
                            xRect.x = controlStartX + sss.materialTilingOffsetXOffset;
                            Rect yRect = xRect;
                            //yRect.x += (xRect.width - kSpacingSubLabel) * 0.5f + kSpacingSubLabel;
                            yRect.x += xRect.width * 0.5f + 1;

                            rect.width = buttonWidth;
                            xRect.width = buttonWidth;
                            yRect.width = buttonWidth;

                            rect.x -= buttonWidth + sss.margins.right;

                            DrawTextureScaleOffset(rect, xRect, yRect, path, ".tiling");

                            rect.y += totalLineHeight;
                            xRect.y += totalLineHeight;
                            yRect.y += totalLineHeight;

                            DrawTextureScaleOffset(rect, xRect, yRect, path, ".offset");
                        }
                        break;
                }

                Draw(position, prop, label1, label2, editor);

                if (forRawView)
                {
                    EditorGUI.indentLevel++;
                    position.y += position.height;
                    List<string> undrawn;
                    if (avTargets.rvClaimedUndrawnPaths.TryGetValue(path, out undrawn))
                        RawViewDrawer.DrawUndrawn(avTargets, position, so, undrawn, undrawn, 0);
                    EditorGUI.indentLevel--;
                }

                if (openBold)
                    OverrideIndicator.CloseBold(prevBold);
            }
            else
            {
                Draw(position, prop, label1, label2, editor);
            }
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            OnGUI(position, prop, null, label, editor);
        }
        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            OnGUI(position, prop, label, null, editor);
        }
    }

#if false //EDITOR_REPLACEMENT
    public class AVMaterialEditorReplacer : EditorReplacer
    {
        protected override void DeepCopyTo(EditorReplacer deepCopy) { }
        public override string SystemKey => "AVMaterialEditorReplacer";
        public override Type GetEditorType(Type targetType, EditorTreeLocation location)
        {
            return typeof(AVMaterialEditor);
        }
        //TODO:?
        //public override bool RootNeedsWrapper => true;
        //public override ChildAction OnAssignedTargets(IReadOnlyList<Object> inspectedTargets, IReadOnlyList<Object> parentTargets, EditorTreeNode node)
        //{
        //    if (!AVFilter.Should(node.Targets[0]))
        //        return ChildAction.Keep;
        //    else
        //        return ChildAction.RemoveReparent;
        //}
        public override void Replace(EditorTreeDef treeDef)
        {
            //TODO: what about subassets editor?
            if (!typeof(Material).IsAssignableFrom(treeDef.inspectedType))
                return;
            if (!AVFilter.Should(treeDef.inspectedType))
                return;
            //treeDef.WrapDefaults(this);
            treeDef.Root.ForEachNode((n) =>
            {
                if (n.replacer == null)
                    n.replacer = this;
                return true;
            });
        }
        public override IEnumerable<Type> CreateInspectedTypes
        {
            get
            {
                yield return typeof(Material);
            }
        }
    }
#else
    [CustomEditor(typeof(Material))]
#endif
    public class AVMaterialEditor : MaterialEditor
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            PropertyFilter.InheritedIgnorePropertyPath(typeof(Material), "m_SavedProperties");

            AV.customRevertAssetFromParentAsset[typeof(Material)] = CustomRevertAssetFromParentAsset;
            AV.customCreateOverridesFromDifferences[typeof(Material)] = CustomCreateOverridesFromDifferences;

            AV.customCreateImplicitOverrides[typeof(Material)] = CustomImplicit;

            AVHeader.OnCreated += (avHeader) =>
            {
                Editor e;
                if (avHeader.editor.TryGetTarget(out e) && e is AVMaterialEditor)
                {
                    avHeader.parentFilterType = typeof(Material); //?

                    avHeader.setupRawViewWindow += (w) =>
                    {
                        w.additional = () =>
                        {
                            ((AVMaterialEditor)e).DrawAdditionalRawView();
                        };
                    };
                }
            };
        }

        private void DrawAdditionalRawView()
        {
            var header = AVHeader.Get(this);
            if (header != null)
            {
                MOD.forRawView = true;
                MOD.avTargets = header.avTargets;
                try
                {
                    GUILayout.Space(beforeSpacing);

                    RawViewDrawer.ResetCurrentWidth();

                    // TODO2: can these be cached?
                    var properties = GetMaterialProperties(targets);
                    foreach (MaterialProperty prop in properties)
                    {
                        string displayName = prop.displayName;
                        float height = emptyMOD.GetPropertyHeight(prop, displayName, this);
                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(height));
                        emptyMOD.OnGUI(rect, prop, displayName, this);
                    }

                    GUILayout.Space(afterSpacing);
                }
                finally
                {
                    MOD.avTargets = null;
                    MOD.forRawView = false;
                }
            }
        }

        //TODO: what were they originally?
        public static float beforeSpacing = 4, afterSpacing = 4; //TODO: add to settings

        internal const string PATH_PREFIX = "M."; // For fake paths. Do not change this.

        [NonSerialized]
        private Shader shader = null;

        private static class R
        {
            public static Type mphType = AssetVariants.R.assembly.GetType("UnityEditor.MaterialPropertyHandler");
            public static FieldInfo s_PropertyHandlersFI = mphType != null ? mphType.GetField("s_PropertyHandlers", AssetVariants.R.nps) : null;
            public static MethodInfo getHandlerMI = mphType != null ? mphType.GetMethod("GetHandler", AssetVariants.R.nps) : null;
            public static MethodInfo getPropertyStringMI = mphType != null ? mphType.GetMethod("GetPropertyString", AssetVariants.R.nps) : null;
            public static FieldInfo m_PropertyDrawerFI = mphType != null ? mphType.GetField("m_PropertyDrawer", AssetVariants.R.npi) : null;

            public static bool NullFMPHC()
            {
                return mphType == null || getHandlerMI == null || getPropertyStringMI == null || m_PropertyDrawerFI == null;
            }
        }

        private static readonly HashSet<object> modifiedHandlers = new HashSet<object>();
        private void FillMaterialPropertyHandlerCache(Shader newShader)
        {
            if (R.NullFMPHC())
            {
                Debug.LogError("?");
                return;
            }

            modifiedHandlers.Clear();

            var propertyHandlers = (IDictionary)R.s_PropertyHandlersFI.GetValue(null);

            AssetVariants.R.args[0] = newShader;
            var properties = GetMaterialProperties(targets);
            foreach (MaterialProperty property in properties)
            {
                AssetVariants.R.args[1] = property.name;
                var handler = R.getHandlerMI.Invoke(null, AssetVariants.R.args);
                if (handler == null)
                {
                    string key = (string)R.getPropertyStringMI.Invoke(null, AssetVariants.R.args);
                    handler = propertyHandlers[key];
                    if (handler == null)
                    {
                        handler = Activator.CreateInstance(R.mphType);
                        propertyHandlers[key] = handler;
                    }
                }
                if (!modifiedHandlers.Contains(handler))
                {
                    var drawer = R.m_PropertyDrawerFI.GetValue(handler) as MaterialPropertyDrawer;
                    if (drawer != null)
                    {
                        if (!(drawer is MOD))
                            R.m_PropertyDrawerFI.SetValue(handler, new MOD { original = drawer });
                    }
                    else
                        R.m_PropertyDrawerFI.SetValue(handler, emptyMOD);
                    modifiedHandlers.Add(handler);
                }
            }
        }

        private static MaterialProperty FindMaterialProperty(MaterialProperty[] srcProps, string name, ref int srcID)
        {
            // Tries at first to find the src by going to the next property from the previous
            var srcProp = srcID < srcProps.Length ? srcProps[srcID] : null;
            if (srcProp == null || srcProp.name != name)
            {
                // If it didn't match, then search whole array
                for (int ii = 0; ii < srcProps.Length; ii++)
                {
                    srcProp = srcProps[ii];
                    if (srcProp.name == name)
                    {
                        srcID = ii;
                        break;
                    }
                }
            }
            return srcProp;
        }
        private static Object[] srcObject = new Object[1], dstObject = new Object[1];
        private static bool CustomRevertAssetFromParentAsset(AV av, Object parentAsset)
        {
            if (!typeof(Material).IsAssignableFrom(parentAsset.GetType()))
                return false;

            srcObject[0] = parentAsset;
            var srcProps = MaterialEditor.GetMaterialProperties(srcObject);
            dstObject[0] = av.asset;
            var dstProps = MaterialEditor.GetMaterialProperties(dstObject);

            bool dirty = false;

            for (int i = 0, srcID = 0; i < dstProps.Length; i++, srcID++)
            {
                var dstProp = dstProps[i];

                string name = dstProp.name;

                var srcProp = FindMaterialProperty(srcProps, name, ref srcID);

                var type = dstProp.type;
                if (srcProp != null && srcProp.type == type)
                {
                    string path = PATH_PREFIX + name;

                    if (!av.overridesCache.Contains(path))
                    {
                        switch (type)
                        {
                            case MaterialProperty.PropType.Range:
                            case MaterialProperty.PropType.Float:
                                if (dstProp.floatValue != srcProp.floatValue)
                                {
                                    dstProp.floatValue = srcProp.floatValue;
                                    dirty = true;
                                }
                                break;
#if UNITY_2021_1_OR_NEWER
                            case MaterialProperty.PropType.Int:
                                if (dstProp.intValue != srcProp.intValue)
                                {
                                    dstProp.intValue = srcProp.intValue;
                                    dirty = true;
                                }
                                break;
#endif

                            case MaterialProperty.PropType.Color:
                                Color srcColor = srcProp.colorValue;
                                Color dstColor = dstProp.colorValue;
                                if (!av.overridesCache.Contains(path + ".r"))
                                    dstColor.r = srcColor.r;
                                if (!av.overridesCache.Contains(path + ".g"))
                                    dstColor.g = srcColor.g;
                                if (!av.overridesCache.Contains(path + ".b"))
                                    dstColor.b = srcColor.b;
                                if (!av.overridesCache.Contains(path + ".a"))
                                    dstColor.a = srcColor.a;
                                if (dstProp.colorValue != dstColor)
                                {
                                    dstProp.colorValue = dstColor;
                                    dirty = true;
                                }
                                break;
                            case MaterialProperty.PropType.Vector:
                                Vector4 srcVector = srcProp.vectorValue;
                                Vector4 dstVector = dstProp.vectorValue;
                                if (!av.overridesCache.Contains(path + ".x"))
                                    dstVector.x = srcVector.x;
                                if (!av.overridesCache.Contains(path + ".y"))
                                    dstVector.y = srcVector.y;
                                if (!av.overridesCache.Contains(path + ".z"))
                                    dstVector.z = srcVector.z;
                                if (!av.overridesCache.Contains(path + ".w"))
                                    dstVector.w = srcVector.w;
                                if (dstProp.vectorValue != dstVector)
                                {
                                    dstProp.vectorValue = dstVector;
                                    dirty = true;
                                }
                                break;

                            case MaterialProperty.PropType.Texture:
                                if (!av.overridesCache.Contains(path + ".texture"))
                                {
                                    if (dstProp.textureValue != srcProp.textureValue)
                                    {
                                        dstProp.textureValue = srcProp.textureValue;
                                        dirty = true;
                                    }
                                }
                                Vector4 srcScaleOffset = srcProp.textureScaleAndOffset;
                                Vector4 dstScaleOffset = dstProp.textureScaleAndOffset;
                                string tilingPath = path + ".tiling";
                                if (!av.overridesCache.Contains(tilingPath))
                                {
                                    if (!av.overridesCache.Contains(tilingPath + ".x"))
                                        dstScaleOffset.x = srcScaleOffset.x;
                                    if (!av.overridesCache.Contains(tilingPath + ".y"))
                                        dstScaleOffset.y = srcScaleOffset.y;
                                }
                                string offsetPath = path + ".offset";
                                if (!av.overridesCache.Contains(offsetPath))
                                {
                                    if (!av.overridesCache.Contains(offsetPath + ".x"))
                                        dstScaleOffset.z = srcScaleOffset.z;
                                    if (!av.overridesCache.Contains(offsetPath + ".y"))
                                        dstScaleOffset.w = srcScaleOffset.w;
                                }
                                if (dstProp.textureScaleAndOffset != dstScaleOffset)
                                {
                                    dstProp.textureScaleAndOffset = dstScaleOffset;
                                    dirty = true;
                                }
                                break;
                        }
                    }
                }
            }

            return dirty;
        }
        private static void AddDifferences(OverridesCache oc, string parentPath, List<string> parentAllDifferences, List<string> childrenAllDifferences, List<string> childrenDifferences, int maxChildCount)
        {
            if (childrenDifferences.Count == maxChildCount)
            {
                //bool alreadyContainsAChild = false;
                //for (int i = 0; i < childrenDifferences.Count; i++)
                //{
                //    if (oc.Contains(childrenDifferences[i]))
                //    {
                //        alreadyContainsAChild = true;
                //        break;
                //    }
                //}
                //if (alreadyContainsAChild)
                //{
                //    allDifferences.AddRange(childrenDifferences);
                //    return;
                //}

                bool alreadyContainsAllChildren = true;
                for (int i = 0; i < childrenDifferences.Count; i++)
                {
                    if (!oc.Contains(childrenDifferences[i]))
                    {
                        alreadyContainsAllChildren = false;
                        break;
                    }
                }

                if (!alreadyContainsAllChildren)
                    parentAllDifferences.Add(parentPath);
            }
            else
            {
                childrenAllDifferences.AddRange(childrenDifferences);
            }
        }
        private static void CustomCreateOverridesFromDifferences(AV av, Object parentAsset, List<string> allDifferences)
        {
            if (!typeof(Material).IsAssignableFrom(parentAsset.GetType()))
                return;

            srcObject[0] = parentAsset;
            var srcProps = MaterialEditor.GetMaterialProperties(srcObject);
            dstObject[0] = av.asset;
            var dstProps = MaterialEditor.GetMaterialProperties(dstObject);

            List<string> childrenDifferences = new List<string>();
            List<string> childrenDifferences2 = new List<string>();

            var oc = av.GetOverridesCache();

            for (int i = 0, srcID = 0; i < dstProps.Length; i++, srcID++)
            {
                var dstProp = dstProps[i];
                string name = dstProp.name;
                var srcProp = FindMaterialProperty(srcProps, name, ref srcID);
                var type = dstProp.type;
                if (srcProp != null && srcProp.type == type)
                {
                    string path = PATH_PREFIX + name;

                    if (!av.overridesCache.Contains(path))
                    {
                        switch (type)
                        {
                            case MaterialProperty.PropType.Range:
                            case MaterialProperty.PropType.Float:
                                if (dstProp.floatValue != srcProp.floatValue)
                                    allDifferences.Add(path);
                                break;
#if UNITY_2021_1_OR_NEWER
                            case MaterialProperty.PropType.Int:
                                if (dstProp.intValue != srcProp.intValue)
                                    allDifferences.Add(path);
                                break;
#endif

                            case MaterialProperty.PropType.Color:
                                childrenDifferences.Clear();
                                {
                                    Color srcColor = srcProp.colorValue;
                                    Color dstColor = dstProp.colorValue;
                                    if (dstColor.r != srcColor.r)
                                        childrenDifferences.Add(path + ".r");
                                    if (dstColor.g != srcColor.g)
                                        childrenDifferences.Add(path + ".g");
                                    if (dstColor.b != srcColor.b)
                                        childrenDifferences.Add(path + ".b");
                                    if (dstColor.a != srcColor.a)
                                        childrenDifferences.Add(path + ".a");
                                }
                                AddDifferences(oc, path, allDifferences, allDifferences, childrenDifferences, 4);
                                break;
                            case MaterialProperty.PropType.Vector:
                                childrenDifferences.Clear();
                                {
                                    Vector4 srcVector = srcProp.vectorValue;
                                    Vector4 dstVector = dstProp.vectorValue;
                                    if (dstVector.x != srcVector.x)
                                        childrenDifferences.Add(path + ".x");
                                    if (dstVector.y != srcVector.y)
                                        childrenDifferences.Add(path + ".y");
                                    if (dstVector.z != srcVector.z)
                                        childrenDifferences.Add(path + ".z");
                                    if (dstVector.w != srcVector.w)
                                        childrenDifferences.Add(path + ".w");
                                }
                                AddDifferences(oc, path, allDifferences, allDifferences, childrenDifferences, 4);
                                break;

                            case MaterialProperty.PropType.Texture:
                                childrenDifferences.Clear();
                                {
                                    if (dstProp.textureValue != srcProp.textureValue)
                                        childrenDifferences.Add(path + ".texture");
                                    Vector4 srcScaleOffset = srcProp.textureScaleAndOffset;
                                    Vector4 dstScaleOffset = dstProp.textureScaleAndOffset;
                                    {
                                        childrenDifferences2.Clear();
                                        string tilingPath = path + ".tiling";
                                        if (dstScaleOffset.x != srcScaleOffset.x)
                                            childrenDifferences2.Add(tilingPath + ".x");
                                        if (dstScaleOffset.y != srcScaleOffset.y)
                                            childrenDifferences2.Add(tilingPath + ".y");
                                        AddDifferences(oc, tilingPath, childrenDifferences, allDifferences, childrenDifferences2, 2);
                                    }
                                    {
                                        childrenDifferences2.Clear();
                                        string offsetPath = path + ".offset";
                                        if (dstScaleOffset.z != srcScaleOffset.z)
                                            childrenDifferences2.Add(offsetPath + ".x");
                                        if (dstScaleOffset.w != srcScaleOffset.w)
                                            childrenDifferences2.Add(offsetPath + ".y");
                                        AddDifferences(oc, offsetPath, childrenDifferences, allDifferences, childrenDifferences2, 2);
                                    }
                                }
                                AddDifferences(oc, path, allDifferences, allDifferences, childrenDifferences, 3);
                                break;
                        }
                    }
                }
            }
        }

        private static void Implicit(AV av, Object parentAsset, List<string> allDifferences)
        {
            if (!typeof(Material).IsAssignableFrom(parentAsset.GetType()))
                return;

            srcObject[0] = parentAsset;
            var srcProps = MaterialEditor.GetMaterialProperties(srcObject);
            dstObject[0] = av.asset;
            var dstProps = MaterialEditor.GetMaterialProperties(dstObject);

            for (int i = 0, srcID = 0; i < dstProps.Length; i++, srcID++)
            {
                var dstProp = dstProps[i];
                string name = dstProp.name;
                var srcProp = FindMaterialProperty(srcProps, name, ref srcID);
                var type = dstProp.type;
                if (srcProp != null && srcProp.type == type)
                {
                    string path = PATH_PREFIX + name;
                    if (!av.overridesCache.Contains(path))
                    {
                        switch (type)
                        {
                            case MaterialProperty.PropType.Range:
                            case MaterialProperty.PropType.Float:
                                if (dstProp.floatValue != srcProp.floatValue)
                                    allDifferences.Add(path);
                                break;
#if UNITY_2021_1_OR_NEWER
                            case MaterialProperty.PropType.Int:
                                if (dstProp.intValue != srcProp.intValue)
                                    allDifferences.Add(path);
                                break;
#endif

                            case MaterialProperty.PropType.Color:
                                Color srcColor = srcProp.colorValue;
                                Color dstColor = dstProp.colorValue;
                                if (dstColor.r != srcColor.r)
                                    allDifferences.Add(path + ".r");
                                if (dstColor.g != srcColor.g)
                                    allDifferences.Add(path + ".g");
                                if (dstColor.b != srcColor.b)
                                    allDifferences.Add(path + ".b");
                                if (dstColor.a != srcColor.a)
                                    allDifferences.Add(path + ".a");
                                break;
                            case MaterialProperty.PropType.Vector:
                                Vector4 srcVector = srcProp.vectorValue;
                                Vector4 dstVector = dstProp.vectorValue;
                                if (dstVector.x != srcVector.x)
                                    allDifferences.Add(path + ".x");
                                if (dstVector.y != srcVector.y)
                                    allDifferences.Add(path + ".y");
                                if (dstVector.z != srcVector.z)
                                    allDifferences.Add(path + ".z");
                                if (dstVector.w != srcVector.w)
                                    allDifferences.Add(path + ".w");
                                break;

                            case MaterialProperty.PropType.Texture:
                                if (dstProp.textureValue != srcProp.textureValue)
                                    allDifferences.Add(path + ".texture");
                                Vector4 srcScaleOffset = srcProp.textureScaleAndOffset;
                                Vector4 dstScaleOffset = dstProp.textureScaleAndOffset;
                                string tilingPath = path + ".tiling";
                                if (!av.overridesCache.Contains(tilingPath))
                                {
                                    if (dstScaleOffset.x != srcScaleOffset.x)
                                        allDifferences.Add(tilingPath + ".x");
                                    if (dstScaleOffset.y != srcScaleOffset.y)
                                        allDifferences.Add(tilingPath + ".y");
                                }
                                string offsetPath = path + ".offset";
                                if (!av.overridesCache.Contains(offsetPath))
                                {
                                    if (dstScaleOffset.z != srcScaleOffset.z)
                                        allDifferences.Add(offsetPath + ".x");
                                    if (dstScaleOffset.w != srcScaleOffset.w)
                                        allDifferences.Add(offsetPath + ".y");
                                }
                                break;
                        }
                    }
                }
            }
        }
        private static readonly List<string> allDifferences = new List<string>();
        private static void CustomImplicit(AV av, int undoGroup)
        {
            var parentAsset = av.LoadParentAsset();
            if (parentAsset == null)
                return;

            av.GetOverridesCache();

            allDifferences.Clear();
            Implicit(av, parentAsset, allDifferences);

            bool dirty = false;
            for (int ii = 0; ii < allDifferences.Count; ii++)
            {
                string path = allDifferences[ii];
                if (!av.overridesCache.Contains(path))
                {
                    av.overridesCache.Add(path);
                    dirty = true;

                    if (Helper.LogUnnecessary)
                        Debug.Log("Implicitly created override for: " + path + Helper.LOG_END);
                }
            }

            if (dirty)
                av.SaveUserDataWithUndo(false, true, undoGroup);
        }

        internal readonly MOD emptyMOD = new MOD();

        public override void OnInspectorGUI()
        {
            var header = AVHeader.Get(this);
            if (header != null)
            {
                if (header.AnyHasParent)
                {
                    var newShader = ((Material)target).shader;
                    if (newShader != shader)
                    {
                        FillMaterialPropertyHandlerCache(newShader);
                        shader = newShader;
                    }

                    MOD.avTargets = header.avTargets;
                }
                try
                {
                    base.OnInspectorGUI();
                }
                finally
                {
                    MOD.avTargets = null;
                }
            }
            else
            {
                base.OnInspectorGUI();
            }
        }

        //public override void OnDisable()
        //{
        //    base.OnDisable();

        //    shader = null;

        //    // TODO2: any other active ones should reenable them
        //}
    }
}
#endif
