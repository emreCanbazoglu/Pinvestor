#if UNITY_2019_1_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PrecisionCats
{
#if UNITY_EDITOR
    public class LeftwardIMGUIWrapper : IMGUIContainerWrapper
    {
        private int dist = 0;
        public virtual int Dist
        {
            get => dist;
            set
            {
                value = Mathf.Min(value, maxDist);
                if (dist != value)
                {
                    dist = value;
                    UpdateMargins();
                    Wrapping.Apply();
                }
            }
        }
        public int maxDist = 1000000;

        public void UpdateMargins()
        {
            var c = Wrapping.IMGUIContainer;
            if (incorrectPadding != null)
            {
                if (incorrectPadding.left > dist)
                {
                    leftSpacing = 0;
                    c.style.marginLeft = -incorrectPadding.left;
                }
                else
                {
                    leftSpacing = dist - incorrectPadding.left;
                    c.style.marginLeft = -dist;
                }
                c.style.marginRight = -incorrectPadding.right;
                c.style.marginTop = -incorrectPadding.top;
                c.style.marginBottom = -incorrectPadding.bottom;
                modifiedRTB = true;
            }
            else
            {
                leftSpacing = dist;
                c.style.marginLeft = -dist;
                if (modifiedRTB)
                {
                    modifiedRTB = false;
                    c.style.marginRight = 0;
                    c.style.marginTop = 0;
                    c.style.marginBottom = 0;
                }
            }
        }
        private bool modifiedRTB = false;

        private float leftSpacing = 0;

        public GUIStyle incorrectPaddingStyle = null;
        public RectOffset incorrectPadding = null;
        /// <summary>
        /// L, R, T, and possibly B all need to be at least 2, because Unity forces it to be.
        /// </summary>
        public void SetIncorrect(int l, int r, int t, int b)
        {
            bool dirty = false;

            if (incorrectPadding == null)
                incorrectPadding = new RectOffset();

            if (incorrectPadding.left != l)
            {
                incorrectPadding.left = l;
                dirty = true;
            }
            if (incorrectPadding.right != r)
            {
                incorrectPadding.right = r;
                dirty = true;
            }
            if (incorrectPadding.top != t)
            {
                incorrectPadding.top = t;
                dirty = true;
            }
            if (incorrectPadding.bottom != b)
            {
                incorrectPadding.bottom = b;
                dirty = true;
            }

            if (dirty)
                UpdateMargins();
        }
        public void RemoveIncorrect()
        {
            if (incorrectPadding != null)
            {
                incorrectPadding = null;
                UpdateMargins();
            }
        }

        public void Expand(int dist)
        {
            if (dist > Dist)
            {
                Dist = dist;
            }
        }

        public override void Draw()
        {
            // Somehow this applies vertical margins.............
            /*
            if (leftDistStyle == null)
                leftDistStyle = new GUIStyle();
            leftDistStyle.padding.left = dist;

            EditorGUILayout.BeginVertical(leftDistStyle);
            DrawNext();
            EditorGUILayout.EndVertical();
            */

            if (leftSpacing == 0)
            {
                DrawNext();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(leftSpacing);

                EditorGUILayout.BeginVertical();
                {
                    DrawNext();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        internal protected override void OnApply()
        {
            UpdateMargins();
        }
    }
#endif

    public abstract class IMGUIContainerWrapper
    {
        public IMGUIContainerWrapping Wrapping { get; internal set; }

        public virtual void Init() { }

        public abstract void Draw();

        internal protected virtual void OnApply() { }

        //public int priority;

        internal Action next;
        protected void DrawNext()
        {
            next();
        }
    }

    public sealed class IMGUIContainerWrapping : VisualElement
    {
        /// <summary>
        /// The first wrapper in this list is drawn first, and the last wrapper draws the original onGUIHandler.
        /// This can be null.
        /// </summary>
        public List<IMGUIContainerWrapper> wrappers = null;

        public Action OriginalOnGUIHandler { get; private set; }
        private Action knownOnGUIHandler = null;

        public IMGUIContainer IMGUIContainer { get; private set; }

        private static readonly Type type = typeof(IMGUIContainerWrapping);
        public static IMGUIContainerWrapping GetWrapping(IMGUIContainer imgui)
        {
            var parent = imgui.parent;

            // Exists as a sibling, because as a child it breaks the IMGUI layout.

            for (int i = 0; i < parent.childCount; i++)
            {
                var sibling = parent[i];
                if (sibling.GetType() == type)
                {
                    var wrapping = (IMGUIContainerWrapping)sibling;
                    if (wrapping.IMGUIContainer == imgui)
                        return wrapping;
                }
            }

            var newWrapping = new IMGUIContainerWrapping();
            newWrapping.IMGUIContainer = imgui;
            newWrapping.style.display = DisplayStyle.None;
            parent.Add(newWrapping);
            return newWrapping;
        }

        public static T GetWrapper<T>(IMGUIContainer imgui, bool matchPerfectly = false) where T : IMGUIContainerWrapper, new()
        {
            bool _;
            return GetWrapper<T>(imgui, out _, matchPerfectly);
        }
        public static T GetWrapper<T>(IMGUIContainer imgui, out bool newlyCreated, bool matchPerfectly = false) where T : IMGUIContainerWrapper, new()
        {
            var wrapping = GetWrapping(imgui);

            if (wrapping.wrappers == null)
            {
                wrapping.wrappers = new List<IMGUIContainerWrapper>();
            }
            else
            {
                for (int i = 0; i < wrapping.wrappers.Count; i++)
                {
                    var w = wrapping.wrappers[i];
                    if (matchPerfectly ? (w.GetType() == typeof(T)) : (w is T))
                    {
                        newlyCreated = false;
                        return (T)w;
                    }
                }
            }

            var newW = new T()
            {
                Wrapping = wrapping
            };
            wrapping.wrappers.Add(newW);
            newW.Init();
            newlyCreated = true;
            return newW;
        }

        public void Apply()
        {
            var oldGUI = IMGUIContainer.onGUIHandler;
            if (oldGUI != knownOnGUIHandler)
                OriginalOnGUIHandler = oldGUI;

            if (wrappers != null)
            {
                int c = wrappers.Count;
                for (int i = 0; i < c; i++)
                {
                    var w = wrappers[i];

                    w.next = ((i + 1) < c) ? wrappers[i + 1].Draw : OriginalOnGUIHandler;

                    w.OnApply();
                }
            }

            Action newGUI;
            if (wrappers != null && wrappers.Count > 0)
                newGUI = wrappers[0].Draw;
            else
                newGUI = OriginalOnGUIHandler;

            IMGUIContainer.onGUIHandler = newGUI;
            knownOnGUIHandler = newGUI;
        }
    }
}
#endif
