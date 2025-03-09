#if DEBUG_ASSET_VARIANTS
#define DEBUG_DURATION
#endif

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using Object = UnityEngine.Object;
using TypeFilter = PrecisionCats.TypeFilter;
using ER = EditorReplacement.EditorReplacer;

namespace EditorReplacement
{
    /// <summary>
    /// Used only when Root.replacer.RootNeedsWrapper==true.
    /// </summary>
    public class RootWrapperReplacer : ER
    {
        public override string SystemKey => null;
        protected override bool CreateDefault => false;
        public override Type GetEditorType(Type targetType, EditorTreeLocation location)
        {
            return typeof(SpacingFreeWrapperEditor);
        }
        public override void Replace(EditorTreeDef treeDef) { }
        protected override void DeepCopyTo(ER deepCopy) { }
    }

    public class InitializeBeforeEditorReplacementAttribute : Attribute { }

    public static class ERManager
    {
        public static long UpdateCount { get; private set; } = 0;

        public static TypeFilter inspectedTypeFilter = new TypeFilter();

        private static bool ShouldIgnore(Type inspectedType)
        {
            if (inspectedTypeFilter.ShouldIgnore(inspectedType))
                return true;
            return ERSettings.S.inspectedTypeFilter.ShouldIgnore(inspectedType);
        }

        static ERManager()
        {
            AVCommonHelper.onUpdateSetting += (refreshSelection) =>
            {
                Rebuild(false);
                AVCommonHelper.RefreshInspectors(refreshSelection);
            };

            var bl = inspectedTypeFilter.blacklist;
            bl.Add(typeof(GameObject));
            bl.Add(typeof(SceneAsset));
            bl.Add(typeof(DefaultAsset));
            bl.Add(typeof(AssetImporter)); // New unity behaves different. Seems that it now looks for an editor for the importer first, and if it's the base AssetImporter then it tries for the asset itself instead.
            bl.Add(AVCommonHelper.FindType("UnityEditor.U2D.Animation.SpriteLibrarySourceAssetImporter")); // Causes some weird errors, perhaps because of that editor caching a reorderable list or something
            bl.Add(AVCommonHelper.FindType("UnityEditor.PrefabImporter")); // TODO3: what about other cases where Awake is called for an AssetImporterEditor?
            bl.Add(typeof(MonoBehaviour)); //(Non-Inherited) "MonoBehaviour" only exists for components which have missing scripts
            bl.Add(typeof(UnityEngine.Rendering.GraphicsSettings)); // TODO3: find out how in the world an EditorReplacement will cause errors in FetchSettingProviderFromAttribute()
            /*
                (2018.4.36f1)
                0x000000003402A10B (Mono JIT Code) [SettingsService.cs:96] UnityEditor.SettingsService:<FetchSettingProviderFromAttribute>m__3 (UnityEditor.AttributeHelper/MethodWithAttribute)
                0x000000003402A4C7 (Mono JIT Code) (wrapper delegate-invoke) System.Func`2<UnityEditor.AttributeHelper/MethodWithAttribute, UnityEditor.SettingsProvider>:invoke_TResult_T (UnityEditor.AttributeHelper/MethodWithAttribute)
                0x0000000034029B05 (Mono JIT Code) System.Linq.Enumerable/SelectListIterator`2<UnityEditor.AttributeHelper/MethodWithAttribute, UnityEditor.SettingsProvider>:MoveNext ()
                0x00000000326C1799 (Mono JIT Code) System.Linq.Enumerable/ConcatIterator`1<TSource_REF>:MoveNext ()
                0x00000000326AE87B (Mono JIT Code) System.Linq.Enumerable/WhereEnumerableIterator`1<TSource_REF>:ToArray ()
                ...
            */
            inspectedTypeFilter.inheritedBlacklist.Add(typeof(UnityEditor.EditorTools.EditorTool));
            inspectedTypeFilter.inheritedBlacklist.Add(AVCommonHelper.FindType("UnityEditor.EditorTools.EditorToolContext"));
        }

        public static List<Type> needsWrapperInspectedTypes = new List<Type>();

        /// <summary>
        /// You can also use the disable field of an ERSettings
        /// </summary>
        public const string Disable_EditorReplacement_Prefs_Key = "EditorReplacement.Disable";

