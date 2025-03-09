#if DEBUG_ASSET_VARIANTS
#define DEBUG_CREATEINSPECTORGUI_DURATION
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using Object = UnityEngine.Object;

namespace EditorReplacement
{
    public class SpacingFreeWrapperEditor : WrapperEditor
    {
        protected override float PreChildrenSpacing => 0;
        protected override float PostChildrenSpacing => 0;
    }

    [InitializeOnLoad]
    public class WrapperEditor : Editor, IReplacementEditor //abstract
    {
        static WrapperEditor()
        {
            Type decalProjector = AVCommonHelper.FindType("UnityEngine.Rendering.HighDefinition.DecalProjector"); // (This draws a MaterialEditor, which is broken, therefore this is broken as well).
            if (decalProjector != null)
            {
                brokenUITTargetTypes.Add(decalProjector);
                onlyIMGUITargetTypes.Add(decalProjector);
                ERManager.needsWrapperInspectedTypes.Add(decalProjector);
            }
        }

        /// <summary>
        /// Broken for UI Toolkit
        /// </summary>
        public static List<Type> brokenUITTargetTypes = new List<Type>() { typeof(Material) };
        public static List<Type> onlyIMGUITargetTypes = new List<Type>() { typeof(Material), typeof(ParticleSystem) };
        public virtual bool OnlyIMGUI => ERSettings.S.defaultOnlyIMGUI || onlyIMGUITargetTypes.Contains(target.GetType());

        private IReplacementEditor rootEditor = null;
        IReplacementEditor IReplacementBase<IReplacementEditor>.Root { get { return rootEditor; } set { rootEditor = value; } }
        public IReplacementEditor RootEditor => rootEditor;

        public bool IsRoot => ReferenceEquals(this, rootEditor);

        private int editorDepth = -1;
        int IReplacementBase<IReplacementEditor>.Depth { get { return editorDepth; } set { editorDepth = value; } }
        public int EditorDepth => editorDepth;

        private int editorIndex = 0;
        int IReplacementBase<IReplacementEditor>.Index { get { return editorIndex; } set { editorIndex = value; } }
        public int EditorIndex => editorIndex;

        private IReplacementEditor parentEditor = null;
        IReplacementEditor IReplacementBase<IReplacementEditor>.Parent { get { return parentEditor; } set { parentEditor = value; } }
        public IReplacementEditor ParentEditor => parentEditor;

        /// <summary>
        /// Do not modify this.
        /// This will be zero in Count if the DrawDefaultInspector() should be called
        /// </summary>
        public List<Editor> ChildrenEditors { get; } = new List<Editor>();

        List<AssetEditor> IReplacementEditor.AssetEditors { get; } = new List<AssetEditor>();

        void IReplacementEditor.ProcessTree(IReplacementEditor root, EditorTreeDef treeDef)
        {
            ProcessEditorTree(root, treeDef);
        }
        protected virtual void ProcessEditorTree(IReplacementEditor root, EditorTreeDef treeDef) { }

        void IReplacementEditor.OnCreatedTree(IReplacementEditor root, EditorTreeDef treeDef)
        {
            OnCreatedEditorTree(root, treeDef);
        }
        protected virtual void OnCreatedEditorTree(IReplacementEditor root, EditorTreeDef treeDef) { }

        public virtual IEnumerable<Editor> EnumerateEditors(Object[] targetsFilter = null)
        {
            if (targetsFilter == null || AVCommonHelper.TargetsAreEqual(targetsFilter, this))
                yield return this;

            for (int i = 0; i < ChildrenEditors.Count; i++)
            {
                foreach (Editor e in ERHelper.EnumerateEditors(ChildrenEditors[i], targetsFilter))
                    yield return e;
            }
        }

        public bool HasChildrenEditors => ChildrenEditors.Count > 0;
        public Editor FirstChildEditor => ChildrenEditors[0];

