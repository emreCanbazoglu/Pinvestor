/*
MIT License

Copyright (c) 2022 Steffen Vetne

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SystemPrefs
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SystemPrefsState), editorForChildClasses: true)]
    public class SystemPrefsStateEditor : Editor
    {
        protected virtual bool ShouldDrawDefaultInspector => true;

        public override void OnInspectorGUI()
        {
            bool prevGUIEnabled = GUI.enabled;

            serializedObject.Update();

            var validProp = serializedObject.FindProperty("valid");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(validProp);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var state = (SystemPrefsState)targets[i];
                    if (state.Valid && state.Enabled)
                        SystemPrefsUtility.DeleteSystemKey(state.SystemKey);
                }
            }

            GUILayout.Space(2);

            if (validProp.boolValue) // || targets.Length != 1)
                GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("systemKey"));
            GUI.enabled = prevGUIEnabled;

            GUILayout.Space(2);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("systemActive"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("systemPriority"));

            serializedObject.ApplyModifiedProperties();

            if (ShouldDrawDefaultInspector)
            {
                GUILayout.Space(5);

                DrawDefaultInspector();
            }
        }
    }

    [CreateAssetMenu(fileName = "YourSystemName", menuName = "System Prefs/State", order = 1000000)]
    public class SystemPrefsState : SystemPrefsAsset
    {
        [Tooltip("If this asset should (if it's not disabled by its disableKey) write its state (systemActive and systemPriority) to SystemPrefs.")]
        [SerializeField, HideInInspector]
        protected bool valid = true;
        [SerializeField, HideInInspector]
        protected string systemKey;

        [Tooltip("Active value of this state.")]
        [SerializeField, HideInInspector]
        protected bool systemActive = true;
        [Tooltip("Priority value of this state.")]
        [SerializeField, HideInInspector]
        internal protected int systemPriority = 0;

        [TextArea(2, 30)]
        public string notes;

        public bool TryGetDisableKey(out string disableKey)
        {
            string guid; long localId;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out localId))
            {
                disableKey = "SystemPrefsState.DisableKey." + guid + "." + localId;
                return true;
            }
            disableKey = default(string);
            return false;
        }

        public virtual void OnChangedDisablePref(bool newDisabled)
        {
            if (!newDisabled)
            {
                UpdateSystemActive();
                UpdateSystemPriority();
            }
        }

        public virtual void PostDuplicationInit(string systemKey)
        {
            this.systemKey = systemKey;
        }

        /// <summary>
        /// If this state asset is valid.
        /// </summary>
        public override bool Valid => !string.IsNullOrWhiteSpace(systemKey) && valid;
        public override bool Enabled => !Disabled;
        public override bool Disabled => SystemPrefsUtility.GetDisabled(this, false);

        public string SystemKey => systemKey;
        public bool SystemActive
        {
            get
            {
                return systemActive;
            }
            set
            {
                if (systemActive != value)
                {
                    systemActive = value;
                    UpdateSystemActive();
                }
            }
        }
        public int SystemPriority
        {
            get
            {
                return systemPriority;
            }
            set
            {
                if (systemPriority != value)
                {
                    systemPriority = value;
                    UpdateSystemPriority();
                }
            }
        }

        public virtual void OnSystemActiveChanged() { }
        public void UpdateSystemActive()
        {
            if (Valid && Enabled)
                if (SystemPrefsUtility.SetActive(systemKey, systemActive))
                    OnSystemActiveChanged();
        }
        public virtual void OnSystemPriorityChanged() { }
        public void UpdateSystemPriority()
        {
            if (Valid && Enabled)
                if (SystemPrefsUtility.SetPriority(systemKey, systemPriority))
                    OnSystemPriorityChanged();
        }

        public override void Enable()
        {
            SystemPrefsUtility.SetDisabled(this, false);
        }
        public override void Disable()
        {
            SystemPrefsUtility.SetDisabled(this, true);
        }

        public override void PrefsDeactivate()
        {
            Disable();
            PrefsDeactivateDisabled();
        }
        /// <summary>
        /// Deactivates only if PriorityPrefs is what disabled this.
        /// </summary>
        public override void PrefsDeactivateDisabled()
        {
            if (Disabled && Valid)
                SystemPrefsUtility.SetActive(systemKey, false);
        }

        public static void AllUpdatePrefs()
        {
            var all = SystemPrefsUtility.FindAssets<SystemPrefsState>();
            for (int i = 0; i < all.Count; i++)
            {
                all[i].UpdateSystemActive();
                all[i].UpdateSystemPriority();
            }
        }

        protected virtual void Reset()
        {
            //TODO: does this method get called before or after the other values have been default initialized by Unity?
            if (Valid && Enabled)
                SystemPrefsUtility.DeleteSystemKey(systemKey);
        }

        protected virtual void OnValidate()
        {
            UpdateSystemActive();
            UpdateSystemPriority();
        }
    }
}
#endif
