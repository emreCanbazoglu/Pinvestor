#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor;

namespace AssetVariants
{
    [InitializeOnLoad]
    internal sealed class DefineSetter
    {
        private const string ASSET_VARIANTS = "ASSET_VARIANTS";
        private const string ODIN_INSPECTOR = "ODIN_INSPECTOR";
        private const string ASSET_VARIANTS_DO_ODIN_PROPERTIES = "ASSET_VARIANTS_DO_ODIN_PROPERTIES";

        static DefineSetter()
        {
            Update();
        }

        private static bool UpdateDefine(List<string> list, bool shouldDefine, string define)
        {
            bool alreadyDefined = list.Contains(define);

            if (shouldDefine && !alreadyDefined)
            {
                list.Add(define);
                return true;
            }

            if (!shouldDefine && alreadyDefined)
            {
                list.Remove(define);
                return true;
            }

            return false;
        }

        internal static void Update()
        {
            var targets = Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(x => x != BuildTargetGroup.Unknown)
                .Where(x => !IsObsolete(x));

            foreach (var target in targets)
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

                var list = defines.Split(';', ' ')
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                bool dirty = false;

                if (!list.Contains(ASSET_VARIANTS))
                {
                    list.Add(ASSET_VARIANTS);
                    dirty = true;
                }

                //TODO: see if 3.0 works as well
                bool shouldDefine = list.Contains(ODIN_INSPECTOR) &&
                    !EditorProjectPrefs.GetBool(AVSettings.disable_ASSET_VARIANTS_DO_ODIN_PROPERTIES_key, false);
                if (UpdateDefine(list, shouldDefine, ASSET_VARIANTS_DO_ODIN_PROPERTIES))
                    dirty = true;

                if (dirty)
                {
                    defines = list.Aggregate((a, b) => a + ";" + b);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                }
            }
        }

        private static bool IsObsolete(BuildTargetGroup group)
        {
            var attrs = typeof(BuildTargetGroup)
                .GetField(group.ToString())
                .GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attrs != null && attrs.Length > 0;
        }
    }
}
#endif
