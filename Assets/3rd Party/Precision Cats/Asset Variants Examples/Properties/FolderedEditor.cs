using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif


// Note that FolderedEditor (currently) is IMGUI only


[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class NewFolderAttribute : PropertyAttribute
{
    public string name;
    public float space;
    public bool indent;

    public NewFolderAttribute(string name)
    {
        this.name = name;
        this.space = 0;
        this.indent = true;
    }
    public NewFolderAttribute(string name, float space)
    {
        this.name = name;
        this.space = space;
        this.indent = true;
    }
    public NewFolderAttribute(string name, float space, bool indent)
    {
        this.name = name;
        this.space = space;
        this.indent = indent;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class BeginFolderAttribute : PropertyAttribute
{
    public string name;
    public float space;
    public bool indent;

    public BeginFolderAttribute(string name)
    {
        this.name = name;
        this.space = 0;
        this.indent = true;
    }
    public BeginFolderAttribute(string name, float space)
    {
        this.name = name;
        this.space = space;
        this.indent = true;
    }
    public BeginFolderAttribute(string name, float space, bool indent)
    {
        this.name = name;
        this.space = space;
        this.indent = indent;
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class EndFolderAttribute : PropertyAttribute
{
    public EndFolderAttribute()
    {
    }
}


#if UNITY_EDITOR
[CanEditMultipleObjects]
public class FolderedEditor<T> : Editor where T : UnityEngine.Object
{
    // Fields
    protected readonly Folder baseFolder = new Folder();

    //private static readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    private static GUIStyle foldoutStyle;



    // Datatypes
    protected class Folder
    {
        //public bool foldout;
        public Folder parent;

        public string folderName;
        public float space;
        public bool indent;

        public List<object> children = new List<object>();
    }



    // Methods
    private static void Space(float space)
    {
#if UNITY_2019_3_OR_NEWER
        EditorGUILayout.Space(space);
#else
        if (space >= 5)
            EditorGUILayout.Space();
#endif
    }

    protected virtual void DoFolder(Folder f, bool first, string path)
    {
        path = path + "/" + f.folderName;

        Space(f.space);

        bool foldout = true;
        if (!first)
        {
            foldout = EditorPrefs.GetBool("FolderedEditor." + path);

            EditorGUI.BeginChangeCheck();
            foldout = EditorGUILayout.Foldout(foldout, f.folderName, true, foldoutStyle);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool("FolderedEditor." + path, foldout);
        }

        if (f.indent)
            EditorGUI.indentLevel++;

        if (foldout)
        {
            for (int i = 0; i < f.children.Count; i++)
                DoChild(f.children[i], path);
        }

        if (f.indent)
            EditorGUI.indentLevel--;
    }
    protected virtual void DoChild(object o, string path)
    {
        if (o is SerializedProperty)
        {
            DoProperty((SerializedProperty)o);
        }
        else if (o is Folder)
        {
            DoFolder((Folder)o, false, path);
        }
    }
    protected virtual void DoProperty(SerializedProperty sp)
    {
        EditorGUILayout.PropertyField(sp);
    }

    private void Exit(ref Folder currentFolder)
    {
        if (currentFolder.parent != null)
            currentFolder = currentFolder.parent;
    }
    private void DoFolder(string name, float space, bool indent, ref Folder currentFolder)
    {
        //Debug.Log(currentFolder.folderName + "/" + name);

        for (int i = 0; i < currentFolder.children.Count; i++)
        {
            var child = currentFolder.children[i];
            if (child is Folder)
            {
                Folder f = (Folder)child;
                if (f.folderName == name)
                {
                    currentFolder = f;
                    return;
                }
            }
        }

        var newFolder = new Folder()
        {
            parent = currentFolder,
            folderName = name,
            space = space,
            indent = indent,
        };

        currentFolder.children.Add(newFolder);
        currentFolder = newFolder;
    }
    private void Do(Type t, BindingFlags bindingFlags)
    {
        if (t.BaseType != null)
            Do(t.BaseType, bindingFlags);

        Folder currentFolder = baseFolder;
        foreach (FieldInfo field in t.GetFields(bindingFlags))
        {
            var nonserialized = field.GetCustomAttribute(typeof(NonSerializedAttribute)) != null;
            var serializeField = field.GetCustomAttribute(typeof(SerializeField)) != null;
            if ((!field.IsPublic && !serializeField) || nonserialized)
                continue;

            var ft = field.FieldType;

            //End Folders
            var endFolderAttributes = (EndFolderAttribute[])field.GetCustomAttributes(typeof(EndFolderAttribute));
            for (int i = 0; i < endFolderAttributes.Length; i++)
                Exit(ref currentFolder);

            //New Folder
            var newFolderAttribute = (NewFolderAttribute)field.GetCustomAttribute(typeof(NewFolderAttribute));
            if (newFolderAttribute != null)
            {
                Exit(ref currentFolder);
                DoFolder(newFolderAttribute.name, newFolderAttribute.space, newFolderAttribute.indent, ref currentFolder);
            }

            //Folders
            var folderAttributes = (BeginFolderAttribute[])field.GetCustomAttributes(typeof(BeginFolderAttribute));
            for (int i = 0; i < folderAttributes.Length; i++)
            {
                var att = folderAttributes[i];
                DoFolder(att.name, att.space, att.indent, ref currentFolder);
            }

            currentFolder.children.Add(serializedObject.FindProperty(field.Name));
        }
    }




    // Lifecycle
    protected virtual void OnEnable()
    {
        if (baseFolder.children.Count > 0)
            return;

        baseFolder.children.Clear();

        var bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        Do(typeof(T), bindingFlags);
    }

    public override void OnInspectorGUI()
    {
        if (foldoutStyle == null)
        {
            foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.BoldAndItalic;
        }

        serializedObject.Update();


        bool prevGUIEnabled = GUI.enabled;
        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        GUI.enabled = prevGUIEnabled;
        DoFolder(baseFolder, true, serializedObject.GetType().Name);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
