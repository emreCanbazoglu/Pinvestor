#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace PrecisionCats
{
    public static class ManagedReferencesHelper
    {
#if UNITY_2021_2_OR_NEWER
        //TODO: move to pdrsettings?
        public static bool allowMultipleNonCylicalReferences = true;
#else
        /// <summary>
        /// To prevent infinite loops with cyclical graphs, in versions older than 2021.2, which added managedReferenceId.
        /// </summary>
        public static int maxManagedReferenceDepth = 10;
#endif

#if UNITY_2021_2_OR_NEWER
        private static readonly Stack<long> managedReferenceStack = new Stack<long>();
        private static readonly HashSet<long> visitedManagedReferences = new HashSet<long>();
#else
        private static int managedReferenceDepth;
#endif

        public static void InitSkipDuplicates()
        {
#if UNITY_2021_2_OR_NEWER
            if (allowMultipleNonCylicalReferences)
                managedReferenceStack.Clear();
            else
                visitedManagedReferences.Clear();
#else
            managedReferenceDepth = 0;
#endif
        }

        public static bool SkipDuplicate(SerializedProperty prop, out int prevDepth)
        {
#if UNITY_2021_2_OR_NEWER
            prevDepth = default(int);
#else
            prevDepth = managedReferenceDepth;
#endif
#if UNITY_2019_3_OR_NEWER
            if (prop.propertyType == SerializedPropertyType.ManagedReference)
            {
#if UNITY_2021_2_OR_NEWER
                long refId = prop.managedReferenceId;
                if (allowMultipleNonCylicalReferences)
                {
                    if (managedReferenceStack.Contains(refId))
                        return true;
                    managedReferenceStack.Push(refId);
                    prevDepth = 1337;
                }
                else
                {
                    if (!visitedManagedReferences.Add(refId))
                        return true;
                }
#else
                managedReferenceDepth++;
                if (managedReferenceDepth > maxManagedReferenceDepth)
                    return true;
#endif
            }
#endif
            return false;
        }

        public static void RestoreSkipDuplicateDepth(int prevDepth)
        {
#if UNITY_2021_2_OR_NEWER
            if (prevDepth == 1337)
                managedReferenceStack.Pop();
#else
            managedReferenceDepth = prevDepth;
#endif
        }
    }
}
#endif
