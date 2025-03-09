#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using PrecisionCats;

namespace EditorReplacement
{
    public interface IHavePropertyHandlerCache
    {
        object PropertyHandlerCache { get; }
    }

    public class WrapperInspectorElement : InspectorElement, IHavePropertyHandlerCache
    {
        public Editor Editor { get; private set; }
        public object PropertyHandlerCache => ERHelper.GetEditorPropertyHandlerCache(Editor);

        private static readonly PropertyInfo modePI = typeof(InspectorElement).GetProperty("mode", R.npi);

        private static readonly FieldInfo bindObjectFI = AVCommonHelper.FindType("UnityEditor.UIElements.SerializedObjectBindEvent").GetField("m_BindObject", R.npi);

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            bool isBinding = evt.GetType().Name == "SerializedObjectBindEvent";
            if (isBinding)
            {
                //int mode = (1 << 3) | (1 << 0); // (UIElements only)
                int mode = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3);
                modePI.SetValue(this, mode);

                var bindSO = (SerializedObject)bindObjectFI.GetValue(evt);
                Editor = (Editor)typeof(InspectorElement).GetField("m_Editor", R.npi).GetValue(this);
                var selfSO = Editor.serializedObject; //if (ERHelper.TargetsAreEqual(selfSO.targetObjects, bindSO))
                if (bindSO != selfSO)
                {
                    //Debug.Log("STOPPED A BINDING ATTEMPT");
                    evt.StopImmediatePropagation();
                    return;
                }

                //Debug.Log("ACTUALLY BINDING");
            }

            base.ExecuteDefaultActionAtTarget(evt);

            RemoveFromClassList(uIEInspectorVariantUssClassName);
            //style.paddingLeft = 0;
            //style.paddingRight = 0;
            //style.paddingTop = 0;
            //style.paddingBottom = 0;

