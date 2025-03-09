#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEngine;

namespace PrecisionCats
{
    [Serializable]
    public class StringTypeFilter : Filter<string>
    {
        private TypeFilter typeFilter;
        public TypeFilter TypeFilter
        {
            get
            {
                if (typeFilter == null)
                {
                    typeFilter = new TypeFilter()
                    {
                        useBlacklist = useBlacklist,
                        blacklist = Convert(blacklist),
                        inheritedBlacklist = Convert(inheritedBlacklist),

                        useWhitelist = useWhitelist,
                        whitelist = Convert(whitelist),
                        inheritedWhitelist = Convert(inheritedWhitelist),
                    };
                }
                return typeFilter;
            }
        }

        public void ClearTypeFilter()
        {
            typeFilter = null;
        }

        public static List<Type> Convert(List<string> typeFullNames, bool addNull = false)
        {
            var types = new List<Type>();
            for (int i = 0; i < typeFullNames.Count; i++)
            {
                var name = typeFullNames[i];
                var type = !string.IsNullOrEmpty(name) ? AVCommonHelper.FindType(name) : null;
                if (addNull || type != null)
                    types.Add(type);
            }
            return types;
        }

        public override bool ShouldIgnore(Type targetType)
        {
            return TypeFilter.ShouldIgnore(targetType);
        }
    }

    public class TypeFilter : Filter<Type>
    {
        private List<TypeFilter> typeFilters = new List<TypeFilter>();
        public IEnumerable<TypeFilter> TypeFilters => typeFilters;
        //TODO2: should this instead add to the lists themselves depending on the union of useWL/useBL, and add dependencies as a list, and update dependency trees?
        /// <summary>
        /// (This will not include the typeFilters added to other)
        /// This will "synchronize" because it works by adding this TypeFilter to a list.
        /// This behaves mostly as if you were to add to the lists directly in this TypeFilter,
        /// in that it's not necessary that a type exists in every whitelist.
        /// However, useWhitelist and useBlacklist are individual,
        /// and if a type cannot be found in any of the whitelists or blacklists,
        /// then it will ignore the type if any of the typeFilters has useWhitelist=true.
        /// </summary>
        public void Add(TypeFilter other)
        {
            typeFilters.Remove(other);
            typeFilters.Add(other);
        }
        /// <summary>
        /// (This will not include the typeFilters added to other)
        /// </summary>
        public void Remove(TypeFilter other)
        {
            if (other != this)
                typeFilters.Remove(other);
        }

        public TypeFilter()
        {
            typeFilters.Add(this);
        }

        public override bool ShouldIgnore(Type targetType)
        {
            for (int i = 0; i < typeFilters.Count; i++)
            {
                var tf = typeFilters[i];
                if (tf.useBlacklist && tf.blacklist.Contains(targetType))
                    return true;
            }
            for (int i = 0; i < typeFilters.Count; i++)
            {
                var tf = typeFilters[i];
                if (tf.useWhitelist && tf.whitelist.Contains(targetType))
                    return false;
            }

            //bool search = false;
            //for (int i = 0; i < typeFilters.Count; i++)
            //{
            //    var tf = typeFilters[i];
            //    if (tf.useBlacklist && tf.inheritedBlacklist.Count > 0)
            //    {
            //        search = true;
            //        break;
            //    }
            //    if (tf.useWhitelist && tf.inheritedWhitelist.Count > 0)
            //    {
            //        search = true;
            //        break;
            //    }
            //}
            //if (search)
            {
                for (Type type = targetType; type != null; type = type.BaseType)
                {
                    for (int i = 0; i < typeFilters.Count; i++)
                    {
                        var tf = typeFilters[i];
                        if (tf.useBlacklist && tf.inheritedBlacklist.Contains(type))
                            return true;
                    }
                    for (int i = 0; i < typeFilters.Count; i++)
                    {
                        var tf = typeFilters[i];
                        if (tf.useWhitelist && tf.inheritedWhitelist.Contains(type))
                            return false;
                    }
                }
            }

            for (int i = 0; i < typeFilters.Count; i++)
                if (typeFilters[i].useWhitelist)
                    return true;
            return false;
        }
    }

    public abstract class Filter<T>
    {
        [Tooltip("If blacklist and inheritedBlacklist should be used.")]
        public bool useBlacklist = true;
        [Space(2)]
        public List<T> blacklist = new List<T>();
        [Tooltip("Like blacklist, but children class types are also blocked.")]
        public List<T> inheritedBlacklist = new List<T>();

        [Space(4)]

        [Tooltip("If whitelist and inheritedWhitelist should be used. If this is false then only blacklisted types will be blocked.")]
        public bool useWhitelist = false;
        [Space(2)]
        public List<T> whitelist = new List<T>();
        [Tooltip("Like whitelist, but children class types are also let through.")]
        public List<T> inheritedWhitelist = new List<T>();

        public abstract bool ShouldIgnore(Type targetType);
    }
}
#endif
