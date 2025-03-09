#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace EditorReplacement
{
    public abstract class TemplateEditorReplacer : EditorReplacer
    {
        public bool IsTemplate { get; set; }

        protected override void DeepCopyTo(EditorReplacer deepCopy)
        {
            ((TemplateEditorReplacer)deepCopy).IsTemplate = IsTemplate;
        }
    }


    public abstract class SubAssetsParentEditorReplacer<ParentReplacerType, ChildReplacerType> : FilteredEditorReplacer<ParentReplacerType>
        where ParentReplacerType : SubAssetsParentEditorReplacer<ParentReplacerType, ChildReplacerType>
        where ChildReplacerType : SubAssetsChildEditorReplacer<ParentReplacerType>
    {
        /// <summary>
        /// Null for choosing every type
        /// </summary>
        public virtual Type SubAssetType => null;

        public virtual bool ImporterIgnoreImportedObject => false;

        public virtual int MaxSubAssetsToDisplay => 20;

        public override ChildAction OnAssignedTargets(IReadOnlyList<Object> _, IReadOnlyList<Object> __, EditorTreeNode node)
        {
            var targets = node.Targets;

            bool ignore = typeFilter.ShouldIgnore(targets[0].GetType());

            for (int i = 0; i < node.children.Count; i++)
            {
                var childNode = node.children[i];
                var childReplacer = childNode.replacer as ChildReplacerType;
                if (childReplacer != null && childReplacer.IsTemplate)
                {
                    node.children.RemoveAt(i);
                    i--;

                    if (ignore)
                        continue;

                    bool anyIsMainOrImporter = false;

                    List<Object>[] allSubAssets = new List<Object>[targets.Count];
                    List<int>[] allSubAssetIndices = new List<int>[targets.Count];
                    for (int ii = 0; ii < targets.Count; ii++)
                    {
                        var subAssets = new List<Object>();
                        var subAssetIndices = new List<int>();
                        allSubAssets[ii] = subAssets;
                        allSubAssetIndices[ii] = subAssetIndices;

                        var target = targets[ii];
                        var path = AssetDatabase.GetAssetPath(target);
                        if (string.IsNullOrEmpty(path))
                            continue;
                        if (path.EndsWith(".unity")) // Scene Assets, which give an error for LoadAllAssetsAtPath()
                            continue;
                        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                        subAssets.AddRange(assets);
                        for (int iii = 0; iii < subAssets.Count; iii++)
                        {
                            if (subAssets[iii] == null) // This can actually happen (if the script is missing I believe)
                            {
                                subAssets.RemoveAt(iii);
                                iii--;
                            }
                            else
                                subAssetIndices.Add(iii);
                        }
                        if (subAssets.Count == 0)
                            continue;

                        var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                        if (target == mainAsset)
                        {
                            anyIsMainOrImporter = true;
                            int index = subAssets.IndexOf(mainAsset);
                            if (index != -1) // Just to be safe
                            {
                                subAssets.RemoveAt(index);
                                subAssetIndices.RemoveAt(index);
                            }
                        }
                        else
                        {
                            var importer = AssetImporter.GetAtPath(path);
                            if (target == importer)
                            {
                                anyIsMainOrImporter = true;
                                if (ImporterIgnoreImportedObject)
                                {
                                    subAssets.RemoveAt(0);
                                    subAssetIndices.RemoveAt(0);
                                }
                            }
                            else
                            {
                                subAssets.Clear();
                                subAssetIndices.Clear();
                            }
                        }

                        //subAssets.Reverse();
                        //subAssetIndices.Reverse();
                        //subAssets.Clear();
                        //subAssetIndices.Clear();
                    }

                    if (!anyIsMainOrImporter)
                        continue;

                    List<List<Object>> allTargets = new List<List<Object>>();
                    List<List<int>> allTargetIndices = new List<List<int>>();
                    while (true)
                    {
                        List<Object> newTargets = new List<Object>();
                        List<int> newTargetIndices = new List<int>();

                        Type type = SubAssetType;
                        for (int ii = 0; ii < allSubAssets.Length; ii++)
                        {
                            var subAssets = allSubAssets[ii];
                            var subAssetIndices = allSubAssetIndices[ii];
                            for (int iii = 0; iii < subAssets.Count; iii++)
                            {
                                var asset = subAssets[iii];
                                int index = subAssetIndices[iii];
                                if (type == null || asset.GetType() == type)
                                {
                                    type = asset.GetType();
                                    subAssets.RemoveAt(iii);
                                    subAssetIndices.RemoveAt(iii);
                                    newTargets.Add(asset);
                                    newTargetIndices.Add(index);
                                    break;
                                }
                            }
                        }

                        if (newTargets.Count != 0)
                        {
                            allTargets.Add(newTargets);
                            allTargetIndices.Add(newTargetIndices);
                        }
                        else
                            break;
                    }

                    bool exceededMax = false;
                    int max = MaxSubAssetsToDisplay;
                    while (allTargets.Count > max)
                    {
                        allTargets.RemoveAt(max);
                        allTargetIndices.RemoveAt(max);
                        exceededMax = true;
                    }

                    for (int ii = 0; ii < allTargets.Count; ii++)
                    {
                        var inst = childNode.DeepCopy();
                        var instR = ((ChildReplacerType)inst.replacer);
                        instR.targets = allTargets[ii];
                        instR.targetIndices = allTargetIndices[ii];
                        instR.IsTemplate = false;
                        if (ii == allTargets.Count - 1)
                            instR.warnExceededMax = exceededMax;
                        node.children.Add(inst);
                    }

                    break;
                }
            }
            return ChildAction.Keep;
        }

        protected virtual WrapType WrapType => WrapType.Root; //TODO2: should this wrap defaults instead?

        public override void Replace(EditorTreeDef treeDef)
        {
            if (ERSettings.S.subAssets.disabled)
                return;

            if (typeof(Component).IsAssignableFrom(treeDef.inspectedType))
                return;
            if (typeof(GameObject) == treeDef.inspectedType)
                return;

            treeDef.Wrap
            (
                this, WrapType, (replacer, node) =>
                {
                    var template = replacer.CreateTemplateChild();
                    var templateR = template.replacer as ChildReplacerType;
                    if (templateR == null)
                        Debug.LogError("CreateTemplateChild().replacer does not inherit from ChildReplacerType");
                    else
                    {
                        templateR.IsTemplate = true;
                        node.children.Add(template);
                    }
                }
            );
        }

        protected abstract EditorTreeNode CreateTemplateChild();
    }

    public abstract class SubAssetsChildEditorReplacer<ParentReplacerType> : TemplateEditorReplacer
    {
        public override Type GetExpectedTargetType(Type parentTargetType, EditorTreeLocation location)
        {
            return null;
        }

        /// <summary>
        /// If this is the last SubAssets child and it should give a warning box that MaxSubAssetsToDisplay was exceeded.
        /// </summary>
        public bool warnExceededMax = false;

        internal List<Object> targets;
        internal List<int> targetIndices;
        public override IReadOnlyList<Object> GetTargets(IReadOnlyList<Object> parentTargets, EditorTreeLocation location)
        {
            if (targets == null)
            {
                Debug.LogError("What");
                return new Object[0];
            }
            return targets;
        }

        public override void Replace(EditorTreeDef treeDef) { }

        public override void OnCreatedEditor(Editor editor)
        {
            var sace = (SubAssetsChildEditor)editor;
            sace.targetIndices.Clear();
            if (targetIndices == null)
                Debug.LogError("\n");
            else
                sace.targetIndices.AddRange(targetIndices);
            sace.warnExceededMax = warnExceededMax;
        }
    }


    public class SubAssetsParentEditor : WrapperEditor
    {
        protected readonly List<bool> expanded = new List<bool>(); //TODO2: static?

        private float GetSpacing(float spacing)
        {
            if (ChildrenEditors.Count > 1)
                return spacing;
            return 0;
        }
        protected override float PreChildrenSpacing => GetSpacing(base.PreChildrenSpacing);
        protected override float PostChildrenSpacing => GetSpacing(base.PostChildrenSpacing);

        /// <summary>
        /// Spacing before and after drawing a foldout of sub assets
        /// </summary>
        protected virtual float SubAssetsSpacing => ERSettings.S.subAssets.defaultSpacing;

        public static string exceededWarning = "Exceeded MaxSubAssetsToDisplay; there are more Sub Assets that are hidden.";

        public string foldoutLabel = "Sub Assets";

        protected override void DoOnInspectorGUI()
        {
            bool subAssets = false;
            int expandedID = 0;
            bool skip = false;
            int prevIndentLevel = EditorGUI.indentLevel;

            var s = ERSettings.S.subAssets;

            int indentation = s.indented ? 1 : 0;

            for (int i = 0; i < ChildrenEditors.Count; i++)
            {
                var child = ChildrenEditors[i];

                var sace = child as SubAssetsChildEditor;
                if (sace != null)
                {
                    if (!subAssets)
                    {
                        if (expanded.Count <= expandedID)
                            expanded.Add(false);

                        GUILayout.Space(SubAssetsSpacing);
                        EditorGUI.indentLevel = prevIndentLevel + indentation;
                        expanded[expandedID] = EditorGUILayout.Foldout(expanded[expandedID], foldoutLabel, true);
                        EditorGUI.indentLevel = prevIndentLevel + 1 + indentation; // Cant simply increment and decrement, because some editors don't restore the state after setting indent
                        subAssets = true;

                        skip = !expanded[expandedID];

                        expandedID++;
                    }
                }
                else
                {
                    if (subAssets)
                    {
                        subAssets = false;
                        EditorGUI.indentLevel = prevIndentLevel;
                        GUILayout.Space(SubAssetsSpacing);
                        skip = false;
                    }
                }

                if (!skip)
                {
                    GUILayout.Space(PreChildrenSpacing);
                    child.ContextualOnInspectorGUI();
                    GUILayout.Space(PostChildrenSpacing);

                    if (subAssets)
                    {
                        if (sace.warnExceededMax)
                        {
                            ERHelper.DrawSeparationLine(s.lineColor, s.lineThickness, s.linePadding);

                            GUILayout.Space(PreChildrenSpacing);
                            EditorGUILayout.HelpBox(exceededWarning, MessageType.Warning);
                            GUILayout.Space(PostChildrenSpacing);
                        }

                        if (i + 1 < ChildrenEditors.Count)
                        {
                            var next = ChildrenEditors[i + 1];
                            if (next is SubAssetsChildEditor)
                                ERHelper.DrawSeparationLine(s.lineColor, s.lineThickness, s.linePadding);
                        }
                    }
                }
            }

            if (subAssets)
            {
                EditorGUI.indentLevel = prevIndentLevel;
                GUILayout.Space(SubAssetsSpacing);
            }
        }

#if UNITY_2019_1_OR_NEWER
        private static readonly float kIndentPerLevel = (float)typeof(EditorGUI).GetField("kIndentPerLevel", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); //15;

        protected override void FillInspectorGUI(VisualElement container)
        {
            Foldout subAssetsFoldout = null;

            var s = ERSettings.S.subAssets;

            for (int i = 0; i < ChildrenEditors.Count; i++)
            {
                var child = ChildrenEditors[i];

                var sace = child as SubAssetsChildEditor;
                if (sace != null)
                {
                    if (subAssetsFoldout == null)
                    {
                        subAssetsFoldout = new Foldout();
                        subAssetsFoldout.text = foldoutLabel;
                        //TODO: make this indentation consistent with IMGUI
                        if (s.indented)
                            subAssetsFoldout.style.marginLeft = kIndentPerLevel;
                        subAssetsFoldout.style.marginTop = SubAssetsSpacing;
                        subAssetsFoldout.style.marginBottom = SubAssetsSpacing;
                        subAssetsFoldout.value = false;
                        container.Add(subAssetsFoldout);
                    }
                }
                else
                {
                    subAssetsFoldout = null;
                }

                if (!brokenUITTargetTypes.Contains(child.target.GetType()))
                {
                    var childGUI = new WrapperInspectorElement(child);
                    childGUI.style.marginTop = PreChildrenSpacing;
                    childGUI.style.marginBottom = PostChildrenSpacing;

                    if (subAssetsFoldout != null)
                    {
                        subAssetsFoldout.Add(childGUI);

                        if (sace.warnExceededMax)
                        {
                            subAssetsFoldout.Add(ERHelper.CreateSeparationLine(s.lineColor, s.lineThickness, s.linePadding));

                            VisualElement helpBox;
#if UNITY_2020_1_OR_NEWER
                            helpBox = new HelpBox(exceededWarning, HelpBoxMessageType.Warning);
#else
                            helpBox = new Label(exceededWarning);
#endif
                            helpBox.style.marginTop = PreChildrenSpacing;
                            helpBox.style.marginBottom = PostChildrenSpacing;
                            subAssetsFoldout.Add(helpBox);
                        }

                        if (i + 1 < ChildrenEditors.Count)
                        {
                            var next = ChildrenEditors[i + 1];
                            if (next is SubAssetsChildEditor)
                                subAssetsFoldout.Add(ERHelper.CreateSeparationLine(s.lineColor, s.lineThickness, s.linePadding));
                        }
                    }
                    else
                    {
                        container.Add(childGUI);
                    }
                }
            }
        }
#endif
    }

    public class SubAssetsChildEditor : WrapperEditor
    {
        /// <summary>
        /// Weirdly enough this needs to be smaller than 1 to make it layout evenly.
        /// </summary>
        public static float singleTargetMainFoldoutFlexShrink = 0.5f;
        /// <summary>
        /// Only used for IMGUI (because UIElements doesn't need a background to hide text)
        /// </summary>
        public static float foldoutBackgroundPaddingLeft = 15;

        public float targetIndexWidth = 60;

        public GUIContent label = new GUIContent();
        public GUIContent targetsLabel = new GUIContent("Targets");
        public float targetsWidth = 60;

        public virtual bool SingleTargetUsesFoldout => ERSettings.S.subAssets.defaultSingleTargetUsesFoldout;

        /// <summary>
        /// If this is the last SubAssets child and it should give a warning box that MaxSubAssetsToDisplay was exceeded.
        /// </summary>
        public bool warnExceededMax = false;

        public List<int> targetIndices = new List<int>();

        private bool expanded = false;
        private bool targetsExpanded = false;

        protected override bool AssetAlwaysEditable => true; //TODO: EditorAlwaysEditable

        protected override void OnEnable()
        {
            base.OnEnable();

            label.text = target.GetType().Name;

            initialized = false;
        }

        protected override void ProcessEditorTree(IReplacementEditor root, EditorTreeDef treeDef)
        {
            if (ERHelper.deferEditorInitialization != null)
                foreach (var e in EnumerateEditors())
                    ERHelper.deferEditorInitialization(e);
        }

        private bool initialized;
        protected override void DoOnInspectorGUI()
        {
            var fullRect = EditorGUILayout.GetControlRect();

            var targets = this.targets;

            var prevIndent = EditorGUI.indentLevel;

            bool foldout = SingleTargetUsesFoldout || targets.Length > 1;

            float targetsWidth;
            if (foldout)
                targetsWidth = this.targetsWidth + foldoutBackgroundPaddingLeft;
            else
                targetsWidth = EditorGUI.IndentedRect(fullRect).width * 0.5f;

            // Main foldout
            var foldoutRect = fullRect;
            foldoutRect.width -= targetsWidth;
            expanded = EditorGUI.Foldout(foldoutRect, expanded, label, true);

            // Targets foldout
            var targetsRect = fullRect;
            targetsRect.x = foldoutRect.x + foldoutRect.width;
            targetsRect.width = targetsWidth;
            bool prevGUIEnabled = GUI.enabled;
            if (foldout)
            {
                // Background to hide text
                GUI.Label(targetsRect, "", EditorStyles.objectField);

                EditorGUI.indentLevel = 0;
                targetsRect.x += foldoutBackgroundPaddingLeft;
                targetsRect.width -= foldoutBackgroundPaddingLeft;
                targetsExpanded = EditorGUI.Foldout(targetsRect, targetsExpanded, targetsLabel, true);
                EditorGUI.indentLevel = prevIndent;

                // Targets
                if (targetsExpanded)
                {
                    GUI.enabled = false;
                    var targetType = target.GetType();
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var fullTargetRect = EditorGUILayout.GetControlRect();
                        var targetIndexRect = EditorGUI.IndentedRect(fullTargetRect);
                        targetIndexRect.width = targetIndexWidth;
                        var targetRect = fullTargetRect;
                        targetRect.width -= targetIndexRect.width;
                        EditorGUI.ObjectField(targetRect, targets[i], targetType, false);
                        targetIndexRect.x = targetRect.x + targetRect.width;
                        GUI.Label(targetIndexRect, " Index: " + targetIndices[i]);
                    }
                }
            }
            else
            {
                EditorGUI.indentLevel = 0;

                // Opaque background to hide text
                GUI.enabled = true;
                GUI.Label(targetsRect, "", EditorStyles.objectField);

                GUI.enabled = false;
                EditorGUI.ObjectField(targetsRect, target, target.GetType(), false);

                EditorGUI.indentLevel = prevIndent;
            }
            GUI.enabled = prevGUIEnabled;

            // Main
            if (expanded)
            {
                if (!initialized)
                {
                    initialized = true;
                    if (ERHelper.initializeEditor != null)
                        foreach (var e in EnumerateEditors())
                            ERHelper.initializeEditor(e);
#if DEBUG_ASSET_VARIANTS
                    Debug.Log("Initialized Sub-Asset GUI");
#endif
                }

                base.DoOnInspectorGUI();
            }
        }

#if UNITY_2019_1_OR_NEWER
        protected override void FillInspectorGUI(VisualElement container)
        {
            var foldouts = new VisualElement() { name = "Foldouts" };
            foldouts.style.flexDirection = FlexDirection.Row;

            // Main foldout
            var mainFoldout = new Foldout() { text = label.text };
            mainFoldout.style.flexGrow = 1;
            mainFoldout.SetValueWithoutNotify(false);

            foldouts.Add(mainFoldout);

            // Targets foldout
            var targetType = target.GetType();
            if (SingleTargetUsesFoldout || targets.Length > 1)
            {
                var targetsFoldout = new Foldout() { text = "Targets" };
                targetsFoldout.SetValueWithoutNotify(false);
                targetsFoldout.style.width = targetsWidth;
                foldouts.Add(targetsFoldout);

                container.Add(foldouts);

                // Targets
                VisualElement targetsContainer = new VisualElement() { name = "Targets Container" };
                targetsContainer.style.display = DisplayStyle.None;
                //targetsContainer.SetEnabled(false); //TODO: which version can you ping a disabled?, also remove RegisterCallback
                for (int i = 0; i < targets.Length; i++)
                {
                    VisualElement targetContainer = new VisualElement();
                    targetContainer.style.flexDirection = FlexDirection.Row;

                    var targetObject = targets[i];
                    var targetField = new ObjectField(null)
                    {
                        objectType = targetType,
                        value = targetObject,
                        allowSceneObjects = false,
                    };
                    targetField.style.flexGrow = 1;
                    targetField.style.flexBasis = 0;
                    targetContainer.Add(targetField);
                    targetField.RegisterCallback<ChangeEvent<Object>>((e) =>
                    {
                        targetField.value = targetObject;
                        e.StopImmediatePropagation();
                    }, TrickleDown.TrickleDown);

                    var indexLabel = new Label(" Index: " + targetIndices[i]);
                    indexLabel.style.width = targetIndexWidth;
                    targetContainer.Add(indexLabel);

                    targetsContainer.Add(targetContainer);
                }
                container.Add(targetsContainer);
                targetsFoldout.RegisterCallback<ChangeEvent<bool>>((e) =>
                {
                    if (e.target == targetsFoldout)
                        targetsContainer.SetDisplay(e.newValue);
                });
            }
            else
            {
                var targetField = new ObjectField(null)
                {
                    objectType = targetType,
                    value = target,
                    allowSceneObjects = false,
                };
                //targetField.SetEnabled(false); //TODO: which version can you ping a disabled?, also remove RegisterCallback
                targetField.RegisterCallback<ChangeEvent<Object>>((e) =>
                {
                    targetField.value = target;
                    e.StopImmediatePropagation();
                }, TrickleDown.TrickleDown);

                foldouts.Add(targetField);

                mainFoldout.style.flexBasis = 1;
                mainFoldout.style.flexShrink = singleTargetMainFoldoutFlexShrink;

                targetField.style.flexGrow = 1;
                targetField.style.flexBasis = 1;
                targetField.style.flexShrink = 1;

                container.Add(foldouts);
            }

            // Main
            var mainContainer = new VisualElement() { name = "Main Container" };
            mainContainer.style.display = DisplayStyle.None;
            bool filled = false;
            container.Add(mainContainer);
            mainFoldout.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                if (evt.target == mainFoldout)
                {
                    if (!filled)
                    {
                        filled = true;

                        if (ERHelper.initializeEditor != null)
                            foreach (var e in EnumerateEditors())
                                ERHelper.initializeEditor(e);

                        ChildrenCreateInspectorGUI(mainContainer);

                        //ERManager.UpdateBindingUpdateCount();
                        mainContainer.Bind(serializedObject);

#if DEBUG_ASSET_VARIANTS
                        Debug.Log("Filled Sub-Asset GUI");
#endif
                    }

                    mainContainer.SetDisplay(evt.newValue);
                }
            });
        }
#endif
    }
}

#endif
