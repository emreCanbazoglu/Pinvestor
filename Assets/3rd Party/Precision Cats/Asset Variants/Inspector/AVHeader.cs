#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using Object = UnityEngine.Object;
using S = AssetVariants.AVSettings;
using PF = AssetVariants.PropertyFilter;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
#endif

namespace AssetVariants
{
    internal static class FilterSetup
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            PF.AddEmulationProperty(typeof(Material), new StringHashSetEmulationProperty("m_ShaderKeywords", " "), true); //TODO: are semi-colons ever used for this?
            PF.AddEmulationProperty(typeof(Material), new HashSetEmulationProperty("m_ValidKeywords"), true);
            PF.AddEmulationProperty(typeof(Material), new HashSetEmulationProperty("m_InvalidKeywords"), true);
            PF.AddEmulationProperty(typeof(Material), new DictionaryEmulationProperty("stringTagMap", "first"), true);
            PF.AddEmulationProperty(typeof(Material), new HashSetEmulationProperty("disabledShaderPasses"), true);

            PF.AddEmulationProperty(typeof(AnimatorOverrideController), new DictionaryEmulationProperty("m_Clips", "m_OriginalClip"), true);

            PF.InheritedIgnorePropertyPath(typeof(Font), "m_FontData"); // This can have hundreds of thousands of array elements
            PF.InheritedIgnorePropertyPath(typeof(Texture), "m_ImageContentsHash");
            PF.IgnorePropertyPath(typeof(Texture2D), "m_CompleteImageSize");
            var assetType = AVCommonHelper.FindType("TMPro.TMP_Asset");
            if (assetType != null)
            {
                PF.InheritedIgnorePropertyPath(assetType, "hashCode");
                PF.InheritedIgnorePropertyPath(assetType, "materialHashCode");
            }

            // AVNonSOEditor:
            PF.IgnorePropertyPath(typeof(AudioImporter), "m_PreviewData");
            PF.IgnorePropertyPath(typeof(ModelImporter), "m_ImportedTakeInfos"); //?
            PF.IgnorePropertyPath(typeof(ModelImporter), "m_ImportedRoots");
            // I don't know what these are, but just to be safe they're ignored
            PF.InheritedIgnorePropertyPath(typeof(AssetImporter), "m_ExternalObjects");
            PF.InheritedIgnorePropertyPath(typeof(AssetImporter), "m_UsedFileIDs");

            // AVEditorReplacer Filtering
            var bl = AVFilter.typeFilter.blacklist;
            bl.Add(typeof(MonoScript));
            bl.Add(typeof(DefaultAsset)); // Folders for example
            bl.Add(AVCommonHelper.FindType("UnityEditor.TextScriptImporter")); // There's just basically no reason for it
            bl.Add(typeof(Shader)); // Shader is afaik always purely generated (e.g. by ShaderImporter). I believe it's always non-editable, but it might as well be ignored early on.
            bl.Add(AVCommonHelper.FindType("UnityEditorInternal.AssemblyDefinitionImporter")); // Just no reason to use Asset Variants with it
            bl.Add(typeof(UnityEditor.Animations.AnimatorController));
            bl.Add(AVCommonHelper.FindType("UnityEditor.PrefabImporter"));
            var ibl = AVFilter.typeFilter.inheritedBlacklist;
            ibl.Add(typeof(SystemPrefs.SystemPrefsState)); //?