        protected bool IsEditable { get; set; }
        /// <summary>
        /// You can use this to prevent Helper.IsEditable() from being called when unnecessary, for example when a wrapper editor is not affected by GUI.enabled (i.e. if it's only cosmetic).
        /// </summary>
        protected virtual bool EditorAlwaysEditable => false;
        protected virtual bool EditorNeverEditable => false;
        /// <summary>
        /// You can also override this to true if you know that the asset this editor gets created for (by an EditorReplacer) is always editable.
        /// You should probably return true in such a case as this:
        /// public override ChildAction OnAssignedTargets([...])
        /// {
        ///     if (!ERHelper.IsEditable(node.Targets[0]))
        ///         return ChildAction.RemoveReparent;
        /// }
        /// </summary>
        protected virtual bool AssetAlwaysEditable => false;

        protected virtual void OnEnable()
        {
            callOnDisable = true;

            if (target == null) // Can occur if code recompiles
                return;

            ERManager.CreateTree(this);

            if (AssetAlwaysEditable)
            {
                IsEditable = true;
            }
            else
            {
                bool consistent = EditorNeverEditable || EditorAlwaysEditable;
                if (consistent && ChildrenEditors.Count != 0)
                {
                    bool allWrappers = true;
                    for (int i = 0; i < ChildrenEditors.Count; i++)
                    {
                        if (!(ChildrenEditors[i] is WrapperEditor))
                        {
                            allWrappers = false;
                            break;
                        }
                    }
                    if (allWrappers)
                        IsEditable = !EditorNeverEditable;
                    else
                        IsEditable = AVCommonHelper.IsEditable(target);
                }
                else
                {
                    IsEditable = AVCommonHelper.IsEditable(target);
                }
            }
        }
        private bool callOnDisable = false;
        protected virtual void OnDisable()
        {
            callOnDisable = false;

#if UNITY_2019_1_OR_NEWER
            Container = null;
#endif

            ERManager.DestroyTree(this, false);
        }
        protected virtual void OnDestroy()
        {
            if (callOnDisable)
            {
                OnDisable();
            }
            else
            {
                if (ChildrenEditors.Count > 0)
                {
                    // Turns out this is possible when deleting an asset. I guess Unity doesn't call OnDisable() for an editor when deleting its asset.
                    ERManager.DestroyTree(this, false);
                }
            }
        }

        /// <summary>
        /// Spacing before and after drawing a child editor
        /// </summary>
        protected virtual float PreChildrenSpacing => ERSettings.S.defaultPreChildrenSpacing;
        protected virtual float PostChildrenSpacing => ERSettings.S.defaultPostChildrenSpacing;