        /// <summary>
        /// Do not modify.
        /// </summary>
        public static readonly Dictionary<Editor, IReplacementEditor> rootByEditor = new Dictionary<Editor, IReplacementEditor>();
        /// <summary>
        /// Do not modify.
        /// </summary>
        public static readonly WeakTable<SerializedObject, Editor> editorBySerializedObject = new WeakTable<SerializedObject, Editor>();

        public static event Action<IReplacementEditor> OnBeginCreatingTree;

        public static void RegisterEditor(Editor editor, IReplacementEditor rootEditor)
        {
            if (editor != null)
            {
                rootByEditor.Add(editor, rootEditor);
                editorBySerializedObject.Add(editor.serializedObject, editor);
            }
        }

        public static bool CreatingTree { get; private set; } = false;
        /// <summary>
        /// Call this even if editor is not the root. It will ensure that is true before doing anything.
        /// </summary>
        public static void CreateTree(IReplacementEditor editor)
        {
            if (DestroyingTree)
            {
                // This is definitely a Unity bug (I'm pretty sure).
                // It would seem this happens because calling DestroyImmediate() on an Editor somehow... calls OnEnable? How?
                // (When clicking off an AssetImporterEditor when there are changes still left unapplied and then pressing Apply, in 2019.4.0f1).
                /*
                EditorReplacement.ERManager:CreateTree(IReplacementEditor) (at Assets/Precision Cats/editor-replacement/ERManager.cs:241)
                EditorReplacement.WrapperEditor:OnEnable() (at Assets/Precision Cats/editor-replacement/Editors/WrapperEditor.cs:119)
                UnityEngine.Object:DestroyImmediate(Object)
                EditorReplacement.ERManager:Destroy(Editor, Boolean) (at Assets/Precision
                */
                Debug.LogWarning("CreateTree() called when DestroyingTree");
                return;
            }

            var actualEditor = (Editor)editor;

            if (actualEditor == null || actualEditor.target == null) // Can occur if code recompiles
            {
                Debug.LogWarning("(editor == null || editor.target == null) - editor: " + editor + " editor.target: " + actualEditor.target);
                return;
            }

            //Debug.Log("CreateTree() for IReplacementEditor: " + editor + " with target type: " + actualEditor.target.GetType());

            if (!CreatingTree && editor.Depth == -1)
            {
                RegisterEditor(actualEditor, editor);

#if DEBUG_DURATION
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
#endif

                //Debug.Log("Create Tree for: " + editor + "\n");

                if (OnBeginCreatingTree != null)
                    OnBeginCreatingTree(editor);

                editor.Index = 0;
                editor.Parent = null;

                var inspectedTargets = actualEditor.targets;

                bool multiEdit = inspectedTargets.Length > 1;
                var treeDefs = multiEdit ? multiEditTreeDefs : singleEditTreeDefs;

                Type inspectedType = inspectedTargets[0].GetType();

                EditorTreeDef treeDef;
                if (!treeDefs.matchType.TryGetValue(inspectedType, out treeDef))
                {
                    bool found = false;
                    for (Type baseType = inspectedType; baseType != null; baseType = baseType.BaseType) // TODO3: go to type.BaseType immediately?
                    {
                        if (treeDefs.childType.TryGetValue(baseType, out treeDef))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        treeDef = null;

                    treeDefs.matchType[inspectedType] = treeDef;
                }

                if (treeDef == null)
                {
                    editor.Depth = 0;
                    return;
                }

                if (treeDef.Root.replacer == null)
                {
                    Debug.LogWarning("(treeDef.Root.replacer == null) for inspectedType: " + inspectedType + "\n");
                    return;
                }
                Type expectedEditorType = treeDef.Root.replacer.GetEditorType(inspectedType, treeDef.RootLocation);
                if (expectedEditorType != editor.GetType())
                {
                    Debug.LogError("Mismatch between editor type and replacer.GetEditorType(inspectedType, 0). You probably need to use AVEditorReplacer.CreateInspectedTypes to enumerate all inspectedTypes/targetTypes that are conditions that determine the results of EditorReplacer.GetEditorType() and which don't already have a specific [CustomEditor()].\nInspectedType: " + inspectedType + "\nExpected editor type: " + expectedEditorType + "\nEditor type: " + editor.GetType() + "\n");
                    return;
                }

                //Debug.Log("Creating Tree for: " + editor.GetType() + "\n");

                treeDef = treeDef.DeepCopy();

                var roInspectedTargets = Array.AsReadOnly(inspectedTargets);

                var rootAction = OnAssignTargets(roInspectedTargets, roInspectedTargets, treeDef.RootLocation, true);
                if (rootAction != ChildAction.Keep)
                    Debug.LogWarning("OnAssignTargets() returned " + rootAction + " for the Root node. This is not possible because the root editor has already been created. You probably need to override EditorReplacer.RootNeedsWrapper to true.");

                // FinalPostProcess
                ERHelper.PostProcess<EditorTreeLocation, ER, EditorTreeDef, EditorTreeNode>
                (
                    treeDef.RootLocation,
                    (EditorTreeLocation loc) =>
                    { loc.node.replacer.FinalPostProcess(loc.node, loc.treeDef); }
                );

                CreatingTree = true;
                try
                {
                    // Calls this here because the root is already created by Unity
                    if (treeDef.Root.replacer != null)
                        treeDef.Root.replacer.OnCreatedEditor(actualEditor);

                    CreateChildrenNodes(editor, roInspectedTargets, inspectedType, treeDef.RootLocation, null);

                    foreach (var e in editor.EnumerateEditors())
                    {
                        var re = e as IReplacementEditor;
                        if (re != null)
                            re.ProcessTree(editor, treeDef);
                    }

                    foreach (var e in editor.EnumerateEditors())
                    {
                        var re = e as IReplacementEditor;
                        if (re != null)
                            re.OnCreatedTree(editor, treeDef);
                    }

                    foreach (var replacer in treeDef.Root)
                        replacer.OnCreatedTree(editor, treeDef);
                }
                catch (Exception e) { Debug.LogError(e); }
                CreatingTree = false;

#if DEBUG_DURATION
                Debug.Log("CreateTree() duration: " + sw.Elapsed.TotalMilliseconds + " ms\n");
#endif
            }
        }
        private static Editor CreateNode(Editor parentEditor, IReadOnlyList<Object> parentTargets, Type parentTargetType, EditorTreeLocation loc, IReplacementEditor rootRE)
        {
            IReadOnlyList<Object> targets;
            Type targetType;
            Type editorType;
            if (loc.node.replacer == null)
            {
                targets = parentTargets;
                if (!AVCommonHelper.TargetsAreEqual(targets, loc.node.Targets)) //targets != loc.node.Targets //?
                    Debug.LogError(targets + " != " + loc.node.Targets);
                targetType = parentTargetType;
                editorType = OriginalFindCustomEditorTypeByType(targetType, loc.treeDef.multiEdit);
                //Debug.Log("Found original with target type: " + targetType + " and editor type: " + editorType);
            }
            else
            {
                if (loc.node.replacer.ShouldSkip(parentEditor))
                    return null;

                targets = loc.node.Targets;
                targetType = targets[0].GetType();
                var expectedTargetType = loc.node.replacer.GetExpectedTargetType(parentTargetType, loc);
                if (expectedTargetType != null && expectedTargetType != targetType)
                    Debug.LogError("replacer.GetExpectedTargetType(parentTargetType) != replacer.GetTargets(parentTargets)[0].GetType()");
                editorType = loc.node.replacer.GetEditorType(targetType, loc);
            }

            //Debug.Log("CreateNode - " + parentTargetType + " - " + editorType);

            if (editorType == null)
                editorType = ERHelper.genericInspectorType;

            Object[] targetsCopy = new Object[targets.Count];
            for (int i = 0; i < targetsCopy.Length; i++)
                targetsCopy[i] = targets[i];
            Editor editor;
            using (new EditorCreationScope(true)) // This scope exists because UnityEditor.Rendering.HighDefinition.DecalProjectorEditor does "m_MaterialEditor = (MaterialEditor)CreateEditor(materials);" in OnEnable() (and OnInspectorGUI())
            {
                editor = parentEditor.CreateEditor(targetsCopy, editorType);
            }
            if (editor == null)
            {
                Debug.LogWarning("Editor.CreateEditor() == null");
                return null;
            }
            if (editor is AssetImporterEditor)
                TryInitAssetImporterEditor((AssetImporterEditor)editor, (IReplacementEditor)parentEditor);
            if (loc.node.replacer == null)
                InitOriginalEditor(editor, (Editor)rootRE);
            else
                loc.node.replacer.OnCreatedEditor(editor);
            RegisterEditor(editor, rootRE);

            var re = editor as IReplacementEditor;
            if (re != null)
            {
                CreateChildrenNodes(re, targets, targetType, loc, rootRE);
            }

            return editor;
        }
        private static void CreateChildrenNodes(IReplacementEditor re, IReadOnlyList<Object> parentTargets, Type parentTargetType, EditorTreeLocation loc, IReplacementEditor rootRE)
        {
            //Debug.Log(node.children.Count + " CreateChildrenNodes of " + parentTargetType);

            re.Depth = loc.depth;

            if (rootRE == null)
                rootRE = re;
            re.Root = rootRE;

            for (int i = 0; i < loc.node.children.Count; i++)
            {
                var childLoc = new EditorTreeLocation(loc.depth + 1, i, loc.node.children[i], loc.treeDef, loc);
                var childEditor = CreateNode((Editor)re, parentTargets, parentTargetType, childLoc, rootRE);
                if (childEditor != null)
                {
                    re.ChildrenEditors.Add(childEditor);
                    var childRE = childEditor as IReplacementEditor;
                    if (childRE != null)
                    {
                        childRE.Index = i;
                        childRE.Parent = re;
                    }
                }
            }
        }

        private static ChildAction OnAssignTargets(IReadOnlyList<Object> inspectedTargets, IReadOnlyList<Object> parentTargets, EditorTreeLocation loc, bool isRoot)
        {
            if (loc.node.replacer == null)
            {
                loc.node.Targets = parentTargets;

                return ChildAction.Keep;
            }
            else
            {
                var targets = loc.node.replacer.GetTargets(parentTargets, loc);

                if (targets.Count == 0)
                    return ChildAction.RemoveWhole; //?

                if (isRoot)
                {
                    if (!AVCommonHelper.TargetsAreEqual(targets, inspectedTargets))
                        Debug.LogError("The root EditorReplacer's GetTargets() returned targets that do not match inspectedTargets. You probably need to override RootNeedsWrapper to true.");
                }

                loc.node.Targets = targets;

                var action = loc.node.replacer.OnAssignedTargets(inspectedTargets, parentTargets, loc.node);
                if (action != ChildAction.Keep)
                    return action;

                for (int i = 0; i < loc.node.children.Count; i++)
                {
                    var childNode = loc.node.children[i];
                    var childLoc = new EditorTreeLocation(loc.depth + 1, i, childNode, loc.treeDef, loc);
                    var childAction = OnAssignTargets(inspectedTargets, targets, childLoc, false);
                    if (childAction == ChildAction.RemoveReparent)
                    {
                        // Inserts childNode's children after childNode in the correct sequence
                        for (int ii = 0; ii < childNode.children.Count; ii++)
                            loc.node.children.Insert(i + 1 + ii, childNode.children[ii]);
                    }
                    if (childAction != ChildAction.Keep)
                    {
                        loc.node.children.RemoveAt(i);
                        i--;
                    }
                }

                return ChildAction.Keep;
            }
        }

        public static bool DestroyingTree { get; private set; } = false;
        public static void DestroyTree(IReplacementEditor editor, bool destroyRoot = false)
        {
            if (editor == null || DestroyingTree)
                return;

            DestroyingTree = true;
            try
            {
                var actualEditor = (Editor)editor;
                if (actualEditor.target != null) //?
                    Destroy(actualEditor, destroyRoot);
            }
            catch (Exception e) { Debug.LogError(e); }
            DestroyingTree = false;
        }
        private static void Destroy(Editor editor, bool destroySelf)
        {
            var re = editor as IReplacementEditor;
            if (re != null)
            {
                if (ERHelper.forgetEditor != null)
                {
                    for (int i = 0; i < re.ChildrenEditors.Count; i++)
                        ERHelper.forgetEditor(re.ChildrenEditors[i]);
                    for (int i = 0; i < re.AssetEditors.Count; i++)
                        ERHelper.forgetEditor(re.AssetEditors[i].assetEditor);
                }
                for (int i = 0; i < re.ChildrenEditors.Count; i++)
                    Destroy(re.ChildrenEditors[i], true);
                re.ChildrenEditors.Clear();
                for (int i = 0; i < re.AssetEditors.Count; i++)
                    Object.DestroyImmediate(re.AssetEditors[i].assetEditor);
                re.AssetEditors.Clear();

                re.Root = null;
                re.Depth = -1;
                re.Index = 0;
                re.Parent = null;

                //Debug.Log("Destroy IReplacementEditor " + re);
            }

            //var so = editor.serializedObject;

            if (destroySelf)
            {
                // TODO: fix the problem with ModelImporter, or rather every importer?
                //bool prev = Debug.unityLogger.logEnabled;
                //Debug.unityLogger.logEnabled = false;
                //try
                //{
                Object.DestroyImmediate(editor);
                //}
                //finally
                //{
                //    Debug.unityLogger.logEnabled = prev;
                //}
            }

            rootByEditor.Remove(editor); //editorBySerializedObject.Remove(so);
        }

        public static Action headerFlush;

        /// <summary>
        ///Makes MaterialEditor not be a foldout
        /// </summary>
        private static readonly PropertyInfo firstInspectedEditorMI = typeof(Editor).GetProperty("firstInspectedEditor", R.npi);
        public static void InitOriginalEditor(this Editor originalEditor, Editor rootEditor)
        {
            //TODO2: should this also be used to init wrapper editors?

            headerFlush += () =>
            {
                //Debug.Log("InitOriginalEditor " + originalEditor);
                firstInspectedEditorMI.SetValue(originalEditor, firstInspectedEditorMI.GetValue(rootEditor));
            };
        }
        private static readonly MethodInfo internalSetAssetImporterTargetEditorMI = typeof(AssetImporterEditor).GetMethod("InternalSetAssetImporterTargetEditor", R.npi);
        private static readonly PropertyInfo assetEditorPI = typeof(AssetImporterEditor).GetProperty("assetEditor", R.npi);
        /// <summary>
        /// Makes it so ModelImporter's preview window doesn't cause errors,
        /// and it enables the Revert/Apply buttons for an importer editor.
        /// </summary>
        public static void TryInitAssetImporterEditor(this AssetImporterEditor editor, IReplacementEditor parent)
        {
            var importerTargets = editor.targets;
            Object[] assetTargets = new Object[importerTargets.Length];
            for (int i = 0; i < assetTargets.Length; i++)
            {
                var path = AssetDatabase.GetAssetPath(importerTargets[i]);
                assetTargets[i] = AssetDatabase.LoadMainAssetAtPath(path);
            }
            Editor assetEditor;
            using (new EditorCreationScope(true))
            {
                assetEditor = editor.CreateEditor(assetTargets);
            }
            parent.AssetEditors.Add(new AssetEditor() { importerEditor = editor, assetEditor = assetEditor });

            if (internalSetAssetImporterTargetEditorMI != null)
            {
                R.arg[0] = assetEditor;
                internalSetAssetImporterTargetEditorMI.Invoke(editor, R.arg);
            }
            else
            {
                assetEditorPI.SetValue(editor, assetEditor);
            }
        }

        public static readonly TreeDefs singleEditTreeDefs = new TreeDefs();
        public static readonly TreeDefs multiEditTreeDefs = new TreeDefs();
        public class TreeDefs
        {
            public readonly Dictionary<Type, EditorTreeDef> matchType = new Dictionary<Type, EditorTreeDef>();
            public readonly Dictionary<Type, EditorTreeDef> childType = new Dictionary<Type, EditorTreeDef>();
        }

        private static IDictionary original_kSCustomEditors = null;
        private static IDictionary original_kSCustomMultiEditors = null;
        private static IDictionary replaced_kSCustomEditors = null;
        private static IDictionary replaced_kSCustomMultiEditors = null;
        internal static void SetOriginal()
        {
            if (original_kSCustomEditors == null)
            {
                //Debug.LogWarning("\n");
                return;
            }

            R.kSCustomEditorsFI.SetValue(null, original_kSCustomEditors);
            R.kSCustomMultiEditorsFI.SetValue(null, original_kSCustomMultiEditors);
        }
        internal static bool IsOriginal()
        {
            return R.kSCustomEditorsFI.GetValue(null) == original_kSCustomEditors;
        }
        internal static void SetReplaced()
        {
            if (replaced_kSCustomEditors == null)
            {
                //Debug.LogWarning("\n");
                return;
            }

            R.kSCustomEditorsFI.SetValue(null, replaced_kSCustomEditors);
            R.kSCustomMultiEditorsFI.SetValue(null, replaced_kSCustomMultiEditors);
        }
        internal static bool IsReplaced()
        {
            return R.kSCustomEditorsFI.GetValue(null) == replaced_kSCustomEditors;
        }

        /// <summary>
        /// Get the original editor type by inspected type
        /// </summary>
        public static Type OriginalFindCustomEditorTypeByType(Type type, bool multiEdit)
        {
            if (original_kSCustomEditors != null)
                SetOriginal();
            else
                Debug.LogWarning("original_kSCustomEditors is null"); //?

            var editorType = FindCustomEditorTypeByType(type, multiEdit);

            if (original_kSCustomEditors != null)
                SetReplaced();

            return editorType;
        }
        /// <summary>
        /// Get the current editor type by inspected type, including replaced root editors, instead of having to do Editor.CreateEditor() to find the type.
        /// </summary>
        public static Type FindCustomEditorTypeByType(Type type, bool multiEdit)
        {
            R.args[0] = type;
            R.args[1] = multiEdit;
            return (Type)R.findCustomEditorTypeByTypeMI.Invoke(null, R.args);
        }

        private static EditorTreeDef CreateTreeDef(Type inspectedType, Type rpType, List<MonoEditorType> monoEditorTypes, Dictionary<Type, EditorTreeDef> treeDefs, bool multiEdit, bool forChildClasses)
        {
            var treeDef = new EditorTreeDef(inspectedType, rpType, multiEdit, forChildClasses, monoEditorTypes);

            for (int i = 0; i < ER.sses.Count; i++)
            {
                var replacer = ER.sses[i];

                if (!replacer.cachedActive)
                    continue;

                string key = replacer.SystemKey;

                #region Check for overlap with keys already in tree
                ER overlapping = null;
                foreach (var r in treeDef.Root)
                {
                    if (key == r.SystemKey)
                    {
                        overlapping = r;
                        break;
                    }
                }
                if (overlapping != null)
                    Debug.LogError(replacer + "'s SystemKey: " + key + " overlaps with " + overlapping + "'s SystemKey.\nA key is only allowed to be shared if for every tree (values of inspectedType/renderPipelineType/multiEdit/forChildClasses/monoEditorTypes), at most one EditorReplacer of the same key intends to change the tree in EditorReplacer.Replace(); shared keys are only allowed if the same key doesn't overlap for any combination of inspectedType/renderPipelineType/multiEdit/forChildClasses/monoEditorTypes.\n"); //TODO: don't spam the console with lag
                #endregion

                replacer.Replace(treeDef);
            }

            ERHelper.PostProcess<EditorTreeLocation, ER, EditorTreeDef, EditorTreeNode> // Wow thank you c# for being able to infer the types...
                (treeDef.RootLocation);

            bool inspectedTypeNeedsWrapper = needsWrapperInspectedTypes.Contains(inspectedType);
            if (inspectedTypeNeedsWrapper || treeDef.Root.replacer != null)
            {
                if (inspectedTypeNeedsWrapper || treeDef.Root.replacer.RootNeedsWrapper)
                    treeDef.WrapRoot(new RootWrapperReplacer());
                treeDefs[inspectedType] = treeDef;
            }

            return treeDef;
        }
        private static void CreateReplacements(IDictionary replaced, Type inspectedType, IList originalMETs, TreeDefs treeDefs, bool multiEdit)
        {
            List<Type> rpTypes = new List<Type>()
            {
                null
            };

            for (int i = 0; i < ER.sses.Count; i++)
            {
                var rRPTypes = ER.sses[i].GetRenderPipelineTypes(inspectedType);
                if (rRPTypes != null)
                {
                    foreach (var rpType in rRPTypes)
                    {
                        if (!rpTypes.Contains(rpType))
                            rpTypes.Add(rpType);
                    }
                }
            }

            List<MonoEditorType> wrappedMETs = null;
            if (originalMETs != null)
            {
                wrappedMETs = new List<MonoEditorType>();
                for (int i = 0; i < originalMETs.Count; i++)
                    wrappedMETs.Add(new MonoEditorType(originalMETs[i]));

                for (int i = 0; i < wrappedMETs.Count; i++)
                {
                    Type rpType = wrappedMETs[i].RenderPipelineType;
                    if (!rpTypes.Contains(rpType)) // TODO2: only add if not fallback?
                        rpTypes.Add(rpType);
                }
            }

            var replacedMETs = (IList)Activator.CreateInstance(R.monoEditorTypeListType);

            for (int i = 0; i < rpTypes.Count; i++)
            {
                var rpType = rpTypes[i];

                var matchTypeTree = CreateTreeDef(inspectedType, rpType, wrappedMETs, treeDefs.matchType, multiEdit, false);
                var childTypeTree = CreateTreeDef(inspectedType, rpType, wrappedMETs, treeDefs.childType, multiEdit, true);

                if (matchTypeTree.Root.replacer != null)
                {
                    var met = MonoEditorType.CreateActual
                    (
                        inspectedType: inspectedType,
                        inspectorType: matchTypeTree.Root.replacer.GetEditorType(inspectedType, matchTypeTree.RootLocation),
                        renderPipelineType: rpType,
                        editorForChildClasses: false,
                        isFallback: false
                    );
                    replacedMETs.Add(met);
                }

                if (childTypeTree.Root.replacer != null)
                {
                    var met = MonoEditorType.CreateActual
                    (
                        inspectedType: inspectedType,
                        inspectorType: childTypeTree.Root.replacer.GetEditorType(inspectedType, childTypeTree.RootLocation),
                        renderPipelineType: rpType,
                        editorForChildClasses: true,
                        isFallback: false
                    );
                    replacedMETs.Add(met);
                }
            }

            if (wrappedMETs != null)
            {
                // Add the original types
                for (int i = 0; i < wrappedMETs.Count; i++)
                    replacedMETs.Add(wrappedMETs[i].actual);
            }

            //TODO2: make wrappedMETs modify the originals!

            if (replacedMETs.Count > 0)
                replaced[inspectedType] = replacedMETs;

            replacedInspectedTypes.Add(inspectedType);
        }

        private static void AddDefaultEditor(IList replacedMETs, Type inspectedType)
        {
            replacedMETs.Add(MonoEditorType.CreateActual(inspectedType, ERHelper.genericInspectorType, null, true, false)); // TODO2: not for children?
        }

        private static readonly HashSet<Type> replacedInspectedTypes = new HashSet<Type>();
        private static void FillReplaced(IDictionary replaced, IDictionary original, TreeDefs remaps, bool multiEdit)
        {
            replaced.Clear();
            remaps.matchType.Clear();
            remaps.childType.Clear();

            replacedInspectedTypes.Clear();

            foreach (var key in original.Keys)
            {
                Type inspectedType = (Type)key;
                var originalMETs = (IList)original[key];
                if (ShouldIgnore(inspectedType))
                {
                    var replacedMETs = (IList)Activator.CreateInstance(R.monoEditorTypeListType);
                    for (int i = 0; i < originalMETs.Count; i++)
                        replacedMETs.Add(originalMETs[i]); //Debug.Log(new MonoEditorType(originalMETs[i]).IsFallback);
                    AddDefaultEditor(replacedMETs, inspectedType); // In case the originalMETs are fallbacks
                    replaced[inspectedType] = replacedMETs;
                }
                else
                    CreateReplacements(replaced, inspectedType, originalMETs, remaps, multiEdit);
            }

            List<Type> blacklisted = new List<Type>();
            blacklisted.AddRange(inspectedTypeFilter.blacklist);
            blacklisted.AddRange(inspectedTypeFilter.inheritedBlacklist);
            var inspectedTypeFilter2 = ERSettings.S.inspectedTypeFilter.TypeFilter;
            blacklisted.AddRange(inspectedTypeFilter2.blacklist);
            blacklisted.AddRange(inspectedTypeFilter2.inheritedBlacklist);
            for (int i = 0; i < blacklisted.Count; i++)
            {
                Type inspectedType = blacklisted[i];
                if (inspectedType == null)
                    continue;
                if (!replaced.Contains(inspectedType))
                {
                    var replacedMETs = (IList)Activator.CreateInstance(R.monoEditorTypeListType);
                    AddDefaultEditor(replacedMETs, inspectedType);
                    replaced[inspectedType] = replacedMETs;
                }
            }

            for (int i = 0; i < ER.sses.Count; i++)
            {
                var replacer = ER.sses[i];

                if (!replacer.cachedActive)
                    continue;

                var createInspectedTypes = replacer.CreateInspectedTypes;
                if (createInspectedTypes != null)
                {
                    foreach (Type inspectedType in createInspectedTypes)
                    {
                        if (inspectedType != null && !ShouldIgnore(inspectedType))
                        {
                            if (!replacedInspectedTypes.Contains(inspectedType))
                                CreateReplacements(replaced, inspectedType, null, remaps, multiEdit);
                        }
                    }
                }
            }
        }

        private static void FillReplaced()
        {
            FillReplaced(replaced_kSCustomEditors, original_kSCustomEditors, singleEditTreeDefs, false);
            FillReplaced(replaced_kSCustomMultiEditors, original_kSCustomMultiEditors, multiEditTreeDefs, true);
        }

        public static bool Disabled
        {
            get
            {
                if (EditorProjectPrefs.GetBool(Disable_EditorReplacement_Prefs_Key, false))
                    return true;
                ERSettings.LoadSettings();
                return ERSettings.S.disabled;
            }
        }

        public static void Rebuild() { Rebuild(true); }
        public static void Rebuild(bool refreshInspectors)
        {
            Build();

            if (refreshInspectors)
                AVCommonHelper.RefreshInspectors();
        }
        private static void Build()
        {
            if (R.s_InitializedFI == null)
            {
                Debug.LogError("EditorReplacement is not supported yet for this version of Unity, if you want it to just contact me.");
                Disable();
                return;
            }

            if (Disabled)
            {
                Disable();
                return;
            }

            if (//R.s_InitializedFI != null ? 
                !((bool)R.s_InitializedFI.GetValue(null))
                // : R.customEditorCacheType != null && ((R.m_CustomEditorCache.GetValue(R.m_Cache.GetValue(R.instance.GetValue(null))) as IDictionary).Count > 0)
            )
            {
#if UNITY_2019_1_OR_NEWER
                R.rebuildMI.Invoke(null, null);
#else
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) //?
                {
                    R.arg[0] = assembly;
                    R.rebuildMI.Invoke(null, R.arg);
                }
#endif
                // if (R.s_InitializedFI != null)
                R.s_InitializedFI.SetValue(null, true);
            }

            if (original_kSCustomEditors == null)
            {
                original_kSCustomEditors = (IDictionary)R.kSCustomEditorsFI.GetValue(null);
                original_kSCustomMultiEditors = (IDictionary)R.kSCustomMultiEditorsFI.GetValue(null);
                var type = original_kSCustomEditors.GetType();
                replaced_kSCustomEditors = (IDictionary)Activator.CreateInstance(type);
                replaced_kSCustomMultiEditors = (IDictionary)Activator.CreateInstance(type);
            }

            ER.UpdateSSes();

            FillReplaced();

            SetReplaced();
        }