            AVFilter.typeFilter.Add(PrecisionCats.ProjectSettingsFiltering.filter);
        }
    }

    public static class AVFilter
    {
        public static readonly PrecisionCats.TypeFilter typeFilter = new PrecisionCats.TypeFilter();

        public static bool Should(Object target)
        {
            if (target == null)
                return false;
            var targetType = target.GetType();
            if (!Should(targetType))
                return false;
            if (!EditorUtility.IsPersistent(target))
                return false;
            return AVCommonHelper.IsEditable(target);
        }
        public static bool Should(Type targetType)
        {
            if (typeof(Component).IsAssignableFrom(targetType))
                return false;

            if (typeFilter.ShouldIgnore(targetType))
                return false;
            var s = S.S;
            if (s.typeFilter.ShouldIgnore(targetType))
                return false;

            if (s.scriptableObjectOnly || s.materialOnly)
            {
                if (s.scriptableObjectOnly && typeof(ScriptableObject).IsAssignableFrom(targetType))
                    return true;
                if (s.materialOnly && typeof(Material).IsAssignableFrom(targetType))
                    return true;
                return false;
            }

            if (typeof(Material).IsAssignableFrom(targetType))
                if (s.disableMaterialVariants)
                    return false;

            return true;
        }
    }

    public sealed class AVHeader
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            Editor.finishedDefaultHeaderGUI += (Editor e) =>
            {
                var header = Get(e);
                if (header != null)
                    header.Draw();
            };

            EditorApplication.update += Update;

            Undo.undoRedoPerformed += () =>
            {
                InitAllTargets();
                EditorApplication.delayCall += () =>
                {
                    OverrideIndicator.ClearCaches();
                };
            };
        }

        private static void Update()
        {
            if (!EditorGUIUtility.editingTextField)
            {
                for (int i = all.list.Count - 1; i >= 0; i--)
                {
                    var pair = all.list[i];
                    Editor e;
                    // (Redundant checks) //TODO:
                    if (pair.value != null && (!pair.value.editor.TryGetTarget(out e) || e == null))
                        all.list.RemoveAt(i);
                    else if (!pair.key.TryGetTarget(out e) || e == null)
                        all.list.RemoveAt(i);
                    else
                    {
                        var h = pair.value;
                        if (h != null)
                        {
                            for (int ii = 0; ii < h.targets.Length; ii++)
                            {
                                int prev = h.dirtyCounts[ii];
                                var o = h.targets[ii];
                                int curr = EditorUtility.GetDirtyCount(o);
                                if (curr != prev)
                                {
                                    h.dirtyCounts[ii] = curr;

                                    if (prev != -1) // TODO:?
                                    {
                                        using (var av = AV.Open(o))
                                        {
                                            if (av != null && av.Valid())
                                            {
                                                //Debug.Log("Header Undo Group: " + AVUpdater.GetUndoGroup(av.asset));
                                                av.TryDirtyImplicit(AVUpdater.GetUndoGroup(av.asset));
                                                av.childrenNeedUpdate = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static event Action<AVHeader> OnCreated;
        private static readonly WeakTable<Editor, AVHeader> all = new WeakTable<Editor, AVHeader>();
        public static AVHeader Get(Editor editor)
        {
            AVHeader avHeader;
            if (!all.TryGetValue(editor, out avHeader))
            {
                bool hasNull = false;
                var targets = editor.targets;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null)
                    {
                        hasNull = true;
                        break;
                    }
                }
                if (!hasNull && AVFilter.Should(editor.target))
                {
                    avHeader = new AVHeader(editor);
                    if (OnCreated != null)
                        OnCreated(avHeader);
                }
                else
                    avHeader = null;
                all.Add(editor, avHeader);
            }
            return avHeader;
        }
        public AVHeader(Editor editor)
        {
            this.editor = new WeakReference<Editor>(editor);
            targets = editor.targets;
            dirtyCounts = new int[targets.Length];
            for (int i = 0; i < dirtyCounts.Length; i++)
                dirtyCounts[i] = -1;
            avTargets = AVTargets.Get(editor.serializedObject);

#if ODIN_INSPECTOR
            OdinOverrideDrawer.marginsStyle = null;
#endif

            allowParentAssignment.Add((Object newParent) =>
            {
                if (newParent == null)
                    return true;
                var type = newParent.GetType();
                for (int i = 0; i < allowedParentTypes.Count; i++)
                {
                    if (allowedParentTypes[i].IsAssignableFrom(type))
                        return true;
                }
                return false;
            });

            InitTargets(true);
        }

        public readonly WeakReference<Editor> editor;
        public readonly Object[] targets;
        public readonly int[] dirtyCounts;
        internal readonly AVTargets avTargets;

        public List<Type> allowedParentTypes = new List<Type>() { typeof(Object) };
        public List<Func<Object, bool>> allowParentAssignment = new List<Func<Object, bool>>();

        /// <summary>
        /// Key is asset type (target type), and Value is parent asset type for the parent picker.
        /// Like the virtual property ParentFilterType, but you don't need to inherit from an AVEditor to specify it. You can do it from [InitializeOnLoadMethod].
        /// </summary>
        public static readonly Dictionary<Type, Type> parentFilterTypes = new Dictionary<Type, Type>();
        public Type parentFilterType = null;

        public RawViewWindow RawViewWindow { get; private set; }
        public void CloseRawViewWindow()
        {
            if (RawViewWindow != null)
            {
                RawViewWindow.Close();
                RawViewWindow = null;
            }
        }

        private bool expandChildren = EditorProjectPrefs.GetBool("AVHeader.expandChildren");
        private static GUIContent rawViewButtonLabel = new GUIContent("Raw View");
        private static GUIStyle rawViewButtonStyle = null;
        private static readonly GUIContent childrenLabel = new GUIContent("Children");
        private static readonly GUIContent parentLabel = new GUIContent("Parent");
        private void Draw()
        {
            if (mainTargetID == -1)
                InitTargets();

            var avs = avTargets.AVs;

            if (S.S.debugTypes)
            {
                GUILayout.Space(5);

                AV av = avs[mainTargetID];
                Object parent = av.LoadParentAsset();
                var debugRect = EditorGUILayout.GetControlRect();
                debugRect.width *= 0.5f;
                EditorGUI.LabelField(debugRect, "Type: " + av.asset.GetType());
                debugRect.x += debugRect.width;
                EditorGUI.LabelField(debugRect, "Parent Type: " + (parent ? parent.GetType() : null));
            }

            int childCount;
            if (targets.Length == 1)
                childCount = avs[0].data.children.Count;
            else
                childCount = 0; // Hides the children if multiple targets are selected

            float slh = EditorGUIUtility.singleLineHeight;
            float totalLineHeight = slh + EditorGUIUtility.standardVerticalSpacing;

            var rect = EditorGUILayout.GetControlRect(false);

            var rvbRect = rect;
            rvbRect.width = S.S.rawViewButtonWidth;
            rvbRect.x += rect.width - rvbRect.width;
            #region Raw View Button
            {
                if (rawViewButtonStyle == null)
                    rawViewButtonStyle = new GUIStyle("EditModeSingleButton");
                rawViewButtonLabel.text = S.S.rawViewButtonLabel;

                bool prevGUIEnabled = GUI.enabled;
                GUI.enabled = true;
                EditorGUI.BeginChangeCheck();
                var _ = GUI.Toggle(rvbRect, RawViewWindow != null, rawViewButtonLabel, rawViewButtonStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    if (RawViewWindow != null)
                    {
                        CloseRawViewWindow();
                    }
                    else
                    {
                        Editor e;
                        if (editor.TryGetTarget(out e))
                        {
                            RawViewWindow.setup = (w) =>
                            {
                                avTargets.rvUndrawnPathsDirty = true;
                                w.avTargets = avTargets;
                                w.serializedObject = e.serializedObject;
                                w.serializedProperty = null;
                                w.editor = e;
                                w.autoClose = false;
                                w.autoHeight = false;
                                if (setupRawViewWindow != null)
                                    setupRawViewWindow(w);
                            };
                            RawViewWindow = EditorWindow.CreateWindow<RawViewWindow>("Raw View");
                            RawViewWindow.setup = null;
                        }
                        else
                            Debug.LogError("?");
                    }
                }
                GUI.enabled = prevGUIEnabled;
            }
            #endregion
            rect.width -= rvbRect.width + S.S.rawViewButtonSpacing;

            if (childCount > 0)
            {
                #region Children Foldout
                Rect childrenRect = rect;
                childrenRect.width = S.S.childrenFoldoutWidth;
                childrenRect.x += rect.width - childrenRect.width;

                var prevHM = EditorGUIUtility.hierarchyMode;
                EditorGUIUtility.hierarchyMode = true;
                EditorGUI.BeginChangeCheck();
                expandChildren = EditorGUI.Foldout(childrenRect, expandChildren, childrenLabel, true);
                if (EditorGUI.EndChangeCheck())
                    EditorProjectPrefs.SetBool("AVHeader.expandChildren", expandChildren);
                EditorGUIUtility.hierarchyMode = prevHM;

                childrenRect = rect;
                childrenRect.y += totalLineHeight;

                if (expandChildren)
                {
                    EditorGUI.indentLevel++;

                    float spacing = 0;

                    var prevGUIEnabled = GUI.enabled;

                    float rcbw = S.S.removeChildButtonWidth;

                    var av = avs[0];
                    for (int i = 0; i < av.data.children.Count; i++)
                    {
                        var r = childrenRect;

                        var childAsset = GetPicker(av.data.children[i].Load());
                        r.width -= rcbw;
                        GUI.enabled = false;
                        EditorGUI.ObjectField(r, childAsset, typeof(Object), false);

                        r.x += r.width;
                        r.width = rcbw;
                        GUI.enabled = true;
                        if (GUI.Button(r, "X"))
                            av.TryRemoveChild(av.data.children[i]);

                        childrenRect.y += totalLineHeight;
                        spacing += totalLineHeight;
                    }

                    GUI.enabled = prevGUIEnabled;

                    GUILayout.Space(spacing);

                    EditorGUI.indentLevel--;
                }
                #endregion

                rect.width -= S.S.childrenFoldoutWidth + 20; // (The 20 is for the foldout arrow)
            }

            #region Parent Selection Field
            Type parentType;
            if (parentFilterType != null)
            {
                parentType = parentFilterType;
            }
            else
            {
                var mainTarget = targets[mainTargetID];
                Type childType = mainTarget.GetType();
                if (!parentFilterTypes.TryGetValue(childType, out parentType))
                {
                    parentType = childType;
                    if (Helper.TypeIsImporter(parentType))
                    {
                        var parent = AssetDatabase.LoadMainAssetAtPath(mainTarget.Path());
                        if (parent != null)
                            parentType = parent.GetType();
                        //else
                        //    Debug.LogWarning("\n");
                    }
                }
            }

            bool prevShowMixed = EditorGUI.showMixedValue;
            if (!sameParent)
            {
                EditorGUI.showMixedValue = true;
                var helpRect = EditorGUI.IndentedRect(rect);
                EditorGUI.HelpBox(helpRect, "Parent mismatch", MessageType.Warning);
                float w = Mathf.Max(S.S.parentLabelWidth, 105);
                rect.x += w;
                rect.width -= w;
            }
            else if (childCount == 0)
            {
                var parentLabelRect = rect;
                parentLabelRect.width = S.S.parentLabelWidth;
                EditorGUI.LabelField(parentLabelRect, parentLabel);
                rect.x += parentLabelRect.width;
                rect.width -= parentLabelRect.width;
            }
            EditorGUI.BeginChangeCheck();
            Object newParent = EditorGUI.ObjectField(rect, GUIContent.none, parentPicker, parentType, false);
            EditorGUI.showMixedValue = prevShowMixed;
            if (EditorGUI.EndChangeCheck())
            {
                bool allow = true;
                for (int i = 0; i < allowParentAssignment.Count; i++)
                {
                    if (!allowParentAssignment[i](newParent))
                    {
                        allow = false;
                        break;
                    }
                }
                if (allow)
                {
                    string newParentPath = newParent.Path();
                    for (int i = 0; i < avs.Length; i++)
                    {
                        Object typeMatchedParent = null;

                        if (newParent != null)
                        {
                            Type idealParentType;
                            Object oldParent = avs[i].LoadParentAsset();
                            if (oldParent != null)
                                idealParentType = oldParent.GetType();
                            else
                                idealParentType = avs[i].asset.GetType();

                            if (Helper.TypeIsImporter(idealParentType))
                                typeMatchedParent = AssetImporter.GetAtPath(newParentPath);
                            else if (newParent.GetType() != idealParentType)
                                typeMatchedParent = AssetDatabase.LoadAssetAtPath(newParentPath, idealParentType);
                            if (typeMatchedParent == null)
                                typeMatchedParent = newParent;

                            if (S.S.debugTypes)
                            {
                                Debug.Log("Ideal Parent Type: " + idealParentType + Helper.LOG_END);
                                Debug.Log("Actual Parent Type: " + (typeMatchedParent ? typeMatchedParent.GetType() : null) + Helper.LOG_END);
                            }
                        }

                        avs[i].ChangeParent(AssetID.Get(typeMatchedParent));
                    }

                    InitTargets();
                }
                else
                {
                    Debug.LogError("AllowParentAssignment(newParent) returned false for attempted assignment of parent to: " + newParent.name + "\nParent was not changed." + Helper.LOG_END);
                }
            }
            #endregion

            #region Clearing Parentless Overrides Button
            bool hasParentlessOverrides = false;
            for (int i = 0; i < avs.Length; i++)
            {
                if (avs[i].data.HasParentlessOverrides)
                {
                    hasParentlessOverrides = true;
                    break;
                }
            }
            if (hasParentlessOverrides)
            {
                GUILayout.Space(5);

                GUIContent content = new GUIContent("Clear Parentless Overrides", "If a currently selected asset has no parent, this will clear the property overrides in its AssetImporter's userData");
                var buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                if (GUI.Button(buttonRect, content))
                {
                    for (int i = 0; i < avs.Length; i++)
                    {
                        if (avs[i].data.HasParentlessOverrides)
                        {
                            avs[i].data.overrides.Clear();
                            avs[i].SaveUserDataWithUndo();
                            avs[i].UpdateOverridesCache();
                        }
                    }

                    Debug.Log("Cleared property overrides of selected parentless assets." + Helper.LOG_END);
                }
            }
            #endregion
        }

        public Action<RawViewWindow> setupRawViewWindow;

        private int mainTargetID;
        private Object parentPicker;
        private bool sameParent = false;
        public bool AnyHasParent => !sameParent || parentPicker != null;
        private Object GetPicker(Object parent)
        {
            // Remaps an AssetImporter to the main asset so that the parent picker / children foldout
            // shows the name of the asset (an AssetImporter ObjectField will have a blank name).
            if (parent != null && Helper.TypeIsImporter(parent.GetType()))
                parent = AssetDatabase.LoadMainAssetAtPath(parent.Path());
            return parent;
        }
        private void InitTargets(bool first = false)
        {
            bool prevAnyHasParent = AnyHasParent;

            //TODO: Why? should all the AVs forget their parents?
            ////////avTargets.parents = null;

            mainTargetID = 0;

            var avs = avTargets.AVs;

            if (targets.Length == 1)
            {
                parentPicker = GetPicker(avs[0].LoadParentAsset());
                sameParent = true;
            }
            else
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == Selection.activeObject)
                        mainTargetID = i;
                }
                parentPicker = avs[mainTargetID].LoadParentAsset();

                sameParent = true;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (avs[i].LoadParentAsset() != parentPicker)
                        sameParent = false;
                }

                parentPicker = GetPicker(parentPicker);
            }

            if (!first && prevAnyHasParent != AnyHasParent)
            {
                avTargets.UpdateAnyHasParent();

                //TODO: only if UI Toolkit or something???
                AVCommonHelper.RefreshInspectors();
            }
        }
        public static void InitAllTargets()
        {
            // TODO3: maybe also OnDisable()+OnEnable()?
            foreach (var v in all)
            {
                // TODO: why would v be null?
                if (v != null && v.avTargets != null && v.avTargets.AVs != null)
                    v.InitTargets();
            }
        }
    }
}

#endif
