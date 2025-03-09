#if UNITY_EDITOR
using UnityEngine;

namespace PrecisionCats
{
    /// <summary>
    /// A shared filter that can be added to another TypeFilter. Changes to this filter will "synchronize" to the other filter.
    /// </summary>
    public static class ProjectSettingsFiltering
    {
        public static readonly TypeFilter filter = new TypeFilter();

        static ProjectSettingsFiltering()
        {
            filter.inheritedBlacklist.Add(AVCommonHelper.FindType("UnityEditorInternal.ProjectSettingsBase"));
            var bl = filter.blacklist;
            bl.Add(AVCommonHelper.FindType("UnityEditor.PlayerSettings"));
            bl.Add(AVCommonHelper.FindType("UnityEditor.EditorSettings"));
            bl.Add(typeof(QualitySettings));
#if UNITY_2020_1_OR_NEWER
            bl.Add(AVCommonHelper.FindType("UnityEditor.VersionControlSettings")); //bl.Add(typeof(VersionControlSettings));
#endif
        }
    }
}
#endif
