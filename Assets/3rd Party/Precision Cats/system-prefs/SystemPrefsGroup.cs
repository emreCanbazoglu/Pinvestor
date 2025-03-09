#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SystemPrefs
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SystemPrefsGroup))]
    public class SystemPrefsGroupEditor : Editor
    {
        public static readonly GUIContent toggleActiveSiblingLabel =
            new GUIContent("-> Toggle Active Sibling <-", "Toggles the active state, and deactivates siblings (if there is a parent).");
        public static readonly GUIContent toggleActiveLabel =
             new GUIContent("-> Toggle Active <-");

        public override void OnInspectorGUI()
        {
            bool prevGUIEnabled = GUI.enabled;
            GUI.enabled = true;

            GUILayout.Space(5);

            var targets = this.targets;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("parent"));

            GUILayout.Space(5);

            if (SystemPrefsWindow.shouldRepaint())
                Repaint();

            var buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (SystemPrefsWindow.toggleActiveSelfOnly())
            {
                if (GUI.Button(buttonRect, toggleActiveLabel))
                    for (int i = 0; i < targets.Length; i++)
                        ((SystemPrefsGroup)targets[i]).ToggleActive();
            }
            else
            {
                if (GUI.Button(buttonRect, toggleActiveSiblingLabel))
                    for (int i = 0; i < targets.Length; i++)
                        ((SystemPrefsGroup)targets[i]).ToggleActiveSibling();
            }

            GUILayout.Space(15);

            buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (GUI.Button(buttonRect, "Enable All"))
            {
                for (int i = 0; i < targets.Length; i++)
                    ((SystemPrefsGroup)targets[i]).Enable();
            }
            buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (GUI.Button(buttonRect, "Disable All"))
            {
                for (int i = 0; i < targets.Length; i++)
                    ((SystemPrefsGroup)targets[i]).Disable();
            }

            GUILayout.Space(5);

            buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (GUI.Button(buttonRect, "Deactivate All Disabled"))
            {
                for (int i = 0; i < targets.Length; i++)
                    ((SystemPrefsGroup)targets[i]).PrefsDeactivateDisabled();
            }
            buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            if (GUI.Button(buttonRect, "Deactivate All"))
            {
                for (int i = 0; i < targets.Length; i++)
                    ((SystemPrefsGroup)targets[i]).PrefsDeactivate();
            }

            if (targets.Length == 1)
            {
                GUILayout.Space(10);

                serializedObject.Update();

                var prop = serializedObject.FindProperty("children");

                // Size Property
                GUI.enabled = prevGUIEnabled;
                prop.NextVisible(true);
                EditorGUILayout.PropertyField(prop);
                GUI.enabled = true;

                GUILayout.Space(5);

                var spg = (SystemPrefsGroup)target;

                int id = 0;
                while (prop.NextVisible(false))
                {
                    var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());

                    float stw = SystemPrefsWindow.stateDisabledWidth;

                    rect.x += stw;
                    rect.width -= stw;

                    if (id < spg.children.Count)
                    {
                        var child = spg.children[id];

                        Color prevGUIColor = SystemPrefsWindow.SetColor(child == null || child.Valid, child == null || child.Enabled);

                        GUI.enabled = prevGUIEnabled;
                        EditorGUI.PropertyField(rect, prop, GUIContent.none);

                        if (child != null)
                        {
                            rect.x -= stw;
                            rect.width = stw;

                            //TODO: add toggle for the active state?

                            bool disabled = child.Disabled;
                            GUI.enabled = true;
                            if (GUI.Button(rect, disabled ? SystemPrefsWindow.disabledLabel : SystemPrefsWindow.enabledLabel))
                            {
                                if (disabled)
                                    child.Enable();
                                else
                                    child.Disable();
                            }
                        }

                        GUI.color = prevGUIColor;
                    }

                    id++;
                }

                bool hasStateChildren = false;
                for (int i = 0; i < spg.children.Count; i++)
                {
                    if (spg.children[i] is SystemPrefsState)
                    {
                        hasStateChildren = true;
                        break;
                    }
                }
                if (hasStateChildren)
                {
                    GUILayout.Space(15);

                    if (GUILayout.Button(new GUIContent("Duplicate", "This will create a random number of 6 digits and replace any numerical suffix in the duplicated SystemPrefsStates' keys.")))
                    {
                        string randomID = Random.Range(0, 1000000).ToString();

                        string oldPath = AssetDatabase.GetAssetPath(spg);
                        string newPath = oldPath.Substring(0, oldPath.Length - 6) + " - " + randomID + ".asset";

                        if (!AssetDatabase.CopyAsset(oldPath, newPath))
                            throw new System.Exception("?");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        var allNew = AssetDatabase.LoadAllAssetsAtPath(newPath);
                        for (int i = 0; i < allNew.Length; i++)
                        {
                            var newS = allNew[i] as SystemPrefsState;
                            if (newS == null)
                                continue;

                            string systemKey = newS.SystemKey;
                            for (int ii = systemKey.Length - 1; ii >= 0; ii--)
                            {
                                if (char.IsNumber(systemKey[ii]))
                                    systemKey = systemKey.Substring(0, ii);
                                else
                                    break;
                            }
                            newS.PostDuplicationInit(systemKey + randomID);
                            EditorUtility.SetDirty(newS);
                        }

                        var newSPG = AssetDatabase.LoadAssetAtPath<SystemPrefsGroup>(newPath);

                        var parent = spg.parent;
                        Undo.RecordObject(parent, "Duplicate");
                        parent.children.Add(newSPG);
                        EditorUtility.SetDirty(parent);

                        newSPG.parent = parent;
                        EditorUtility.SetDirty(newSPG);

                        Undo.RegisterCreatedObjectUndo(newSPG, "Duplicate");

                        Selection.objects = new Object[] { newSPG };
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            GUI.enabled = prevGUIEnabled;
        }
    }

    public abstract class SystemPrefsAsset : ScriptableObject
    {
        public abstract bool Valid { get; }
        public abstract bool Enabled { get; }
        public abstract bool Disabled { get; }

        public abstract void Enable();
        public abstract void Disable();

        public abstract void PrefsDeactivate();
        public abstract void PrefsDeactivateDisabled();
    }

    [CreateAssetMenu(menuName = "System Prefs/Group", order = 1000000)]
    public class SystemPrefsGroup : SystemPrefsAsset
    {
        /// <summary>
        /// If any of the descendants are null
        /// </summary>
        public bool Null
        {
            get
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] == null)
                        return true;
                    var group = children[i] as SystemPrefsGroup;
                    if (group != null && group.Null)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// If any of the children are Valid
        /// </summary>
        public override bool Valid
        {
            get
            {
                for (int i = 0; i < children.Count; i++)
                    if (children[i].Valid)
                        return true;
                return false;
            }
        }
        /// <summary>
        /// If any of the children are Enabled
        /// </summary>
        public override bool Enabled
        {
            get
            {
                for (int i = 0; i < children.Count; i++)
                    if (children[i].Enabled)
                        return true;
                return false;
            }
        }
        /// <summary>
        /// If any of the children are Disabled
        /// </summary>
        public override bool Disabled
        {
            get
            {
                for (int i = 0; i < children.Count; i++)
                    if (children[i].Disabled)
                        return true;
                return false;
            }
        }

        public override void Enable()
        {
            for (int i = 0; i < children.Count; i++)
                children[i].Enable();
        }
        public override void Disable()
        {
            for (int i = 0; i < children.Count; i++)
                children[i].Disable();
        }

        public override void PrefsDeactivateDisabled()
        {
            for (int i = 0; i < children.Count; i++)
                children[i].PrefsDeactivateDisabled();
        }
        public override void PrefsDeactivate()
        {
            for (int i = 0; i < children.Count; i++)
                children[i].PrefsDeactivate();
        }

        public void ToggleActiveSibling()
        {
            if (parent != null)
            {
                for (int i = 0; i < parent.children.Count; i++)
                {
                    var sibling = parent.children[i];
                    if (sibling != null && sibling != this)
                        sibling.PrefsDeactivate();
                }
            }

            ToggleActive();
        }
        public void ToggleActive()
        {
            //if (ValidAndEnabled)
            //    PrefsDeactivate();
            //else
            //    PrefsActivate();
            if (Disabled)
                Enable();
            else
                PrefsDeactivate();
        }

        public SystemPrefsGroup parent;
        public List<SystemPrefsAsset> children = new List<SystemPrefsAsset>();

        private void OnValidate()
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] is SystemPrefsGroup)
                {
                    var spg = (SystemPrefsGroup)children[i];
                    if (spg.parent != this)
                        children.RemoveAt(i);
                }
            }

            if (parent != null && !parent.children.Contains(this))
            {
                parent.children.Add(this);
                EditorUtility.SetDirty(parent);
            }
        }
    }
}
#endif