            if (isBinding)
                WrapChildrenIMGUI(PossiblyPrefab(Editor));
        }

        private void WrapChildrenIMGUI(bool possiblyPrefab)
        {
            for (int i = 0; i < childCount; i++)
            {
                var imguiContainer = this[i] as IMGUIContainer;
                if (imguiContainer != null)
                {
                    WrapIMGUI(imguiContainer, possiblyPrefab);
                    break;
                }
            }
        }

        public static int foldoutTriangleWidth = 18; //TODO: does PropertyField's IMGUIContainer need this as well? Should this be baked into leftward?

        public class IMGUIWrapper : IMGUIContainerWrapper
        {
            public LeftwardIMGUIWrapper leftward;

            private void DoDraw()
            {
                DrawNext();

                if (GUI.changed) // Not quite "EndChangeCheck" but whatever
                {
                    using (var e = EditorEndChangeCheckEvent.GetPooled())
                    {
                        var imgui = Wrapping.IMGUIContainer;
                        e.target = imgui;
                        imgui.SendEvent(e);
                    }
                }
            }

            private static readonly FieldInfo currentFI = typeof(EditorStyles).GetField("s_Current", R.nps);
            private static readonly FieldInfo inspectorDefaultMarginsFI = typeof(EditorStyles).GetField("m_InspectorDefaultMargins", R.npi);
            public override void Draw()
            {
                if (leftward.incorrectPadding == null)
                {
                    DoDraw();
                    return;
                }

                //TODO: why is this box not aligned on the right side?
                //EditorGUI.DrawRect(EditorGUILayout.GetControlRect(), Color.red);
                //    DrawNext();
                //return;

                var originalIDM = EditorStyles.inspectorDefaultMargins;
                var current = currentFI.GetValue(null);

                if (leftward.incorrectPaddingStyle == null)
                    leftward.incorrectPaddingStyle = new GUIStyle();
                if (leftward.incorrectPaddingStyle.padding != leftward.incorrectPadding)
                    leftward.incorrectPaddingStyle.padding = leftward.incorrectPadding;

                inspectorDefaultMarginsFI.SetValue(current, leftward.incorrectPaddingStyle);

                try
                {
                    DoDraw();
                }
                finally
                {
                    inspectorDefaultMarginsFI.SetValue(current, originalIDM);
                }
            }
        }

        private static readonly MethodInfo canBeExpandedViaAFoldoutWithoutUpdateMI = typeof(Editor).GetMethod("CanBeExpandedViaAFoldoutWithoutUpdate", R.npi);
        private void WrapIMGUI(IMGUIContainer imgui, bool possiblyPrefab)
        {
            if (possiblyPrefab && !ERSettings.S.alignPrefabBars)
                possiblyPrefab = false;

            LeftwardIMGUIWrapper l = IMGUIContainerWrapping.GetWrapper<LeftwardIMGUIWrapper>(imgui);

            //Action update = () =>
            //{
            if (!float.IsNaN(worldBound.xMin))
            {
                if (possiblyPrefab)
                {
                    // Pushes the rect leftward to align with the inspector window's edge, which is necessary for prefab bars
                    int dist = Mathf.RoundToInt(worldBound.xMin) - 1; // I believe the 1 is becaus the bars are 2 pixels wide, so that the center is to the right by 1 pixel. //TODO2: should it always?;
#if UNITY_2021_2_OR_NEWER //TODO:: find out why Unity is culling the bars otherwise
                    dist--;
#endif
                    l.maxDist = dist;
                    l.Dist = dist;
                }
                else
                {
                    l.maxDist = 1000000; //?
                    l.Expand(foldoutTriangleWidth);
                }

                if (Editor.UseDefaultMargins() && (bool)canBeExpandedViaAFoldoutWithoutUpdateMI.Invoke(Editor, null))
                {
                    int left = Mathf.Min(l.Dist, foldoutTriangleWidth);
                    l.SetIncorrect(left, 2, 2, 0); // (Minimum is forced to be 2 by Unity) //TODO::
                }
                else
                    l.RemoveIncorrect();
            }
            //};
            //update();
            //TO//DO: ensure no duplicates!!!
            //imgui.RegisterCallback<GeometryChangedEvent>((e) => { update(); });

            var w = IMGUIContainerWrapping.GetWrapper<IMGUIWrapper>(imgui);
            w.leftward = l;
            w.Wrapping.Apply();
        }

        private void AlignPrefabBars()
        {
            var prefabBars = this.Q("unity-prefab-override-bars-container");
            if (prefabBars != null)
                prefabBars.style.left = -worldBound.x + 2; // TODO2: Improve!
        }

        private bool PossiblyPrefab(Editor editor)
        {
            return editor.target is Component;
        }

        //private static readonly Type editorElementType = AVCommonHelper.FindType("UnityEditor.UIElements.EditorElement");
        public WrapperInspectorElement(Editor editor) : base(editor)
        {
            Editor = editor;

            bool scrollFixed = false;
            RegisterCallback<AttachToPanelEvent>((e) =>
            {
                if (scrollFixed)
                    return;
                scrollFixed = true;

                // This is needed because otherwise if the editor is not tall enough to warrant a vertical scrollbar,
                // then it's given a horizontal one (at least in 2021.3.2f1), due to the mismatch in IMGUI's default margins and UIElements' default margins, and the fact that I counteract IMGUI's default margins, thereby increasing the bounding box.
                // TODO: is there a better solution?
                var p = parent;
                while (p != null)
                {
                    //if (p.GetType() == editorElementType)
                    //{
                    //    // Delayed otherwise it creates the error "Assertion failed" in UnityEngine.UIElements.UIR.Implementation.RenderEvents:DepthFirstOnVisualsChanged
                    //    var ee = p;
                    //    ee.panel.visualTree.schedule.Execute(() =>
                    //    {
                    //        //    ee.style.overflow = Overflow.Hidden;
                    //        // Does it to the parent because otherwise it creates the error "Assertion failed" in UnityEngine.UIElements.UIR.Implementation.RenderEvents:DepthFirstOnVisualsChanged
                    //        ee.parent.style.overflow = Overflow.Hidden;
                    //    });
                    //}

                    if (p is ScrollView)
                    {
                        var sv = (ScrollView)p;
                        //sv.contentContainer.style.overflow = Overflow.Hidden;

                        //// Temporarily disable the horizontal scrollbar while we wait for the EditorElement to change its overflow
#if UNITY_2021_1_OR_NEWER
                        var originalVisibility = sv.horizontalScrollerVisibility;
                        sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
#else
                        //TODO:::?
                        //var originalVisibility = sv.showHorizontal;
                        //sv.showHorizontal = false;
#endif
                        //p.panel.visualTree.schedule.Execute(() =>
                        //{
                        //    sv.contentContainer[0].style.overflow = Overflow.Hidden;
                        //    sv.horizontalScrollerVisibility = originalVisibility; //ScrollerVisibility.Auto
                        //});
                        // Ok then, there is no solution that prevents those errors if you have a physicsmaterial selected. Great.
                        // at least without entirely disabling horizontal scrollbar, which seems like a bad idea.
                        //TODO: revisit

                        break;
                    }

                    p = p.parent;
                }
            });

            RegisterCallback<GeometryChangedEvent>((e) =>
            {
                WrapChildrenIMGUI(PossiblyPrefab(editor));
                AlignPrefabBars();
            });
        }
    }
}
#endif
