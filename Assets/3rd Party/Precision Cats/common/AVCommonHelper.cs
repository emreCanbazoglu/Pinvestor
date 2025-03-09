//#define SIMPLE_IS_EDITABLE_CHECK // E.g. TextureImporter's Sprite children are not marked with HideFlags.NotEditable, can't reliably use the simple check

#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorReplacement
{
    /// <summary>
    /// Exists only to make sure the namespace EditorReplacement exists.
    /// </summary>
    internal static class DummyClass { }
}

public static class AVCommonHelper
{
    private static readonly Assembly assembly = typeof(Editor).Assembly;

    internal static Action<bool> onUpdateSetting;
    internal static void UpdateSetting(bool value, ref int prevValue, bool refreshSelection = false)
    {
        int newValue = value ? 1 : 0;
        if (prevValue != newValue)
        {
            if (prevValue != -1)
            {
                if (onUpdateSetting != null)
                    onUpdateSetting(refreshSelection);
            }

            prevValue = newValue;
        }
    }

    private static readonly Type iwType = assembly.GetType("UnityEditor.InspectorWindow");
    private static readonly MethodInfo getAllInspectorWindowsMI = iwType.GetMethod("GetAllInspectorWindows", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
    public static Array GetAllInspectorWindows()
    {
        return (Array)getAllInspectorWindowsMI.Invoke(null, null);
    }
    private const BindingFlags nppi = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
    private static readonly FieldInfo trackerFI = iwType.GetField("m_Tracker", nppi);
    public static object GetInspectorWindowTracker(object inspectorWindow)
    {
        return trackerFI.GetValue(inspectorWindow);
    }

    private static Type activeEditorTrackerType = assembly.GetType("UnityEditor.ActiveEditorTracker");
    private static MethodInfo forceRebuildMI = activeEditorTrackerType.GetMethod("ForceRebuild", nppi);

    private static bool refreshingSelection = false;
    public static void RefreshInspectors(bool refreshSelection = false, bool noRefreshSelection = false)
    {
        if (!refreshSelection)
        {
            try
            {
                var windows = GetAllInspectorWindows();
                for (int i = 0; i < windows.Length; i++)
                {
                    var tracker = GetInspectorWindowTracker(windows.GetValue(i));
                    if (tracker != null)
                        forceRebuildMI.Invoke(tracker, null);
                }
            }
            catch //(Exception e)
            {
                //Debug.LogWarning("? " + e);
                refreshSelection = true;
            }
        }

        if (noRefreshSelection)
            return;

        if (refreshSelection)
        {
            if (refreshingSelection)
                return;

            var selection = Selection.objects;
            if (selection.Length == 0)
                return;

            refreshingSelection = true;

            Selection.objects = new UnityEngine.Object[0];
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
                    refreshingSelection = false;
                    Selection.objects = selection;
                };
            };
        }
    }

    public static Type FindUnityEditorType(string fullName)
    {
        var type = assembly.GetType(fullName);
        if (type != null)
            return type;
        return null;
    }
    public static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName);
            if (type != null)
                return type;
        }
        return null;
    }

    // TODO3:
#if !SIMPLE_IS_EDITABLE_CHECK
    /// <summary>
    /// Needs to match; child types need to also be added.
    /// </summary>
    public static List<Type> mainAssetsOverwriteChildren = new List<Type>() { };
    /// <summary>
    /// Needs to match; child types need to also be added.
    /// </summary>
    public static List<Type> importersDontOverwriteChildren = new List<Type>()
    {
        typeof(AssetImporter), // Base AssetImporter is used for ScriptableObjects
    };
#endif
    public static bool IsEditable(this UnityEngine.Object target)
    {
#if SIMPLE_IS_EDITABLE_CHECK
        return (target.hideFlags & HideFlags.NotEditable) == 0;
#else
        if (target is Component)
            return true;

        if ((target.hideFlags & HideFlags.NotEditable) != 0)
            return false;

        var path = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(path))
            return true;

        //TODO2: EditorUtility.IsPersistent etc.

        var importer = AssetImporter.GetAtPath(path);
        if (importer == null || importer == target)
            return true;
        if (!importersDontOverwriteChildren.Contains(importer.GetType()))
            return false;

        var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
        if (mainAsset == null || mainAsset == target)
            return true;
        if (mainAssetsOverwriteChildren.Contains(mainAsset.GetType()))
            return false;

        return true;
#endif
    }

    public static Rect GetFlatRect(bool hasLabel = true)
    {
        var svs = EditorGUIUtility.standardVerticalSpacing;
        //var rect = EditorGUILayout.GetControlRect(hasLabel, GUILayout.Height(-svs));
        var rect = EditorGUILayout.GetControlRect(hasLabel, -svs);
        rect.height = 0;
        return rect;
    }

    public static bool TargetsAreEqual(Object[] targets, Editor editor)
    {
        if (editor.target != targets[0])
            return false;
        return TargetsAreEqual(targets, editor.targets); // TODO2: does this create garbage?
    }
    public static bool TargetsAreEqual(Object[] targets, SerializedObject so)
    {
        if (so.targetObject != targets[0])
            return false;
        return TargetsAreEqual(targets, so.targetObjects); // TODO2: does this create garbage?
    }
    public static bool TargetsAreEqual(IReadOnlyCollection<Object> targets1, IReadOnlyCollection<Object> targets2)
    {
        if (targets1.Count != targets2.Count)
            return false;
        return TargetsAreEqual((IEnumerable<Object>)targets1, targets2);
    }
    private static bool TargetsAreEqual(IEnumerable<Object> targets1, IEnumerable<Object> targets2)
    {
        var e1 = targets1.GetEnumerator();
        var e2 = targets2.GetEnumerator();
        {
            while (e1.MoveNext() && e2.MoveNext())
            {
                var target1 = e1.Current;
                var target2 = e2.Current;
                if (!ReferenceEquals(target1, target2))
                    return false;
            }
        }
        if (e1 is IDisposable)
            e1.Dispose();
        if (e2 is IDisposable)
            e2.Dispose();
        return true;
    }
}
#endif
