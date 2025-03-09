#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Reflection;
using SystemPrefs;

namespace PrecisionCats
{
    /// <summary>
    /// Only works in TODO:::
    /// </summary>
    public abstract class PropertyModifier : SortedSystem<PropertyModifier>
    {
#if UNITY_2021_1_OR_NEWER // TODO2: is this correct version?
        public static float alignmentL = 3;
        public static float alignmentR = 3; //??
#else
        public static float alignmentL = 0;
        public static float alignmentR = 0; //??
#endif

#if UNITY_2020_1_OR_NEWER //TODO: CHECK
        private static readonly Stack<Action<Rect, SerializedProperty>> callbacks = new Stack<Action<Rect, SerializedProperty>>();
#endif

        public virtual bool Disabled => false;

        public virtual void Draw(Rect r, SerializedProperty prop) { }

        public virtual bool ShouldModify(SerializedObject serializedObject) => false;
        public virtual void Modify(PropertyField propertyField, SerializedProperty prop) { }

#if UNITY_2019_1_OR_NEWER
        public virtual bool NeedToApplyStyling => false;
        /// <summary>
        /// (To "leaf"s)
        /// </summary>
        public virtual void ApplyStyling(VisualElement textStyleElement, VisualElement element, VisualElement rootElement, SerializedProperty prop, bool outer) { }
#endif

#if UNITY_2020_1_OR_NEWER //TODO: CHECK
        private static EventInfo beginPropertyEI = typeof(EditorGUIUtility).GetEvent("beginProperty", BindingFlags.NonPublic | BindingFlags.Static);
#endif

        public static void Clear()
        {
#if UNITY_2020_1_OR_NEWER //TODO: CHECK
            var arg = new object[1];

            while (callbacks.Count > 0)
            {
                arg[0] = callbacks.Pop();
                beginPropertyEI.GetRemoveMethod(true).Invoke(null, arg);
            }
#endif
        }
        public static void Rebuild()
        {
            Clear();
            UpdateSSes();

#if UNITY_2020_1_OR_NEWER //TODO: CHECK
            var arg = new object[1];

            for (int i = 0; i < sses.Count; i++)
            {
                if (sses[i].cachedActive && !sses[i].Disabled)
                {
                    Action<Rect, SerializedProperty> callback = sses[i].Draw;
                    arg[0] = callback;
                    beginPropertyEI.GetAddMethod(true).Invoke(null, arg);
                    callbacks.Push(callback);
                }
            }
#endif
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            // (Delayed only for the sake of customSSes)
            EditorApplication.delayCall += () =>
            {
                Rebuild();

                rebuild += Rebuild;
            };

            Editor.finishedDefaultHeaderGUI += (Editor e) =>
            {
                if (!uitEditors.Contains(e))
                    TryInitUIT(e, true);
            };

            EditorApplication.update += () =>
            {
                timestamp++;

                FlushRebuildSSes();
            };

            SystemPrefsWindow.tabs.Add(new ReplacerTab()
            {
                title = new GUIContent("Property Modifiers", "Shows every key found in the current PropertyModifier.sses list.")
            });
        }
        private static int timestamp = int.MinValue;