        internal static void Disable()
        {
            if (original_kSCustomEditors != null)
            {
                SetOriginal();
                original_kSCustomEditors = null;
                original_kSCustomMultiEditors = null;
                replaced_kSCustomEditors = null;
                replaced_kSCustomMultiEditors = null;

                AVCommonHelper.RefreshInspectors();
            }
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            foreach (var method in TypeCache.GetMethodsWithAttribute<InitializeBeforeEditorReplacementAttribute>())
            {
                try { method.Invoke(null, null); }
                catch (Exception e) { Debug.LogError(e); }
            }

            EditorApplication.update += () =>
            {
                UpdateCount++;

                ER.FlushRebuildSSes();
            };

            ER.rebuild += Rebuild;

            if (Disabled)
                return;

            //EditorApplication.delayCall += () =>
            //{
#if DEBUG_DURATION
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            Build();

            // Because at this point the header for an importer is not fully loaded, it seems.
            // TODO: why is that? It's fine without Editor Replacement
            EditorApplication.delayCall += () =>
            {
                AVCommonHelper.RefreshInspectors(false, true);
            };

#if DEBUG_DURATION
            Debug.Log("Init() duration: " + sw.Elapsed.TotalMilliseconds + " ms\n");
#endif
            //};

            //TODO: is this an odin thing? is it only in versions like 2022.1.13fi
            EditorApplication.delayCall += () =>
            {
                Build();
                AVCommonHelper.RefreshInspectors(false, true);
            };
        }
    }
}
#endif
