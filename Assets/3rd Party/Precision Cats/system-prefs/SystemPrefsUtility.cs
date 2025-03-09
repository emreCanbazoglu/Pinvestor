#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SystemPrefs
{
    public static class SystemPrefsUtility
    {
        public const string ACTIVE_SUFFIX = ".Active";
        public const string PRIORITY_SUFFIX = ".Priority";

        public static void DeleteSystemKey(string systemKey)
        {
            EditorProjectPrefs.DeleteKey(systemKey + ACTIVE_SUFFIX);
            EditorProjectPrefs.DeleteKey(systemKey + PRIORITY_SUFFIX);
        }

        public delegate void OnChangedEventHandler(string systemKey);
        public static event OnChangedEventHandler OnChanged;

        public static bool GetActive(string systemKey, bool defaultActive = false)
        {
            return EditorProjectPrefs.GetBool(systemKey + ACTIVE_SUFFIX, defaultActive);
        }
        public static bool SetActive(string systemKey, bool active)
        {
            if (GetActive(systemKey) != active)
            {
                EditorProjectPrefs.SetBool(systemKey + ACTIVE_SUFFIX, active);
                if (OnChanged != null)
                    OnChanged(systemKey);
                return true;
            }
            return false;
        }

        public static int GetPriority(string systemKey, int defaultPriority = 0)
        {
            return EditorProjectPrefs.GetInt(systemKey + PRIORITY_SUFFIX, defaultPriority);
        }
        public static bool SetPriority(string systemKey, int priority)
        {
            if (GetPriority(systemKey) != priority)
            {
                EditorProjectPrefs.SetInt(systemKey + PRIORITY_SUFFIX, priority);
                if (OnChanged != null)
                    OnChanged(systemKey);
                return true;
            }
            return false;
        }

        public delegate void OnDisabledChangedEventHandler(string disableKey);
        public static event OnDisabledChangedEventHandler OnDisabledChanged;

        public static bool GetDisabled(this SystemPrefsState pps, bool unknown)
        {
            string disableKey;
            if (pps.TryGetDisableKey(out disableKey))
                return GetDisabled(disableKey);
            else
                return unknown;
        }
        public static bool SetDisabled(this SystemPrefsState pps, bool disabled)
        {
            string disableKey;
            if (pps.TryGetDisableKey(out disableKey))
            {
                SetDisabled(disableKey, disabled);
                pps.OnChangedDisablePref(disabled);
                return true;
            }
            Debug.LogWarning("!");
            return false;
        }
        private static bool GetDisabled(string disableKey)
        {
            return EditorProjectPrefs.GetBool(disableKey, false);
        }
        private static void SetDisabled(string disableKey, bool disabled)
        {
            if (disabled)
            {
                if (!GetDisabled(disableKey))
                {
                    EditorProjectPrefs.SetBool(disableKey, true);
                    if (OnDisabledChanged != null)
                        OnDisabledChanged(disableKey);
                }
            }
            else
            {
                bool prevDisabled = GetDisabled(disableKey);
                EditorProjectPrefs.DeleteKey(disableKey);
                if (prevDisabled)
                    if (OnDisabledChanged != null)
                        OnDisabledChanged(disableKey);
            }
        }

        private static readonly HashSet<string> alreadyGUIDs = new HashSet<string>();
        public static List<T> FindAssets<T>() where T : Object
        {
            return FindAssets<T>("t:" + typeof(T).Name);
        }
        public static List<T> FindAssets<T>(string search) where T : Object
        {
            var guids = AssetDatabase.FindAssets(search);

            List<T> ts = new List<T>();

            alreadyGUIDs.Clear();
            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                if (!alreadyGUIDs.Add(guid))
                    continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".unity"))
                    continue;
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int ii = 0; ii < assets.Length; ii++)
                {
                    var t = assets[ii] as T;
                    if (t != null)
                        ts.Add(t);
                }
            }

            return ts;
        }
    }
}
#endif
