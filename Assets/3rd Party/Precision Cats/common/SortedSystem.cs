#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SystemPrefs;

namespace PrecisionCats
{
    public abstract class SortedSystem<T> : IComparable<T>
    where T : SortedSystem<T>
    {
        public class ReplacerTab : SystemPrefsWindow.Tab
        {
            public ReplacerTab()
            {
                useGroups = false;
            }

            protected override void CreateElements(List<SystemPrefsAsset> assets)
            {
                for (int i = 0; i < sses.Count; i++)
                    elements.Add(CreateKey(sses[i].SystemKey));
            }
        }

        /// <summary>
        /// You can override this to false to discard the default Activator.CreateInstance() one.
        /// </summary>
        protected virtual bool CreateDefault => true;

        /// <summary>
        /// For use with SystemPrefsUtility
        /// With ".Active" suffix it determines if this replacer should be skipped or not.
        /// With ".Priority" suffix, it determines the priority, the call order. Lower priority gets Replace() called first, as such a higher priority can clear replacements of lower priority.
        /// This has to be unique for every tree. That means that if multiple replacers share the same key,
        /// then for every tree's input combination there can be only one which does anything to the tree in Replace() or PostProcess(). There can't be overlap. (That restriction is to avoid incompatibility).
        /// </summary>
        public abstract string SystemKey { get; }

        public int CompareTo(T other)
        {
            return cachedPriority.CompareTo(other.cachedPriority);
        }
        public bool cachedActive;
        public int cachedPriority;

        private static List<T> scriptSSes = null;
        public static readonly List<T> customSSes = new List<T>();
        public static readonly List<T> sses = new List<T>();
        public static void RemoveCustomSS(T sortedSystem) //TODO: have this dirty?
        {
            customSSes.Remove(sortedSystem);
            sses.Remove(sortedSystem);
        }
        public static void AddCustomSS(T sortedSystem, bool sortSSes = true)
        {
            customSSes.Add(sortedSystem);
            sses.Add(sortedSystem);
            if (sortSSes)
                SortSSes();
        }
        /// <summary>
        /// You should have no need to call this yourself.
        /// </summary>
        public static void UpdateSSes()
        {
            if (scriptSSes == null)
            {
                scriptSSes = new List<T>();

                //foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                //{
                // 2019.2+
                foreach (Type t in UnityEditor.TypeCache.GetTypesDerivedFrom<T>()) //a.GetTypes())
                {
                    if (!t.IsValueType && !t.IsAbstract && !t.IsInterface && !t.IsGenericType) //?
                    {
                        //if (typeof(RT).IsAssignableFrom(t))
                        //{
                        var replacer = (T)Activator.CreateInstance(t);
                        if (replacer.CreateDefault && replacer.SystemKey != null)
                            scriptSSes.Add(replacer);
                        //}
                    }
                }
                //}
            }

            sses.Clear();
            sses.AddRange(scriptSSes);
            sses.AddRange(customSSes);

            SortSSes();

            SystemPrefsUtility.OnChanged -= OnChanged;
            SystemPrefsUtility.OnChanged += OnChanged;
        }
        public static event Action rebuild;
        private static void OnChanged(string key)
        {
            //UnityEngine.Debug.Log("OnChanged: " + key);
            for (int i = 0; i < sses.Count; i++)
            {
                if (sses[i].SystemKey == key)
                {
                    ssesDirty = true;
                    return;
                }
            }
        }

        public static void DirtySSes()
        {
            ssesDirty = true;
        }

        private static bool ssesDirty = false;
        public static void FlushRebuildSSes()
        {
            if (ssesDirty)
            {
                ssesDirty = false;

                SortSSes();
                if (rebuild != null)
                    rebuild();
            }
        }

        public static void SortSSes()
        {
            for (int i = 0; i < sses.Count; i++)
            {
                var r = sses[i];
                string key = r.SystemKey;
                if (string.IsNullOrEmpty(key))
                {
                    r.cachedActive = false;
                }
                else
                {
                    r.cachedActive = SystemPrefsUtility.GetActive(key);
                    r.cachedPriority = SystemPrefsUtility.GetPriority(key);
                }
            }

            sses.Sort();
        }
    }
}
#endif