        protected virtual void ChildrenOnInspectorGUI()
        {
            Profiler.BeginSample("ChildrenOnInspectorGUI");
            if (ChildrenEditors.Count == 0)
            {
                GUILayout.Space(PreChildrenSpacing);
                DrawDefaultInspector();
                GUILayout.Space(PostChildrenSpacing);
            }
            else
            {
                for (int i = 0; i < ChildrenEditors.Count; i++)
                {
                    GUILayout.Space(PreChildrenSpacing);
                    ChildrenEditors[i].ContextualOnInspectorGUI();
                    GUILayout.Space(PostChildrenSpacing);
                }
            }
            Profiler.EndSample();
        }
        /// <summary>
        /// This is used instead of OnInspectorGUI() so that OnInspectorGUI() itself can consistently apply e.g. IsEditable to GUI.enabled around all the GUI.
        /// Just make sure you call either base.DoOnInspectorGUI() or ChildrenOnInspectorGUI() instead of base.OnInspectorGUI(), or you'll probably end up with an infinite loop.
        /// </summary>
        protected virtual void DoOnInspectorGUI()
        {
            ChildrenOnInspectorGUI();
        }
        public sealed override void OnInspectorGUI()
        {
            var prevGUIEnabled = GUI.enabled;
            GUI.enabled = prevGUIEnabled && IsEditable;

            // Because UnityEditor.EditorGUI:DoObjectField()->UnityEditor.EditorGUI:DrawObjectFieldLargeThumb()->UnityEditor.AssetPreview:GetAssetPreview()->...->MonoCreateAssetPreview()->...
            using (new EditorCreationScope(original: true)) // Also, this scope needs to exists because UnityEditor.Rendering.HighDefinition.DecalProjectorEditor does "m_MaterialEditor = (MaterialEditor)CreateEditor(materials);" in OnInspectorGUI()
            {
                DoOnInspectorGUI();
            }

            GUI.enabled = prevGUIEnabled;
        }

#if UNITY_2019_1_OR_NEWER
        protected virtual void ChildrenCreateInspectorGUI(VisualElement container)
        {
            if (ChildrenEditors.Count == 0)
            {
                var gui = new VisualElement()
                {
                    name = "ContextualFillDefaultInspector-Self"
                };
                UIToolkitHelper.ContextualFillDefaultInspector(gui, this);
                gui.style.marginTop = PreChildrenSpacing;
                gui.style.marginBottom = PostChildrenSpacing;
                container.Add(gui);
            }
            else
            {
                for (int i = 0; i < ChildrenEditors.Count; i++)
                {
                    var childEditor = ChildrenEditors[i];

                    if (!brokenUITTargetTypes.Contains(childEditor.target.GetType()))
                    {
                        var childGUI = new WrapperInspectorElement(childEditor);
                        childGUI.style.marginTop = PreChildrenSpacing;
                        childGUI.style.marginBottom = PostChildrenSpacing;
                        container.Add(childGUI);
                    }
                }
            }
        }
        protected virtual void FillInspectorGUI(VisualElement container)
        {
            ChildrenCreateInspectorGUI(container);
        }
        public VisualElement Container { get; private set; } // (EditorContainer.Container)

        public sealed override VisualElement CreateInspectorGUI()
        {
#if DEBUG_CREATEINSPECTORGUI_DURATION
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            //return null;
            if (OnlyIMGUI)
                return null;

            VisualElement container = new VisualElement()
            {
                name = "WrapperEditor:" + GetType().Name
            };
            if (Container == null)
                Container = container;

            // Leaves it open as the current
            new PropertyHandlerScope(ERHelper.uieProxyPropertyHandlerCache);

            //using (new PropertyHandlerScope(this)) // It seemed not even the outermost Editor gets its cache set as default by Unity, in the way that OnInspectorGUI does.
            //{
            FillInspectorGUI(container);
            //}

            container.RegisterCallback<AttachToPanelEvent>((e) =>
            {
                if (container.enabledInHierarchy) // Otherwise opacity stacks for color wraps
                    container.SetEnabled(IsEditable);
            });

#if DEBUG_CREATEINSPECTORGUI_DURATION
            Debug.Log("CreateInspectorGUI() duration: " + sw.Elapsed.TotalMilliseconds + "\n");
#endif

            return container;
        }

        public void RebuildGUI()
        {
            if (Container != null)
            {
                Container.Clear();

                using (new PropertyHandlerScope(this))
                {
                    FillInspectorGUI(Container);
                }

                Container.Bind(serializedObject);
            }
        }
#endif

        protected virtual bool IsMinimalEditor => EditorIndex != 0; // TODO2: allow a custom index?
        
        private static readonly Type[] zeroTypes = new Type[0];