        private static readonly Type editorElementType = AVCommonHelper.FindType("UnityEditor.UIElements.EditorElement");
        private static readonly PropertyInfo inspectorElementPI = editorElementType.GetProperty("m_InspectorElement", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo inspectorElementFI = editorElementType.GetField("m_InspectorElement", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo editorFI = typeof(InspectorElement).GetField("m_Editor", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool ignoreClicks = true;

        private static readonly WeakList<Editor> uitEditors = new WeakList<Editor>();
        private static readonly PropertyInfo editorsElementPI = typeof(Editor).Assembly.GetType("UnityEditor.PropertyEditor").GetProperty("editorsElement", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void TryInitUIT(Editor editor, bool first)
        {
            var inspectors = AVCommonHelper.GetAllInspectorWindows();
            for (int i = 0; i < inspectors.Length; i++)
            {
                var inspector = inspectors.GetValue(i);
                var tracker = (ActiveEditorTracker)AVCommonHelper.GetInspectorWindowTracker(inspector);
                if (tracker == null)
                    continue;

                bool contains = false;
                var editors = tracker.activeEditors;
                for (int ii = 0; ii < editors.Length; ii++)
                {
                    if (editors[ii] == editor)
                    {
                        contains = true;
                        break;
                    }
                }
                if (!contains)
                    continue;

                uitEditors.Add(editor); //???? TODO: move?

                var so = editor.serializedObject;

                uitModifiers.Clear();
                for (int ii = 0; ii < sses.Count; ii++)
                {
                    var ss = sses[ii];
                    if (ss.cachedActive && !ss.Disabled)
                        if (ss.ShouldModify(so))
                            uitModifiers.Add(ss);
                }
                if (uitModifiers.Count == 0)
                    continue;

                var editorsElement = editorsElementPI.GetValue(inspector) as VisualElement;
                if (editorsElement == null)
                    continue;

                VisualElement inspectorElement = null;
                for (int ii = 0; ii < editorsElement.childCount; ii++)
                {
                    var ee = editorsElement[ii];
                    if (ee.GetType() == editorElementType)
                    {
                        var ie = (inspectorElementPI != null ?
                            inspectorElementPI.GetValue(ee) :
                            inspectorElementFI.GetValue(ee)) as InspectorElement;
                        if (ie != null)
                        {
                            var e = (Editor)editorFI.GetValue(ie);
                            if (e == editor)
                            {
                                inspectorElement = ie;
                                break;
                            }
                        }
                    }
                }
                if (inspectorElement == null)
                {
                    if (first)
                    {
                        editorsElement.schedule.Execute(() =>
                        {
                            editorsElement.schedule.Execute(() =>
                            {
                                editorsElement.schedule.Execute(() =>
                                {
                                    //TODO: improve this.

                                    // Sometimes the EditorElement's InspectorElement isn't created yet.
                                    TryInitUIT(editor, false);
                                });
                            });
                        });
                    }
                    //else
                    //Debug.LogWarning("?");
                    return;
                }

                // This is necessary because unity rebuilds the visual elements for a locked inspector when the selection changes...............
                inspectorElement.RegisterCallback((DetachFromPanelEvent e) =>
                {
                    //TODO: surely in some version this is not necessary right?
                    uitEditors.Remove(editor);
                });

                //TODO:::not delayed?
                inspectorElement.schedule.Execute(() =>
                {
                    //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    //sw.Start();
                    TryModify(inspectorElement);
                    //Debug.Log(sw.Elapsed.TotalMilliseconds);
                });

                //TODO: does any UI Toolkit stuff use collapse/expand to modify their children?
                long ignoreTimestamp = long.MinValue;
                if (ignoreClicks)
                {
                    inspectorElement.RegisterCallback((ClickEvent e) =>
                    {
                        ignoreTimestamp = timestamp + 1;
                    }, TrickleDown.TrickleDown);
                }

                //TODO: make this ignore foldout changes?
                inspectorElement.RegisterCallback((GeometryChangedEvent e) =>
                {
                    if (timestamp <= ignoreTimestamp)
                    {
                        //Debug.Log("Ignored");
                        return;
                    }
                    //TODO: can multiple GeometryChangedEvent occur in one frame?
                    //ignoreTimestamp = timestamp;

                    //// Delayed so for the sake of the skipped ones
                    //inspectorElement.schedule.Execute(() =>
                    //{

                    //TODO: why does this happen twice at start?
                    //Debug.Log("GeometryChangedEvent TryModify");
                    TryModify(inspectorElement);
                    //});
                });

                break;
            }
        }

        private static readonly List<PropertyModifier> uitModifiers = new List<PropertyModifier>();

        private static readonly FieldInfo serializedPropertyFI = typeof(PropertyField).GetField("m_SerializedProperty", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly string modifiedPropertyClassName = "PropertyModifier-modified";
        private static void TryModify(VisualElement ve)
        {
            int cc = ve.childCount;

            if (cc == 0)
                return; //?

            var propertyField = ve as PropertyField;
            if (propertyField != null)
            {
                var first = propertyField[0];

                if (first is IMGUIContainer)
                    return;

                if (!propertyField.ClassListContains(modifiedPropertyClassName))
                {
                    propertyField.AddToClassList(modifiedPropertyClassName);

                    var prop = (SerializedProperty)serializedPropertyFI.GetValue(propertyField);

                    bool needToApplyStyling = false;
                    for (int i = 0; i < uitModifiers.Count; i++)
                    {
                        var m = uitModifiers[i];
                        m.Modify(propertyField, prop);
                        needToApplyStyling |= m.NeedToApplyStyling;
                    }

                    if (needToApplyStyling)
                        ApplyStyling(first, first, prop, true);
                }
            }

            for (int i = 0; i < cc; i++)
                TryModify(ve[i]);
        }

        private static void ApplyStyling(VisualElement element, VisualElement rootElement, SerializedProperty prop, bool outer = true)
        {
            if (element == null)
                return;

            VisualElement textStyleElement;

            // (From BindingsStyleHelpers.cs, or BindingExtensions.cs in older versions).

            if (element is Foldout)
            {
                // We only want to apply override styles onto the Foldout header, not the entire contents.
                textStyleElement = element.Q(className: Foldout.toggleUssClassName);
            }
            else if (element.ClassListContains(BaseCompositeField<int, IntegerField, int>.ussClassName)
                || element is BoundsField || element is BoundsIntField)
            {
                var compositeField = element;

                // The element we style in the main pass is going to be just the label.
                textStyleElement = element.Q(className: BaseField<int>.labelUssClassName);

                // Go through the inputs and find any that match the names of the child PropertyFields.
                var propCopy = prop.Copy();
                var endProperty = propCopy.GetEndProperty();
                propCopy.NextVisible(true); // Expand the first child.
                do
                {
                    if (SerializedProperty.EqualContents(propCopy, endProperty))
                        break;

                    var subInputName = "unity-" + propCopy.name + "-input";
                    var subInput = compositeField.Q(subInputName);
                    if (subInput == null)
                        continue;

                    ApplyStyling(subInput, rootElement, propCopy, false);
                }
                while (propCopy.NextVisible(false)); // Never expand children.
            }
            else
            {
                textStyleElement = element;
            }

            for (int i = 0; i < uitModifiers.Count; i++)
                uitModifiers[i].ApplyStyling(textStyleElement, element, rootElement, prop.Copy(), outer);
        }
    }
}
#endif
