#define ENABLE_DEFAULT_DRAWER

#define CACHED

// Developed by Tom Kail at Inkle
// Released under the MIT Licence as held at https://opensource.org/licenses/MIT
// Modified by Steffen

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ExtendedScriptableObjectAttribute : PropertyAttribute
{
    public bool isReadOnly = false;
    public ExtendedScriptableObjectAttribute() { }
    public ExtendedScriptableObjectAttribute(bool isReadOnly)
    {
        this.isReadOnly = isReadOnly;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class NonExtendedScriptableObjectAttribute : Attribute { }


#if UNITY_EDITOR
#if ENABLE_DEFAULT_DRAWER
// Default for every ScriptableObject without [ExtendedScriptableObject]
[CustomPropertyDrawer(typeof(ScriptableObject))]
internal class DefaultExtendedScriptableObjectDrawer : ExtendedScriptableObjectDrawer
{ }
#endif

/// <summary>
/// Extends how ScriptableObject object references are displayed in the inspector
/// Shows you all values under the object reference
/// Also provides a button to create a new ScriptableObject if property is null.
/// 
/// You can inherit from this and use [CustomPropertyDrawer(typeof(YourScriptableObjectType))].
/// But normally ENABLE_DEFAULT_DRAWER enables DefaultExtendedScriptableObjectDrawer which works for every ScriptableObject field.
/// </summary>
[CustomPropertyDrawer(typeof(ExtendedScriptableObjectAttribute))]
public class ExtendedScriptableObjectDrawer : PropertyDrawer
{
    public static Color lightBoxColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // new Color(0.65f, 0.65f, 0.65f, 1);
    public static Color darkBoxColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public static bool indentBox = false;
    public static readonly List<string> ignoreClassFullNames = new List<string> { "TMPro.TMP_FontAsset" };
    public static int buttonWidth = 40;

    private static GUIStyle fakeFoldoutStyle;
    private static uint depth = 0;

    public static uint maxDepth = 5;


    protected virtual bool ReadOnly { get { return false; } }


#if CACHED
    private static readonly List<Cache> caches = new List<Cache>();

    private class Cache
    {
        public ScriptableObject so;
        public SerializedObject serializedObject;
        public int frame;
        public uint depth;
    }

    private static int frameCounter = 0;

    private static SerializedObject GetSerializedObject(ScriptableObject so)
    {
        for (int i = 0; i < caches.Count; i++)
        {
            if (caches[i].so == so && caches[i].depth == depth)
            {
                caches[i].frame = frameCounter;
                caches[i].serializedObject.Update();
                return caches[i].serializedObject;
            }
        }

        var cache = new Cache()
        {
            so = so,
            serializedObject = new SerializedObject(so),
            frame = frameCounter,
            depth = depth,
        };
        caches.Add(cache);
        return cache.serializedObject;
    }

    /// <summary>
    /// Quick and dirty way to dispose of it after around 3 minutes of disuse
    /// </summary>
    public static int MAX_FRAMES = 10000;

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }
    private static void Update()
    {
        for (int i = caches.Count - 1; i >= 0; i--)
        {
            if (frameCounter < caches[i].frame)
            {
                caches[i].frame = frameCounter;
            }
            else if ((frameCounter - caches[i].frame) > MAX_FRAMES)
            {
                //Debug.Log("Unloaded cache for: " + caches[i].so.name);
                caches[i].serializedObject.Dispose();
                caches.RemoveAt(i);
            }
        }

        frameCounter++;
    }
#endif


    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        depth++;
        try
        {
            float totalHeight = EditorGUIUtility.singleLineHeight;
            var so = property.objectReferenceValue as ScriptableObject;
            if (depth > maxDepth || so == null)
                return totalHeight;

            if (property.isExpanded)
            {
#if CACHED
                SerializedObject serializedObject = GetSerializedObject(so);
#else
                SerializedObject serializedObject = new SerializedObject(so);
#endif

                bool prevGUIEnabled = BeginReadOnly();

                SerializedProperty prop = serializedObject.GetIterator();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.name == "m_Script") continue;
                        float height = EditorGUI.GetPropertyHeight(prop, null, true);
                        height += EditorGUIUtility.standardVerticalSpacing;
                        totalHeight += height;
                    }
                    while (prop.NextVisible(false));
                }
                prop.Dispose();

                totalHeight += EditorGUIUtility.standardVerticalSpacing; // Add a tiny bit of height if open for the background

                GUI.enabled = prevGUIEnabled;

#if !CACHED
                serializedObject.Dispose();
#endif
            }

            return totalHeight;
        }
        finally
        {
            depth--;
        }
    }

    private bool BeginReadOnly()
    {
        bool prevGUIEnabled = GUI.enabled;
        bool isReadOnly = ReadOnly;
        if (!isReadOnly)
        {
            var attribute = this.attribute as ExtendedScriptableObjectAttribute;
            if (attribute != null)
                isReadOnly = attribute.isReadOnly;
        }
        if (isReadOnly)
            GUI.enabled = false;
        return prevGUIEnabled;
    }

    /// <summary>
    /// Done in a roundabout way in order to prevent duplicate BeginProperty/EndProperty calls.
    /// </summary>
    private static void DoObjectField(Rect propertyRect, SerializedProperty property, GUIContent label, Type type)
    {
        bool prevShowMixed = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

        EditorGUI.BeginChangeCheck();
        var o = EditorGUI.ObjectField(propertyRect, label, property.objectReferenceValue, type, true);
        if (EditorGUI.EndChangeCheck())
            property.objectReferenceValue = o; //property.serializedObject.ApplyModifiedProperties();

        EditorGUI.showMixedValue = prevShowMixed;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        depth++;
        try
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var type = GetFieldType();


            // Ignoring
            if (depth > maxDepth || type == null
                || ignoreClassFullNames.Contains(type.FullName)
                || Attribute.IsDefined(fieldInfo, typeof(NonExtendedScriptableObjectAttribute)))
            {
                property.isExpanded = false; // Just in case
                DoObjectField(position, property, label, type);
                //EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }


            var so = property.objectReferenceValue as ScriptableObject;

            SerializedObject serializedObject = null;
            if (so != null)
            {
#if CACHED
                serializedObject = GetSerializedObject(so);
#else
                serializedObject = new SerializedObject(so);
#endif
            }


            // Label
            bool prevGUIChanged = GUI.changed;
            bool prevHierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = true;
            try
            {
                var foldoutRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                if (serializedObject != null && AreAnySubPropertiesVisible(serializedObject))
                {
                    property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
                }
                else
                {
                    if (fakeFoldoutStyle == null)
                        fakeFoldoutStyle = new GUIStyle(EditorStyles.foldout) { name = "" }; //Removes the triangle 
                    EditorGUI.Foldout(foldoutRect, false, label, true, fakeFoldoutStyle);
                }
            }
            finally
            {
                EditorGUIUtility.hierarchyMode = prevHierarchyMode;
                GUI.changed = prevGUIChanged; // Because isExpanded changes should normally be ignored
            }


            // Property
            var indentedPosition = EditorGUI.IndentedRect(position);
            var indentOffset = indentedPosition.x - position.x;
            const float kPrefixPaddingRight = 2;
            var propertyRect = new Rect
            (
                position.x + (EditorGUIUtility.labelWidth - indentOffset) + kPrefixPaddingRight,
                position.y,
                position.width - (EditorGUIUtility.labelWidth - indentOffset) - kPrefixPaddingRight,
                EditorGUIUtility.singleLineHeight
            );

            if (so == null)
                propertyRect.width -= buttonWidth;


            DoObjectField(propertyRect, property, GUIContent.none, type);


            if (serializedObject != null)
            {
                // ScriptableObject's Fields
                if (property.isExpanded)
                {
                    prevGUIChanged = GUI.changed;

                    bool prevGUIEnabled = BeginReadOnly();

                    // Draw a background that shows us clearly which fields are part of the ScriptableObject
                    Rect boxRect;
                    if (indentBox)
                    {
                        boxRect = indentedPosition;
                        boxRect.y += propertyRect.height;
                        boxRect.height -= propertyRect.height;
                    }
                    else
                        boxRect = new Rect(0, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing - 1, Screen.width, position.height - EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);
                    //GUI.Box(boxRect, "");
                    Color boxColor = EditorGUIUtility.isProSkin ? darkBoxColor : lightBoxColor;
                    if (!GUI.enabled)
                        boxColor.a *= 0.5f;
                    EditorGUI.DrawRect(boxRect, boxColor);

                    EditorGUI.indentLevel++;

                    // Iterate over all the values and draw them
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty prop = serializedObject.GetIterator();
                    float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            if (prop.name == "m_Script") continue;
                            float height = EditorGUI.GetPropertyHeight(prop, new GUIContent(prop.displayName, prop.tooltip), true);
                            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, true);
                            y += height + EditorGUIUtility.standardVerticalSpacing;
                        }
                        while (prop.NextVisible(false));
                    }
                    prop.Dispose();
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

#if !CACHED
                    serializedObject.Dispose();
#endif

                    EditorGUI.indentLevel--;

                    GUI.enabled = prevGUIEnabled;

                    GUI.changed = prevGUIChanged; // Because otherwise changes in here would trigger an implicit override to be created in Asset Variants
                }
            }
            else
            {
                // Create Button
                var buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(buttonRect, "New"))
                {
                    string selectedAssetPath = "Assets";
                    if (property.serializedObject.targetObject is MonoBehaviour)
                    {
                        MonoScript ms = MonoScript.FromMonoBehaviour((MonoBehaviour)property.serializedObject.targetObject);
                        selectedAssetPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));
                    }
                    else
                    {
                        var targetObject = property.serializedObject.targetObject;
                        if (targetObject is ScriptableObject)
                        {
                            selectedAssetPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath((ScriptableObject)targetObject));
                        }
                    }

                    property.objectReferenceValue = CreateAssetWithSavePrompt(type, selectedAssetPath);
                }
            }


            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
        finally
        {
            depth--;
        }
    }

    private Type GetFieldType()
    {
        Type type = fieldInfo.FieldType;
        if (type.IsArray)
            type = type.GetElementType();
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            type = type.GetGenericArguments()[0];
        return type;
    }

    private static ScriptableObject CreateAssetWithSavePrompt(Type type, string path)
    {
        path = EditorUtility.SaveFilePanelInProject("Save ScriptableObject", type.Name + ".asset", "asset", "Enter a file name for the ScriptableObject.", path);
        if (path == "") return null;
        ScriptableObject asset = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        EditorGUIUtility.PingObject(asset);
        return asset;
    }

    private static bool AreAnySubPropertiesVisible(SerializedObject serializedObject)
    {
        SerializedProperty prop = serializedObject.GetIterator();
        while (prop.NextVisible(true))
        {
            if (prop.name == "m_Script") continue;
            prop.Dispose();
            return true;
        }
        prop.Dispose();
        return false;
    }
}
#endif