        protected void UpdateChildrenMethodCache(ref MethodInfo[] methodInfos, string methodName)
        {
            int c = ChildrenEditors.Count;
            if (methodInfos == null || methodInfos.Length != c)
            {
                methodInfos = new MethodInfo[c];
                for (int i = 0; i < c; i++)
                {
                    const BindingFlags pnpi = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    for (Type type = ChildrenEditors[i].GetType(); type != null; type = type.BaseType)
                    {
                        //TODO: possibly just check the target type if it's a component, as scriptableobjects would implement it using delegates
                        var mi = type.GetMethod(methodName, pnpi, null, zeroTypes, null);
                        if (mi != null)
                        {
                            methodInfos[i] = mi;
                            break;
                        }
                    }
                }
            }
        }
        protected void CallChildrenMethodCache(MethodInfo[] methodInfos, object[] args = null)
        {
            for (int i = 0; i < ChildrenEditors.Count; i++)
                if (methodInfos[i] != null)
                    methodInfos[i].Invoke(ChildrenEditors[i], args);
        }
        protected IEnumerable<object> ReturnCallChildrenMethodCache(MethodInfo[] methodInfos, object[] args = null)
        {
            for (int i = 0; i < ChildrenEditors.Count; i++)
            {
                if (methodInfos[i] != null)
                    yield return methodInfos[i].Invoke(ChildrenEditors[i], args);
                //else
                //yield return null; //?
            }
        }

        //TODO: possibly enforce IsMinimalEditor skipping on ScriptableObjects' OnSceneGUI(SceneView sceneView) subscribing?
        //I would go through the editors, and unsubscribe them if they're not meant to draw

        public MethodInfo[] childrenOnSceneGUIs = null;
        public virtual void OnSceneGUI()
        {
            if (IsMinimalEditor)
                return;

            UpdateChildrenMethodCache(ref childrenOnSceneGUIs, "OnSceneGUI");
            CallChildrenMethodCache(childrenOnSceneGUIs);
        }

        public MethodInfo[] childrenHasFrameBounds = null;
        public virtual bool HasFrameBounds()
        {
            if (IsMinimalEditor)
                return false;

            UpdateChildrenMethodCache(ref childrenHasFrameBounds, "HasFrameBounds");
            foreach (var ret in ReturnCallChildrenMethodCache(childrenHasFrameBounds))
            {
                if (ret != null && (bool)ret)
                    return true;
            }

            return false;
        }

        public MethodInfo[] childrenOnGetFrameBounds = null;
        public virtual Bounds OnGetFrameBounds()
        {
            if (IsMinimalEditor)
            {
                Debug.LogError("HasFrameBounds() should have returned false.");
                return default(Bounds);
            }

            int i = 0;
            Bounds bounds = default(Bounds);

            UpdateChildrenMethodCache(ref childrenOnGetFrameBounds, "OnGetFrameBounds");
            foreach (var ret in ReturnCallChildrenMethodCache(childrenOnGetFrameBounds))
            {
                if (ret != null)
                {
                    if (i == 0)
                        bounds = (Bounds)ret;
                    else
                        bounds.Encapsulate((Bounds)ret);
                    i++;
                }
            }

            return bounds;
        }

        #region Remapping Preview
        public override bool HasPreviewGUI()
        {
            // (BTW for ModelImporter (maybe similar for others as well) it will do the model's preview if the actual editor's HasPreviewGUI() returns false.)

            // Preview calls don't check for IsMinimalEditor because it will stop after finding any with HasPreviewGUI()=true.
            // TODO2: is that good? That a minimal editor is allowed to have a preview?
            //if (IsMinimalEditor)
            //    return false;

            for (int i = 0; i < ChildrenEditors.Count; i++)
                if (ChildrenEditors[i].HasPreviewGUI())
                    return true;

            return base.HasPreviewGUI();
        }

        public override GUIContent GetPreviewTitle()
        {
            //if (IsMinimalEditor)
            //    return GUIContent.none;

            for (int i = 0; i < ChildrenEditors.Count; i++)
                if (ChildrenEditors[i].HasPreviewGUI())
                    return ChildrenEditors[i].GetPreviewTitle();

            return base.GetPreviewTitle();
        }

        public override string GetInfoString()
        {
            //if (IsMinimalEditor)
            //    return "";

            for (int i = 0; i < ChildrenEditors.Count; i++)
                if (ChildrenEditors[i].HasPreviewGUI())
                    return ChildrenEditors[i].GetInfoString();

            return base.GetInfoString();
        }

