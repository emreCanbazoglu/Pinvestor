/*
#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
using System;
using System.Reflection;
//using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using PrecisionCats;
using Object = UnityEngine.Object;

namespace EditorReplacement
{
    public class WrapperPropertyField : PropertyField
    {
        public object propertyHandlerCache; //protected readonly

        private bool isArray;
        private int arraySize = -1;

        public static IMGUIContainer DrawingWrappedIMGUIContainer { get; private set; } = null;

        public class IMGUIWrapper : IMGUIContainerWrapper
        {
            public WrapperPropertyField wpf;
            public PropertyField propertyField;

            public override void Draw()
            {
                bool prevHM = EditorGUIUtility.hierarchyMode;
                EditorGUIUtility.hierarchyMode = ERSettings.S.propertyIMGUIContainerHierarchyMode;
                var prevDrawingWrappedIMGUIContainer = DrawingWrappedIMGUIContainer;
                DrawingWrappedIMGUIContainer = Wrapping.IMGUIContainer;
                try
                {
                    using (new EditorCreationScope(original: true)) //?
                    using (new PropertyHandlerScope(wpf.propertyHandlerCache))
                    {
                        EditorGUI.BeginChangeCheck();

                        DrawNext();

                        if (EditorGUI.EndChangeCheck())
                        {
                            using (var e = PropertyEndChangeCheckEvent.GetPooled())
                            {
                                e.target = propertyField;
                                propertyField.SendEvent(e);
                            }
                        }
                    }

                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                }
                finally
                {
                    DrawingWrappedIMGUIContainer = prevDrawingWrappedIMGUIContainer;
                    EditorGUIUtility.hierarchyMode = prevHM;
                }
            }
        }

        public void TryWrapIMGUIContainer(PropertyField propertyField)
        {
            if (propertyField.hierarchy.childCount > 0
                && propertyField.hierarchy[0] is IMGUIContainer)
            {
                var imgui = (IMGUIContainer)propertyField.hierarchy[0];
                bool newlyCreated;
                var w = IMGUIContainerWrapping.GetWrapper<IMGUIWrapper>(imgui, out newlyCreated);
                if (newlyCreated)
                {
                    imgui.style.marginLeft = 0;
                    imgui.style.marginRight = 0;
                    imgui.style.marginBottom = 0;
                    imgui.style.marginTop = 0;

                    w.wpf = this;
                    w.propertyField = propertyField;
                    w.Wrapping.Apply();
                }
            }
        }

        private static readonly Type serializedPropertyBindEventType = AVCommonHelper.FindType("UnityEditor.UIElements.SerializedPropertyBindEvent");
        private static readonly FieldInfo spbeBindProperty = serializedPropertyBindEventType.GetField("m_BindProperty", R.npi);

        public event Action CallBeforeBind;

        public static PropertyField CurrentlyBinding { get; private set; } = null;
        /// <summary>
        /// Note that this and CurrentlyUITParentWhole don't care about visibility. It can be false even if there are no visible children. It uses PDRHelper.HasChildren(), and cares about property's hasChildren and PDRSettings.considerWhole_____
        /// </summary>
        public static bool CurrentlyUITWhole { get; private set; }
        public static bool CurrentlyUITParentWhole { get; private set; }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            if (CurrentlyBinding == null) // (Mostly to prevent UIElements.DetachFromPanelEvent of destroyed children)
            {
                // Leaves the proxy cache open as the global current. Quite an ugly solution, but Unity is not diligent either in always maintaining the correct handler cache for a context,
                // which is why swapping out the root editor's cache for the proxy cache is (although necessary) not reliable.
                new PropertyHandlerScope(ERHelper.uieProxyPropertyHandlerCache);
            }

            if (evt.GetType() != serializedPropertyBindEventType) //TODO4: isassignablefrom? 
            {
                // Skip if not rebinding
                base.ExecuteDefaultActionAtTarget(evt);
                return;
            }

            ERManager.UpdateBindingUpdateCount();

            if (!foundParentWPF)
            {
                foundParentWPF = true;
                parentWPF = GetFirstAncestorOfType<WrapperPropertyField>();
                if (parentWPF != null)
                    parentWhole = parentWPF.whole;
            }

            // This works in older versions only
            if (isArray)
            {
                SerializedProperty prop = (SerializedProperty)spbeBindProperty.GetValue(evt);
                if (prop != null)
                {
                    int newArraySize = prop.arraySize;
                    if (arraySize != -1 && newArraySize != arraySize)
                    {
                        if (OnChangeArraySize != null)
                            OnChangeArraySize(this, false, arraySize, newArraySize);
                        if (resetHandlersDirty != null)
                            resetHandlersDirty(prop.serializedObject, bindingPath);
                    }
                    arraySize = newArraySize;
                }
                else
                    Debug.LogWarning("?");
            }

            if (CallBeforeBind != null)
            {
                CallBeforeBind();
                CallBeforeBind = null;
            }

            var prevCurrentlyBinding = CurrentlyBinding;
            CurrentlyBinding = this;
            bool prevCurrentlyUIEParentWhole = CurrentlyUITParentWhole;
            CurrentlyUITParentWhole = parentWhole;
            bool prevCurrentlyUIEWhole = CurrentlyUITWhole;
            CurrentlyUITWhole = whole;
            try
            {
                using (new EditorCreationScope(original: true)) //?
                using (new PropertyHandlerScope(propertyHandlerCache))
                {
                    base.ExecuteDefaultActionAtTarget(evt);
                }

                TryWrapIMGUIContainer(this);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                CurrentlyUITWhole = prevCurrentlyUIEWhole;
                CurrentlyUITParentWhole = prevCurrentlyUIEParentWhole;
                CurrentlyBinding = prevCurrentlyBinding;
            }

            //new PropertyHandlerScope(ERHelper.proxyPropertyHandlerCache);
        }

        /// <summary>
        /// Note that this and parentWhole don't care about visibility. It can be false even if there are no visible children. It uses PDRHelper.HasChildren(), and cares about property's hasChildren and PDRSettings.considerWhole_____
        /// </summary>
        public readonly bool whole;
        public bool parentWhole;
        public WrapperPropertyField parentWPF;
        private bool foundParentWPF = false;

        public static Func<SerializedProperty, bool> getWhole;
        public static Action<SerializedObject, string> resetHandlersDirty;

        public delegate void OnChangeArraySizeEventHandler(WrapperPropertyField wrapperPropertyField, bool isArraySize, int from, int to);
        public static event OnChangeArraySizeEventHandler OnChangeArraySize;

        private WrapperPropertyField(SerializedProperty property) : base(property)
        {
            //Debug.Log("WrapperPropertyField: " + property.propertyPath + " - " + property.propertyType);

            var so = property.serializedObject;
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                RegisterCallback<ChangeEvent<Object>>((e) =>
                {
                    if (resetHandlersDirty != null)
                        resetHandlersDirty(so, bindingPath);
                });
            }

            if (getWhole != null)
                whole = getWhole(property);

            if (property.isArray && property.hasVisibleChildren) // hasVisibleChildren to avoid strings
                isArray = true;

            bool isArraySize = property.propertyType == SerializedPropertyType.ArraySize; //TODO2: only if original size is 0 or if array's element type is Generic/ObjectReference/ManagedReference
            if (isArray || isArraySize)
            {
                string arrayPath = null;
                EventCallback<EventBase> arrayChanged = (e) =>
                {
                    if (isArray)
                    {
                        if (!(e.target is TextField)) // (BTW it could otherwise be Label, for example in TextureImporter presets)
                            return;
                        if (((VisualElement)e.target).name != "unity-list-view__size-field") //TODO2: was it always called this?
                            return;
                        if (this != ((VisualElement)e.target).GetFirstAncestorOfType<PropertyField>())
                            return;
                    }

                    int from, to;
                    if (e is ChangeEvent<int>)
                    {
                        var ce = (ChangeEvent<int>)e;
                        from = ce.previousValue;
                        to = ce.newValue;
                        //Debug.Log("ChangeEvent<int> " + from + " to " + to);
                        //Debug.Log("Does this ever get called?");
                    }
                    else
                    {
                        var ce = (ChangeEvent<string>)e;
                        if (!int.TryParse(ce.previousValue, out from))
                            from = -1;
                        if (!int.TryParse(ce.newValue, out to))
                            to = -1;
                        //Debug.Log("ChangeEvent<string> " + from + " to " + to);
                    }

                    if (OnChangeArraySize != null)
                        OnChangeArraySize(this, isArraySize, from, to);
                    if (resetHandlersDirty != null)
                        resetHandlersDirty(so, arrayPath);
                };

                // This works in newer versions only (because in older versions PropertyField stops the propagation)
                if (isArraySize)
                {
                    arrayPath = bindingPath.Substring(0, bindingPath.Length - 11); // Removes ".Array.size"
                    RegisterCallback<ChangeEvent<int>>(arrayChanged, TrickleDown.NoTrickleDown);
                }
                else
                {
                    arrayPath = bindingPath;
                    RegisterCallback<ChangeEvent<int>>(arrayChanged, TrickleDown.NoTrickleDown); // (It seems this isn't being used, at least yet)
                    RegisterCallback<ChangeEvent<string>>(arrayChanged, TrickleDown.NoTrickleDown);
                }
            }
        }
        public WrapperPropertyField(object propertyHandlerCache, SerializedProperty property) : this(property)
        {
            this.propertyHandlerCache = propertyHandlerCache;
        }
        public WrapperPropertyField(Editor editor, SerializedProperty property) : this(property)
        {
            propertyHandlerCache = ERHelper.GetEditorPropertyHandlerCache(editor);
        }
    }
}
#endif
*/
