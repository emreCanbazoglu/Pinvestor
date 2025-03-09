#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
//using System.Runtime.CompilerServices;
using System.Reflection;
using Object = UnityEngine.Object;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace EditorReplacement
{
    internal static class R
    {
        public const BindingFlags nps = BindingFlags.NonPublic | BindingFlags.Static;

        public static readonly Assembly assembly = typeof(Editor).Assembly;
        public static readonly Type customEditorAttributesType = assembly.GetType("UnityEditor.CustomEditorAttributes");
        public static readonly Type monoEditorTypeType = customEditorAttributesType.GetNestedType("MonoEditorType", BindingFlags.NonPublic); //assembly.GetType("UnityEditor.CustomEditorAttributes.MonoEditorType");
        public static readonly Type monoEditorTypeListType = typeof(List<>).MakeGenericType(monoEditorTypeType);
        public static readonly MethodInfo rebuildMI = customEditorAttributesType.GetMethod("Rebuild", nps);

        public static readonly FieldInfo s_InitializedFI = customEditorAttributesType.GetField("s_Initialized", nps);

        // public static readonly PropertyInfo instance = customEditorAttributesType.GetProperty("instance", nps);
        // public static readonly FieldInfo m_Cache = customEditorAttributesType.GetField("m_Cache", nps);
        // public static readonly Type customEditorCacheType = assembly.GetType("UnityEditor.CustomEditorCache");
        // public static readonly FieldInfo m_CustomEditorCache = customEditorCacheType != null ? customEditorCacheType.GetField("m_CustomEditorCache", nps) : null;
        // public static readonly Type monoEditorTypeStorageType = assembly.GetType("UnityEditor.CustomEditorCache");
        // public static readonly FieldInfo customEditors = monoEditorTypeStorageType != null ? monoEditorTypeStorageType.GetField("kSCustomEditors", nps) : null;
        // public static readonly FieldInfo customEditorsMultiEdition = monoEditorTypeStorageType != null ? monoEditorTypeStorageType.GetField("kSCustomMultiEditors", nps) : null;

        public static readonly FieldInfo kSCustomEditorsFI = customEditorAttributesType.GetField("kSCustomEditors", nps);
        public static readonly FieldInfo kSCustomMultiEditorsFI = customEditorAttributesType.GetField("kSCustomMultiEditors", nps);
        public static readonly MethodInfo findCustomEditorTypeByTypeMI = customEditorAttributesType.GetMethod("FindCustomEditorTypeByType", nps);
        public static readonly object[] arg = new object[1];
        public static readonly object[] args = new object[2];
        public static readonly object[] args3 = new object[3];

        // (Note that in 2017.3.? and earlier kSCustomEditorsFI and kSCustomMultiEditorsFI were List<MonoEditorType>)

        public const BindingFlags npi = BindingFlags.NonPublic | BindingFlags.Instance;
        public const BindingFlags pi = BindingFlags.Public | BindingFlags.Instance;

        public static readonly Type sauType = assembly.GetType("UnityEditor.ScriptAttributeUtility");
        public static readonly FieldInfo currentCacheFI = sauType.GetField("s_CurrentCache", nps);

        public static readonly Type propertyHandlerCacheType = assembly.GetType("UnityEditor.PropertyHandlerCache");

        public static readonly FieldInfo contextFI = typeof(Editor).GetField("m_Context", npi);
    }

    public static class ERHelper
    {
        internal static void PostProcess<TLT, RT, TDT, TNT>(TLT loc, Action<TLT> postProcess = null)
            where TLT : TreeLocation<TLT, RT, TDT, TNT>
            where RT : Replacer<RT, TDT, TNT>
            where TDT : TreeDef<RT, TDT, TNT>
            where TNT : TreeNode<RT, TDT, TNT>
        {
            if (loc.node.replacer != null)
            {
                if (postProcess == null)
                    loc.node.replacer.PostProcess(loc.node, loc.treeDef);
                else
                    postProcess(loc);

                var alreadyPPed = new List<TNT>();
                const int LIMIT = 1000;
                // Double layered so that elements can be inserted and deleted
                for (int i = 0; i < LIMIT; i++)
                {
                    bool processed = false;

                    for (int ii = 0; ii < loc.node.children.Count; ii++)
                    {
                        var node = loc.node.children[ii];
                        if (!alreadyPPed.Contains(node))
                        {
                            alreadyPPed.Add(node);
                            var childLoc = TreeLocation<TLT, RT, TDT, TNT>.Create(loc.depth + 1, ii, node, loc.treeDef, loc);
                            PostProcess<TLT, RT, TDT, TNT>(childLoc, postProcess);
                            processed = true;
                            break; // Breaks so that removing doesn't mess with the order much
                        }
                    }

                    if (!processed)
                        break;

                    if (i == LIMIT - 1)
                        Debug.LogError("Reached limit!");
                }
            }
        }

        public static readonly Type genericInspectorType = AVCommonHelper.FindType("GenericInspector"); //TODO2: use the actual assembly?

        public static Editor CreateEditor(this Editor contextEditor, Object[] targetObjects, Type editorType)
        {
            var context = (Object)R.contextFI.GetValue(contextEditor);
            //Debug.Log(context);
            return Editor.CreateEditorWithContext(targetObjects, context, editorType);
        }
        public static Editor CreateEditor(this Editor contextEditor, Object[] targetObjects)
        {
            var context = (Object)R.contextFI.GetValue(contextEditor);
            return Editor.CreateEditorWithContext(targetObjects, context);
        }

        public static void SetSpacing(this VisualElement ve, GUIStyle style)
        {
            var p = style.padding;
            ve.style.paddingLeft = p.left;
            ve.style.paddingRight = p.right;
            ve.style.paddingTop = p.top;
            ve.style.paddingBottom = p.bottom;

            var m = style.margin;
            ve.style.marginLeft = m.left;
            ve.style.marginRight = m.right;
            ve.style.marginTop = m.top;
            ve.style.marginBottom = m.bottom;

            //TODO2: any more?
        }

#if UNITY_2019_1_OR_NEWER
        public static void SetDisplay(this VisualElement ve, bool flex)
        {
            if (flex)
                ve.style.display = DisplayStyle.Flex;
            else
                ve.style.display = DisplayStyle.None;
        }
#endif

        public static Action<Editor> deferEditorInitialization;
        public static Action<Editor> initializeEditor;
        public static Action<Editor> forgetEditor;

        public static IEnumerable<Editor> EnumerateEditors(Editor editor, Object[] targetsFilter = null)
        {
            var re = editor as IReplacementEditor;
            if (re != null)
            {
                foreach (Editor e in re.EnumerateEditors(targetsFilter))
                    yield return e;
            }
            else if (targetsFilter == null || AVCommonHelper.TargetsAreEqual(targetsFilter, editor))
            {
                yield return editor;
            }
        }

        public static object GetCurrentPropertyHandlerCache()
        {
            return R.currentCacheFI.GetValue(null);
        }

        private static readonly FieldInfo editorCacheFI = typeof(Editor).GetField("m_PropertyHandlerCache", R.npi);
        private static readonly WeakTable<Editor, object> actualEditorPropertyHandlerCaches = new WeakTable<Editor, object>(); //TODO2: ConditionalWeakTable
        public static readonly object uieProxyPropertyHandlerCache = Activator.CreateInstance(R.propertyHandlerCacheType);
        public static object GetEditorPropertyHandlerCache(this Editor editor)
        {
            if (editor == null)
                return null;
            else
            {
                object cache;
                if (actualEditorPropertyHandlerCaches.TryGetValue(editor, out cache))
                {
                    // (https://forum.unity.com/threads/occasional-hard-crash.1257519/)
                    //if (cache.GetType() != R.propertyHandlerCacheType)
                    //    throw new Exception("(cache.GetType() != R.propertyHandlerCacheType) " + cache);
                    return cache;
                }
                cache = editorCacheFI.GetValue(editor);
                if (editor is WrapperEditor && ((WrapperEditor)editor).IsRoot)
                {
                    //Debug.Log("ROOT: " + editor);

                    // (https://forum.unity.com/threads/occasional-hard-crash.1257519/)
                    //if (cache.GetType() != R.propertyHandlerCacheType)
                    //    throw new Exception("ADDING - (cache.GetType() != R.propertyHandlerCacheType) " + cache);
                    actualEditorPropertyHandlerCaches.Add(editor, cache);
                    editorCacheFI.SetValue(editor, uieProxyPropertyHandlerCache);
                }
                return cache;
            }
        }

        public static object BeginEditor(Editor editor)
        {
            object _;
            return BeginEditor(editor, out _);
        }
        public static object BeginEditor(Editor editor, out object editorPropertyHandlerCache)
        {
            object prevCache = R.currentCacheFI.GetValue(null);

            editorPropertyHandlerCache = GetEditorPropertyHandlerCache(editor);
            R.currentCacheFI.SetValue(null, editorPropertyHandlerCache);

            return prevCache;
        }
        public static void EndEditor(object prevPropertyHandlerCache)
        {
            R.currentCacheFI.SetValue(null, prevPropertyHandlerCache);
        }

        /// <summary>
        /// For lone SerializedObjects, not an Editor's serializedObject.
        /// </summary>
        public static object GetSOPropertyHandlerCache(this SerializedObject so)
        {
            if (so == null)
            {
                return null;
            }
            else
            {
                object propertyHandlerCache;
                var caches = soPropertyHandlerCaches;
                if (!caches.TryGetValue(so, out propertyHandlerCache))
                {
                    propertyHandlerCache = Activator.CreateInstance(R.propertyHandlerCacheType);
                    caches.Add(so, propertyHandlerCache);

                    if (OnCreatedSOPropertyHandlerCache != null)
                        OnCreatedSOPropertyHandlerCache(so, propertyHandlerCache);
                }
                return propertyHandlerCache;
            }
        }

        public delegate void OnCreatedSOPropertyHandlerCacheHandler(SerializedObject so, object cache);
        /// <summary>
        /// For lone SerializedObjects, not an Editor's serializedObject.
        /// </summary>
        public static event OnCreatedSOPropertyHandlerCacheHandler OnCreatedSOPropertyHandlerCache;
        /// <summary>
        /// For lone SerializedObjects, not an Editor's serializedObject.
        /// </summary>
        public static readonly WeakTable<SerializedObject, object> soPropertyHandlerCaches = new WeakTable<SerializedObject, object>(); //TODO2: ConditionalWeakTable
        /// <summary>
        /// For lone SerializedObjects, not an Editor's serializedObject.
        /// </summary>
        public static object BeginSO(SerializedObject so)
        {
            object _;
            return BeginSO(so, out _);
        }
        /// <summary>
        /// For lone SerializedObjects, not an Editor's serializedObject.
        /// </summary>
        public static object BeginSO(SerializedObject so, out object soPropertyHandlerCache)
        {
            object prevCache = R.currentCacheFI.GetValue(null);

            soPropertyHandlerCache = GetSOPropertyHandlerCache(so);
            R.currentCacheFI.SetValue(null, soPropertyHandlerCache);

            return prevCache;
        }
        /// <summary>
        /// For lone SerializedObjects, not an Editor's serializedObject.
        /// </summary>
        public static void EndSO(object prevPropertyHandlerCache)
        {
            R.currentCacheFI.SetValue(null, prevPropertyHandlerCache);
        }

        /// <summary>
        /// Important for assigning the editor's property handler cache as the current with: using (new PropertyHandlerScope(childEditor))
        /// </summary>
        public static void ContextualOnInspectorGUI(this Editor childEditor)
        {
            using (new PropertyHandlerScope(childEditor))
            {
                childEditor.OnInspectorGUI();
            }
        }

#if UNITY_2019_1_OR_NEWER
        public static VisualElement CreateSeparationLine(Color color, float thickness = 2, float padding = 10)
        {
            var line = new VisualElement();
            line.style.marginTop = padding * 0.5f;
            line.style.height = thickness;
            line.style.marginBottom = padding * 0.5f;
            line.style.backgroundColor = color;
            return line;
        }
#endif

        public static void DrawSeparationLine(Color color, float thickness = 2, float padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding * 0.5f;
            EditorGUI.DrawRect(r, color);
        }
    }
}
#endif