        public override void OnPreviewSettings()
        {
            //if (IsMinimalEditor)
            //    return;

            for (int i = 0; i < ChildrenEditors.Count; i++)
            {
                if (ChildrenEditors[i].HasPreviewGUI())
                {
                    ChildrenEditors[i].OnPreviewSettings();
                    return;
                }
            }

            base.OnPreviewSettings();
        }

        public override void DrawPreview(Rect previewArea)
        {
            //if (IsMinimalEditor)
            //    return;

            using (new EditorCreationScope(true)) //?
            {
                for (int i = 0; i < ChildrenEditors.Count; i++)
                {
                    if (ChildrenEditors[i].HasPreviewGUI())
                    {
                        ChildrenEditors[i].DrawPreview(previewArea);
                        return;
                    }
                }
            }

            base.DrawPreview(previewArea);
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            //if (IsMinimalEditor)
            //    return null;

            for (int i = 0; i < ChildrenEditors.Count; i++)
                if (ChildrenEditors[i].HasPreviewGUI())
                    return ChildrenEditors[i].RenderStaticPreview(assetPath, subAssets, width, height);

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        public override void ReloadPreviewInstances()
        {
            //if (IsMinimalEditor)
            //    return;

            for (int i = 0; i < ChildrenEditors.Count; i++)
            {
                if (ChildrenEditors[i].HasPreviewGUI())
                {
                    ChildrenEditors[i].ReloadPreviewInstances();
                    return;
                }
            }

            base.ReloadPreviewInstances();
        }
        #endregion

        private static readonly MethodInfo onHeaderGUIMI = typeof(Editor).GetMethod("OnHeaderGUI", R.npi);
        protected override void OnHeaderGUI()
        {
            if (IsMinimalEditor)
                return;

            if (ERManager.headerFlush != null)
            {
                ERManager.headerFlush();
                ERManager.headerFlush = null;
            }

            //TODO::
            if (EditorGUIUtility.hierarchyMode) // For now at least, does not support DrawHeaderFromInsideHierarchy();
                return;

            using (new EditorCreationScope(true))
            {
                if (!HasChildrenEditors)
                {
                    base.OnHeaderGUI();
                }
                else
                {
                    for (int i = 0; i < ChildrenEditors.Count; i++)
                    {
                        var ce = ChildrenEditors[i];
                        var we = ce as WrapperEditor;
                        if (we == null || !we.IsMinimalEditor) // (Might as well)
                            onHeaderGUIMI.Invoke(ce, null); // Doesn't use DrawHeader in order to avoid multiple calls to Editor.finishedDefaultHeaderGUI, which is necessary to avoid duplicate Addressable toggles.
                    }
                }
            }
        }

#if UNITY_2017_3_OR_NEWER
        protected override bool ShouldHideOpenButton()
        {
            if (IsMinimalEditor)
                return false;

            if (!HasChildrenEditors)
                return base.ShouldHideOpenButton();
            return (bool)FirstChildEditor.GetType().GetMethod("ShouldHideOpenButton", R.npi).Invoke(FirstChildEditor, null);
            //TODO2: first that IsMinimalEditor=false in case add custom index?
        }
#endif

        public override bool RequiresConstantRepaint()
        {
            if (IsMinimalEditor)
                return false;

            for (int i = 0; i < ChildrenEditors.Count; i++)
                if (ChildrenEditors[i].RequiresConstantRepaint())
                    return true;

            return false;
        }

        public override bool UseDefaultMargins()
        {
            if (ERSettings.S.neverUseDefaultMargins)
                return false;

            if (IsMinimalEditor)
                return false;

            if (!HasChildrenEditors)
                return base.UseDefaultMargins();
            else
                return FirstChildEditor.UseDefaultMargins();
            //TODO2:
        }
    }
}
#endif
