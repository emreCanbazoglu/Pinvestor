//#define EPP_USES_EDITOR_PREFS

#if UNITY_EDITOR

#if EPP_USES_EDITOR_PREFS
using UnityEditor;
#else
using UnityEngine;
#endif

public static class EditorProjectPrefs
{
    /// <summary>
    /// Only used in EditorProjectPrefs when EPP_USES_EDITOR_PREFS is defined.
    /// </summary>
    public static readonly string editorPrefsPrefix = UnityEditor.PlayerSettings.companyName + "." + UnityEditor.PlayerSettings.productName + "/";

    public static bool HasKey(string key)
    {
#if EPP_USES_EDITOR_PREFS
        return EditorPrefs.HasKey(editorPrefsPrefix + key);
#else
        return PlayerPrefs.HasKey(key);
#endif
    }
    public static void DeleteKey(string key)
    {
#if EPP_USES_EDITOR_PREFS
        EditorPrefs.DeleteKey(editorPrefsPrefix + key);
#else
        PlayerPrefs.DeleteKey(key);
#endif
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
#if EPP_USES_EDITOR_PREFS
        return EditorPrefs.GetInt(editorPrefsPrefix + key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue);
#endif
    }
    public static void SetInt(string key, int value)
    {
#if EPP_USES_EDITOR_PREFS
        EditorPrefs.SetInt(editorPrefsPrefix + key, value);
#else
        PlayerPrefs.SetInt(key, value);
#endif
    }

    public static float GetFloat(string key, float defaultValue = 0.0f)
    {
#if EPP_USES_EDITOR_PREFS
        return EditorPrefs.GetFloat(editorPrefsPrefix + key, defaultValue);
#else
        return PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }
    public static void SetFloat(string key, float value)
    {
#if EPP_USES_EDITOR_PREFS
        EditorPrefs.SetFloat(editorPrefsPrefix + key, value);
#else
        PlayerPrefs.SetFloat(key, value);
#endif
    }

    public static string GetString(string key, string defaultValue = "")
    {
#if EPP_USES_EDITOR_PREFS
        return EditorPrefs.GetString(editorPrefsPrefix + key, defaultValue);
#else
        return PlayerPrefs.GetString(key, defaultValue);
#endif
    }
    public static void SetString(string key, string value)
    {
#if EPP_USES_EDITOR_PREFS
        EditorPrefs.SetString(editorPrefsPrefix + key, value);
#else
        PlayerPrefs.SetString(key, value);
#endif
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
#if EPP_USES_EDITOR_PREFS
        return EditorPrefs.GetBool(editorPrefsPrefix + key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) > 0;
#endif
    }
    public static void SetBool(string key, bool value)
    {
#if EPP_USES_EDITOR_PREFS
        EditorPrefs.SetBool(editorPrefsPrefix + key, value);
#else
        PlayerPrefs.SetInt(key, value ? 1 : 0);
#endif
    }
}
#endif
