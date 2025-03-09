#if UNITY_EDITOR

#if !ODIN_INSPECTOR
#undef ASSET_VARIANTS_DO_ODIN_PROPERTIES
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
#endif
#if !ASSET_VARIANTS_DO_ODIN_PROPERTIES
using Property = UnityEditor.SerializedProperty;
#endif
using SP = UnityEditor.SerializedProperty;

namespace AssetVariants
{
    [Serializable]
    public struct AssetID // UnityEditor.Build.Content.ObjectIdentifier is problematic, needed to reimplement some of it
    {
        public enum AssetType
        {
            Main, // Shortcut
            Sub,
            Importer // AssetDatabase doesn't really consider an AssetImporter a sub asset
        }

        public string guid;
        public AssetType type;
        public long localId; // Only used if type == Type.Sub

        public Object Load()
        {
            var path = guid.ToPath();
            if (type == AssetType.Importer)
                return AssetImporter.GetAtPath(path);
            var mainAsset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (type == AssetType.Main)
                return mainAsset;
#if UNITY_2018_2_OR_NEWER // (Technically UNITY_2018_1 has TryGetGUIDAndLocalFileIdentifier, but with int localId)
            string _; long localId;
            if (mainAsset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mainAsset, out _, out localId))
            {
                if (localId == this.localId)
                {
                    return mainAsset;
                }
                else
                {
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    for (int i = 0; i < allAssets.Length; i++)
                    {
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(allAssets[i], out _, out localId))
                            if (localId == this.localId)
                                return allAssets[i];
                    }
                    Debug.LogWarning("Could not find a sub asset for " + mainAsset + " with the localId: " + localId + Helper.LOG_END);
                }
            }
#endif
            return null;
        }

        public static AssetType GetType(Object asset)
        {
            if (AssetDatabase.IsMainAsset(asset))
                return AssetType.Main;
            else if (Helper.TypeIsImporter(asset.GetType()))
                return AssetType.Importer;
            else
                return AssetType.Sub;
        }

        public static AssetID Get(Object asset)
        {
            if (asset == null)
                return Null;
            if (AssetDatabase.IsMainAsset(asset))
                return new AssetID(asset.GUID(), AssetType.Main, 0); //11400000
            if (Helper.TypeIsImporter(asset.GetType()))
                return new AssetID(asset.GUID(), AssetType.Importer, 0); //3
#if UNITY_2018_2_OR_NEWER // (Technically UNITY_2018_1 has TryGetGUIDAndLocalFileIdentifier, but with int localId)
            string guid;
            long localId;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out localId))
                return new AssetID(guid, AssetType.Sub, localId);
#endif
            return Null;
        }

        public AssetID(string guid, AssetType type, long localId)
        {
            this.guid = guid;
            this.type = type;
            this.localId = localId;
        }

        public static readonly AssetID Null = new AssetID() { guid = null };

        public static bool operator ==(AssetID x, AssetID y)
        {
            if (string.IsNullOrWhiteSpace(x.guid))
                return string.IsNullOrWhiteSpace(y.guid);
            return x.guid == y.guid && x.localId == y.localId;
        }
        public static bool operator !=(AssetID x, AssetID y)
        {
            return !(x == y);
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            if (type == AssetType.Importer)
                return guid + ".Importer";
            if (type == AssetType.Main)
                return guid + ".Main";
            return guid + "." + localId;
        }
    }

    internal struct PathState
    {
        /// <summary>
        /// I.e. the src is the iteration and dst is the finding, or vice versa (which is used for implicit overrides creation)
        /// </summary>
        public string iterationPropertyPath;
        public string findingPropertyPath;
        public string overridePath;
        public bool same;

        public string Path => overridePath;

        public static PathState Same(string path)
        {
            return new PathState()
            {
                iterationPropertyPath = path,
                findingPropertyPath = path,
                overridePath = path,
                same = true,
            };
        }

        public string GetLocalPath(string iterationChildPropertyPath)
        {
            return Helper.GetLocalPath(iterationPropertyPath, iterationChildPropertyPath);
        }

        public PathState Child(string iterationChildPropertyPath)
        {
            if (same)
                return Same(iterationChildPropertyPath);

            //Debug.Log(iterationOriginalPath + " -- " + iterationOriginalChildPath);
            var local = GetLocalPath(iterationChildPropertyPath);
            return new PathState()
            {
                iterationPropertyPath = iterationChildPropertyPath,
                findingPropertyPath = findingPropertyPath + local,
                overridePath = overridePath + local,
                same = false,
            };
        }
        public PathState Child(string iterationChildPropertyPath, string findingChildPropertyPath, string localChildOverridePath)
        {
            return new PathState()
            {
                iterationPropertyPath = iterationChildPropertyPath,
                findingPropertyPath = findingChildPropertyPath,
                overridePath = overridePath + localChildOverridePath,
                same = false,
            };
        }

        public override string ToString()
        {
            return "(iterationPropertyPath: " + iterationPropertyPath + ", findingPropertyPath: " + findingPropertyPath + ", overridePath: " + overridePath + ")";
        }
    }

    public struct PropertyKeyPair
    {
        public object property;
        public string key;
        public SP keySP;
        public PropertyKeyPair(object property, string key, SP keySP)
        {
            this.property = property;
            this.key = key;
            this.keySP = keySP;
        }

        public static void Dispose(List<PropertyKeyPair> propertyKeyPairs)
        {
            for (int i = 0; i < propertyKeyPairs.Count; i++)
            {
                var pair = propertyKeyPairs[i];
                if (pair.property is SP prop)
                    prop.Dispose();
                if (pair.keySP != null)
                    pair.keySP.Dispose();
            }
            propertyKeyPairs.Clear();
        }
    }
    /// <summary>
    /// Used for making Arrays/Lists/delimited strings behave like HashSets/Dictionaries in that their order does not matter
    /// </summary>
    public abstract class EmulationProperty
    {
        public string path;

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
        internal void GetChildren(SP property, List<PropertyKeyPair> children)
        {
            GetChildren(property.ToProperty(), children);
        }
        internal void ModifyChildren(SP property, List<string> removeKeys, List<object> addProperties)
        {
            ModifyChildren(property.ToProperty(), removeKeys, addProperties);
        }
#endif

        internal abstract void GetChildren(Property property, List<PropertyKeyPair> children);
        internal abstract void ModifyChildren(Property property, List<string> removeKeys, List<object> addProperties);

        public EmulationProperty(string path)
        {
            this.path = path;
        }
    }
    public abstract class BasicEmulationProperty : EmulationProperty
    {
        /// <summary>
        /// Relative to the element's propertyPath, not the List/Array's propertyPath.
        /// </summary>
        public virtual string RelativeKeyPath { get; }

        internal override void GetChildren(Property property, List<PropertyKeyPair> children)
        {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (property.IsOdin)
            {
                Debug.LogError("Unimplemented");
            }
            else
#endif
            {
                var sp = property.ToSP();

                if (!sp.isArray)
                {
                    Debug.LogWarning("");
                    return;
                }
                if (sp.arraySize == 0)
                    return;

                var relativeKeyPath = RelativeKeyPath;

                var iterator = sp.Copy();
                int depth = iterator.depth;

                if (iterator.Next(true) && iterator.Next(true) && iterator.Next(true)) // Skips array size
                {
                    do
                    {
                        if (relativeKeyPath != null)
                        {
                            var key = iterator.FindPropertyRelative(relativeKeyPath);
                            if (key != null)
                                children.Add(new PropertyKeyPair(iterator.Copy().ToProperty(), Helper.GetString(key), key));
                            else
                                Debug.LogWarning("? - " + iterator.propertyPath + " - " + relativeKeyPath);
                        }
                        else
                        {
                            children.Add(new PropertyKeyPair(iterator.Copy().ToProperty(), Helper.GetString(iterator), null)); //Debug.Log(Helper.GetString(iterator) + " - " + iterator.propertyType + " - " + iterator.propertyPath);
                        }
                    }
                    while (iterator.Next(false, false) && depth < iterator.depth);
                }
                iterator.Dispose();
            }
        }
        private static readonly List<int> removingChildren = new List<int>();
        internal override void ModifyChildren(Property property, List<string> removeKeys, List<object> addProperties)
        {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (property.IsOdin)
            {
                Debug.LogError("Unimplemented");
            }
            else
#endif
            {
                var sp = property.ToSP();

                if (!sp.isArray)
                {
                    Debug.LogWarning("");
                    return;
                }

                var relativeKeyPath = RelativeKeyPath;

                if (sp.arraySize > 0)
                {
                    removingChildren.Clear();

                    var iterator = sp.Copy();
                    int depth = iterator.depth;
                    if (iterator.Next(true) && iterator.Next(true) && iterator.Next(true)) // Skips array size
                    {
                        int id = 0;
                        do
                        {
                            var key = relativeKeyPath == null ? iterator.Copy() :
                                iterator.FindPropertyRelative(relativeKeyPath);
                            if (key != null)
                            {
                                for (int i = 0; i < removeKeys.Count; i++)
                                {
                                    if (Helper.GetString(key) == removeKeys[i]) //This is unfortunate that it's probably already been calculated before, but usually it shouldn't be slow JSON or whatever
                                    {
                                        removingChildren.Insert(0, id);
                                        break;
                                    }
                                }
                                key.Dispose();
                                id++;
                            }
                        }
                        while (iterator.Next(false, false) && depth < iterator.depth);
                    }
                    iterator.Dispose();

                    //Debug.Log(property.propertyPath + " removingChildren.Count: " + removingChildren.Count);

                    //int childCount = property.arraySize;
                    for (int i = 0; i < removingChildren.Count; i++)
                    {
                        var rc = removingChildren[i];
                        //if (rc < childCount)
                        //{
                        //Debug.Log(rc + "  " + property.arraySize);
                        sp.DeleteArrayElementAtIndex(rc);
                        //}
                        //else
                        //Debug.LogError("How?: " + childCount + " <= " + rc);
                    }
                    removingChildren.Clear();
                }

                for (int i = 0; i < addProperties.Count; i++)
                {
                    int newID = sp.arraySize;

                    if (addProperties[i] is Property add)
                    {
                        var addSP = add.ToSP();
                        if (addSP != null)
                        {
                            sp.InsertArrayElementAtIndex(newID);
                            var newElement = sp.GetArrayElementAtIndex(newID);
                            Helper.CopyValueRecursively(newElement, addSP);
                        }
                    }
                    else
                    {
#if UNITY_2022_1_OR_NEWER
                        try
                        {
                            sp.InsertArrayElementAtIndex(newID);
                            var newElement = sp.GetArrayElementAtIndex(newID);
                            newElement.boxedValue = addProperties[i];
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Couldn't set boxedValue.\n" + e);
                        }
#else
                        Debug.LogError("Unimplemented");
#endif
                    }
                }
            }
        }

        public BasicEmulationProperty(string path) : base(path) { }
    }
    /// <summary>
    /// Treats an Array/List like a Dictionary
    /// </summary>
    public class DictionaryEmulationProperty : BasicEmulationProperty
    {
        /// <summary>
        /// Relative to the element's propertyPath, not the List/Array's propertyPath.
        /// </summary>
        public string relativeKeyPath;

        public override string RelativeKeyPath => relativeKeyPath;

        public DictionaryEmulationProperty(string path, string relativeKeyPath) : base(path)
        {
            this.relativeKeyPath = relativeKeyPath;
        }
    }
    /// <summary>
    /// Treats an Array/List like a HashSet
    /// </summary>
    public class HashSetEmulationProperty : BasicEmulationProperty
    {
        public override string RelativeKeyPath => null;

        public HashSetEmulationProperty(string path) : base(path) { }
    }
    /// <summary>
    /// Treats a string as a "HashSet" of strings separated by the delimiter
    /// </summary>
    public class StringHashSetEmulationProperty : EmulationProperty
    {
        /// <summary>
        /// The first one is the one used long term
        /// </summary>
        public string[] delimiters;
        public StringSplitOptions stringSplitOptions = StringSplitOptions.RemoveEmptyEntries;

        private static string GetString(Property property)
        {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (property.IsOdin)
            {
                Debug.LogError("Unimplemented");
                return null;
            }
            else
#endif
            {
                var sp = property.ToSP();

                if (sp.propertyType != SerializedPropertyType.String)
                {
                    Debug.LogWarning("");
                    return null;
                }

                return sp.stringValue;
            }
        }
        private static void SetString(Property property, string value)
        {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (property.IsOdin)
            {
                Debug.LogError("Unimplemented");
            }
            else
#endif
            {
                var sp = property.ToSP();

                if (sp.propertyType != SerializedPropertyType.String)
                {
                    Debug.LogWarning("");
                    return;
                }

                sp.stringValue = value;
            }
        }

        internal override void GetChildren(Property property, List<PropertyKeyPair> children)
        {
            string s = GetString(property);
            if (s == null)
                return;
            var strings = s.Split(delimiters, stringSplitOptions);

            for (int i = 0; i < strings.Length; i++)
                children.Add(new PropertyKeyPair(strings[i], Helper.TextString(strings[i]), null));
        }
        internal override void ModifyChildren(Property property, List<string> removeKeys, List<object> addProperties)
        {
            string s = GetString(property);
            if (s == null)
                return;
            var strings = new List<string>(s.Split(delimiters, stringSplitOptions));

            if (removeKeys.Count > 0)
            {
                for (int i = strings.Count - 1; i >= 0; i--)
                    if (removeKeys.Contains(Helper.TextString(strings[i])))
                        strings.RemoveAt(i);
            }

            for (int i = 0; i < addProperties.Count; i++)
            {
                var add = addProperties[i];
                if (add is string str)
                    strings.Add(str);
                else if (add is Property addProp && addProp.ToSP() is SP sp && sp.propertyType == SerializedPropertyType.String)
                    strings.Add(sp.stringValue);
                else
                    Debug.LogError("Unimplemented");
            }

            string sum = "";
            for (int i = 0; i < strings.Count; i++)
            {
                sum += strings[i];
                if (i != strings.Count - 1)
                    sum += delimiters[0];
            }

            SetString(property, sum);
        }

        public StringHashSetEmulationProperty(string path, params string[] delimiters) : base(path)
        {
            this.delimiters = delimiters;
        }
    }
    /// <summary>
    /// The one used when the dst is using an emulation property but the src is not.
    /// When the src, it functions essentially the same as HashSetEmulationProperty does
    /// </summary>
    internal class DefaultEmulationProperty : EmulationProperty
    {
        public static readonly DefaultEmulationProperty instance = new DefaultEmulationProperty();

        internal override void GetChildren(Property property, List<PropertyKeyPair> children)
        {
#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
            if (property.IsOdin)
            {
                Debug.LogError("Unimplemented");
            }
            else
#endif
            {
                var sp = property.ToSP();

                if (!sp.isArray)
                {
                    Debug.LogWarning("");
                    return;
                }

                var iterator = sp.Copy();
                int depth = iterator.depth;
                if (iterator.Next(true) && iterator.Next(true))
                {
                    do
                    {
                        children.Add(new PropertyKeyPair(iterator.Copy().ToProperty(), Helper.GetString(iterator), null));
                    }
                    while (iterator.Next(false, false) && depth < iterator.depth);
                }
                iterator.Dispose();
            }
        }

        /// <summary>
        /// (Never used as the dst)
        /// </summary>
        internal override void ModifyChildren(Property property, List<string> removeKeys, List<object> addProperties)
        {
            throw new Exception("This should never happen");
        }

        public DefaultEmulationProperty() : base(null) { }
    }


    public class PropertyFilter
    {
        public List<string> ignoreNameEquals = new List<string>();
        /// <summary>
        /// For performance concerns this does not support ignoring children of an array
        /// </summary>
        public List<string> ignorePathStartsWith = new List<string>()
        {
            "m_Name", // TODO: not importers?
            "m_EditorClassIdentifier", // TODO: is this only for MonoBehaviour?
            "m_Script", //TODO: for ScriptableObject only
#if ODIN_INSPECTOR
            "serializationData", //TODO: for SerializableScriptableObject only?
#endif
        };
        public List<string> ignorePathRegexPatterns = new List<string>();
        public bool ShouldIgnore(string name, string path, bool enteredArray = false)
        {
            for (int i = 0; i < ignoreNameEquals.Count; i++) // TODO2: could the names differ in Odin?
                if (name == ignoreNameEquals[i])
                    return true;

            if (path != null)
            {
                if (!enteredArray)
                {
                    for (int i = 0; i < ignorePathStartsWith.Count; i++)
                        if (path.StartsWith(ignorePathStartsWith[i]))
                            return true;
                }

                for (int i = 0; i < ignorePathRegexPatterns.Count; i++)
                    if (System.Text.RegularExpressions.Regex.IsMatch(path, ignorePathRegexPatterns[i]))
                        return true;
            }

            return false;
        }

        public List<EmulationProperty> emulationProperties = new List<EmulationProperty>();
        public EmulationProperty GetEmulationProperty(string path)
        {
            for (int i = 0; i < emulationProperties.Count; i++)
            {
                if (emulationProperties[i].path == path)
                    return emulationProperties[i];
            }
            return null;
        }
        public static void AddEmulationProperty(Type assetType, EmulationProperty property, bool inherited)
        {
            var filter = inherited ? GetInherited(assetType) : Get(assetType);
            if (filter.GetEmulationProperty(property.path) == null)
            {
                filter.emulationProperties.Add(property);

                if (inherited)
                {
                    foreach (var kv in propertyFilters)
                        if (assetType.IsAssignableFrom(kv.Key))
                            kv.Value.emulationProperties.Add(property);
                }
            }
            else
                Debug.LogError("Already has a dictionary emulation");
        }
        public static bool RemoveEmulationProperty(Type assetType, string path, bool inherited, out EmulationProperty removed)
        {
            var filter = inherited ? GetInherited(assetType) : Get(assetType);

            for (int i = 0; i < filter.emulationProperties.Count; i++)
            {
                var ep = filter.emulationProperties[i];
                if (ep.path == path)
                {
                    if (inherited)
                    {
                        foreach (var kv in propertyFilters)
                            if (assetType.IsAssignableFrom(kv.Key))
                                kv.Value.emulationProperties.Remove(ep);
                    }
                    filter.emulationProperties.RemoveAt(i);
                    removed = ep;
                    return true;
                }
            }

            removed = null;
            return false;
        }

        private static readonly Dictionary<Type, PropertyFilter> inheritedPropertyFilters = new Dictionary<Type, PropertyFilter>();
        private static PropertyFilter GetInherited(Type assetType)
        {
            PropertyFilter filter;
            if (!inheritedPropertyFilters.TryGetValue(assetType, out filter))
            {
                filter = new PropertyFilter();
                inheritedPropertyFilters.Add(assetType, filter);
            }
            return filter;
        }

        public static void InheritedIgnorePropertyName(Type assetType, string fullName)
        {
            var filter = GetInherited(assetType);
            if (!filter.ignoreNameEquals.Contains(fullName))
            {
                filter.ignoreNameEquals.Add(fullName);

                foreach (var kv in propertyFilters)
                    if (assetType.IsAssignableFrom(kv.Key))
                        kv.Value.ignoreNameEquals.Add(fullName);
            }
        }
        public static void InheritedIgnorePropertyPath(Type assetType, string absolutePathStart)
        {
            var filter = GetInherited(assetType);
            if (!filter.ignorePathStartsWith.Contains(absolutePathStart))
            {
                filter.ignorePathStartsWith.Add(absolutePathStart);

                foreach (var kv in propertyFilters)
                    if (assetType.IsAssignableFrom(kv.Key))
                        kv.Value.ignorePathStartsWith.Add(absolutePathStart);
            }
        }
        public static void InheritedIgnorePropertyPathRegex(Type assetType, string regexPattern)
        {
            var filter = GetInherited(assetType);
            if (!filter.ignorePathRegexPatterns.Contains(regexPattern))
            {
                filter.ignorePathRegexPatterns.Add(regexPattern);

                foreach (var kv in propertyFilters)
                    if (assetType.IsAssignableFrom(kv.Key))
                        kv.Value.ignorePathRegexPatterns.Add(regexPattern);
            }
        }

        public static void IgnorePropertyName(Type assetType, string fullName)
        {
            var filter = Get(assetType);
            filter.ignoreNameEquals.Remove(fullName);
            filter.ignoreNameEquals.Add(fullName);
        }
        public static void IgnorePropertyPath(Type assetType, string absolutePathStart)
        {
            if (absolutePathStart.Contains(".Array.data["))
            {
                Debug.LogWarning("Ignores created with IgnorePropertyPath() are skipped for elements in an array. This path will not be ignored: " + absolutePathStart + "\nYou can use IgnorePropertyName or IgnorePropertyPathRegex instead." + Helper.LOG_END);
                return;
            }

            var filter = Get(assetType);
            filter.ignorePathStartsWith.Remove(absolutePathStart);
            filter.ignorePathStartsWith.Add(absolutePathStart);
        }
        public static void IgnorePropertyPathRegex(Type assetType, string regexPattern)
        {
            var filter = Get(assetType);
            filter.ignorePathRegexPatterns.Remove(regexPattern);
            filter.ignorePathRegexPatterns.Add(regexPattern);
        }

        public static readonly Dictionary<Type, PropertyFilter> propertyFilters = new Dictionary<Type, PropertyFilter>();
        public static PropertyFilter Get(Type assetType)
        {
            PropertyFilter filter;
            if (!propertyFilters.TryGetValue(assetType, out filter))
            {
                filter = new PropertyFilter();

                if (Helper.TypeIsImporter(assetType))
                    filter.ignorePathStartsWith.Add("m_UserData");

                foreach (var kv in inheritedPropertyFilters)
                {
                    if (kv.Key.IsAssignableFrom(assetType))
                    {
                        filter.ignoreNameEquals.AddRange(kv.Value.ignoreNameEquals);
                        filter.ignorePathStartsWith.AddRange(kv.Value.ignorePathStartsWith);
                        filter.ignorePathRegexPatterns.AddRange(kv.Value.ignorePathRegexPatterns);
                        filter.emulationProperties.AddRange(kv.Value.emulationProperties);
                    }
                }

                propertyFilters.Add(assetType, filter);
            }
            return filter;
        }
    }

#if ODIN_INSPECTOR
    /// <summary>
    /// Just a static class with the same name as the generic, storing its active state (whether it should return true in CanResolveForPropertyFilter).
    /// </summary>
    public static class RawPropertyResolver
    {
        /// <summary>
        /// This will be true when AVEditor is initializing/drawing, and when AV is calling RevertAssetFromParentAsset/CreateOverridesFromDifferences
        /// </summary>
        public static bool active = false;

        internal static PropertyTree currentTree = null;
        internal static AVTargets treeAVTargets = null;
    }

    /// <summary>
    /// RectPropertyResolver uses the fake property paths: .position.xy,.size.xy instead of .x,.y,.width,.height
    /// This is a problem because the paths do not match the underlying data of the field.
    /// This is a generic replacement for such problematic PropertyResolvers.
    /// It is a  ProcessedMemberPropertyResolver with [ResolverPriority(1000)]
    /// which also replaces the SerializationBackend of its children with its own
    /// (fixing what I believe is a bug in Odin where e.g. RectInt's children are normally SerializationBackend.None and therefore Asset Variants would show no override buttons for it).
    /// Simply inherit from this class like so: 
    /// public class RectRPR : RawPropertyResolver&lt;Rect&gt; { },
    /// but with another class that has a problematic OdinPropertyResolver, and it might show override buttons for it.
    /// </summary>
    [ResolverPriority(1000)] //double.MaxValue
    public abstract class RawPropertyResolver<T> : ProcessedMemberPropertyResolver<T>
    {
        public override bool CanResolveForPropertyFilter(InspectorProperty property)
        {
            if (!base.CanResolveForPropertyFilter(property))
                return false;
            if (RawPropertyResolver.active)
                return true;
            var tree = property.Tree;
            if (RawPropertyResolver.currentTree != tree)
            {
                RawPropertyResolver.currentTree = tree;
                var so = tree.UnitySerializedObject;
                if (so != null && AVFilter.Should(so.targetObject))
                    RawPropertyResolver.treeAVTargets = AVTargets.Get(so);
                else
                    RawPropertyResolver.treeAVTargets = null;
            }
            if (RawPropertyResolver.treeAVTargets == null)
                return false;
            return RawPropertyResolver.treeAVTargets.AnyHasParent
                && !RawPropertyResolver.treeAVTargets.propertyFilter.ShouldIgnore(property.Name,
                RawPropertyResolver.treeAVTargets.GetOverridePath(property, tree.UnitySerializedObject));
        }

        protected override InspectorPropertyInfo[] GetPropertyInfos()
        {
            var sb = Property.ValueEntry.SerializationBackend;
            var infos = base.GetPropertyInfos();
            for (int i = 0; i < infos.Length; i++)
            {
                var old = infos[i];
                infos[i] = InspectorPropertyInfo.CreateValue
                    (old.PropertyName, old.Order, sb, old.GetGetterSetter(), old.Attributes);
            }
            return infos;
        }
    }

    public class RectRPR : RawPropertyResolver<Rect> { }
    public class RectIntRPR : RawPropertyResolver<RectInt> { }
    public class BoundsRPR : RawPropertyResolver<Bounds> { }
    public class BoundsIntRPR : RawPropertyResolver<BoundsInt> { }
#endif

#if ASSET_VARIANTS_DO_ODIN_PROPERTIES
    internal class Property : IDisposable
    {
        public SP unityProperty;

        public bool IsUnity => unityProperty != null;
        public bool IsOdin => odinProperty != null;

        public InspectorProperty odinProperty;
        public int odinDepth = -1;
        private bool odinDontDispose = false;
        public bool odinIsSize = false; // Odin does not have Array/List size as a property

        public bool CompareType(Property other)
        {
            if (IsUnity)
                return Helper.PropertyTypesMatch(unityProperty.propertyType, other.unityProperty.propertyType);
            else
                return odinProperty.Info.TypeOfValue == other.odinProperty.Info.TypeOfValue;
        }

        public int depth => IsUnity ? unityProperty.depth : odinDepth;
        public string name => IsUnity ? unityProperty.name : odinProperty.Name;
        public string propertyPath
        {
            get
            {
                if (IsUnity)
                    return unityProperty.propertyPath;

                if (odinIsSize)
                    return odinProperty.UnityPropertyPath + Helper.ARRAY_SIZE_SUFFIX;
                return odinProperty.UnityPropertyPath;
            }
        }
        public bool editable => IsUnity ? unityProperty.editable : odinProperty.Info.IsEditable;

        public void Dispose()
        {
            if (IsUnity)
            {
                unityProperty.Dispose();
            }
            else
            {
                if (!odinDontDispose && odinProperty != null)
                    odinProperty.Dispose();
            }
        }

        public Property Copy(bool copyIsOwner = false)
        {
            if (IsUnity)
            {
                return new Property()
                {
                    unityProperty = unityProperty.Copy(),
                    odinProperty = null,
                };
            }

            var copy = new Property()
            {
                unityProperty = null,
                odinProperty = odinProperty,
                odinIsSize = odinIsSize,
                odinDepth = odinDepth,
                odinDontDispose = true
            };
            if (copyIsOwner)
            {
                copy.odinDontDispose = odinDontDispose;
                odinDontDispose = true;
            }
            return copy;
        }

        public void FindDepth()
        {
            if (IsOdin)
            {
                odinDepth = 0;
                var path = odinProperty.Path;
                int l = path.Length;
                for (int i = 0; i < l; i++)
                {
                    if (path[i] == '.')
                        odinDepth++;
                }
            }
        }

        public bool NextVisible(bool enterChildren, bool skipInternalOnInspectorGUI = false)
        {
            if (IsUnity)
                return unityProperty.NextVisible(enterChildren);

            if (!OdinNextProperty(enterChildren, true))
                return false;
            if (skipInternalOnInspectorGUI && propertyPath == "InternalOnInspectorGUI")
            {
                if (!OdinNextProperty(enterChildren, true))
                    return false;
            }
            return true;
        }
        public bool Next(bool enterChildren)
        {
            if (IsUnity)
                return unityProperty.Next(enterChildren);
            return OdinNextProperty(enterChildren, false);
        }
        public bool NextChildSkipArray()
        {
            if (IsUnity)
                return unityProperty.NextChildSkipArray();
            return OdinNextProperty(true, false);
        }
        private bool OdinNextProperty(bool enterChildren, bool visibleOnly = true)
        {
            if (enterChildren && !odinIsSize && odinProperty.IsCollectionWithSize())
            {
                // TODO2: no odin serialized collection has an actual size property right?
                odinDepth++;
                odinIsSize = true;
                return true;
            }
            else
            {
                var prev = odinProperty;
                if (odinIsSize)
                {
                    odinIsSize = false;
                    odinProperty = odinProperty.NextProperty(true, visibleOnly); // Moves to children, because property is stored as the parent property when isSize
                    if (odinProperty != null && odinProperty.Parent != prev)
                        FindDepth();
                }
                else if (enterChildren && Helper.HasChildren(this))
                {
                    odinDepth++;
                    odinProperty = odinProperty.NextProperty(true, visibleOnly);
                    if (odinProperty != null && odinProperty.Path.CustomEndsWith("#key")) //To skip the key subproperty of dictionaries' keyvaluepairs
                    {
                        //Debug.Log("Skipped Key: " + odinProperty.Path);

                        odinProperty = odinProperty.NextProperty(false, visibleOnly);
                        if (odinProperty != null && odinProperty.Parent != prev.Parent)
                            FindDepth();
                    }
                }
                else
                {
                    odinProperty = odinProperty.NextProperty(false, visibleOnly);
                    if (odinProperty != null && odinProperty.Parent != prev.Parent)
                        FindDepth();
                }
                if (!odinDontDispose)
                    prev.Dispose();
                else
                    odinDontDispose = false;
                return odinProperty != null;
            }
        }
    }
#endif

    internal class PathConversionCache
    {
        public HashSet<string> keyPropertyPaths;
        public Dictionary<string, string> propertyPathToOverridePath;
        public Dictionary<string, string> overridePathToPropertyPath; //This one when used does not gain from the caching, but rather loses by being filled all the way up. Using this for that case is for simplicity sake.

        public SerializedObject conversionSO; // I don't know if this could ever change, but it's best to be safe.

        /// <summary>
        /// Can return null
        /// </summary>
        public string ConvertToOverridePath(string propertyPath, SerializedObject so, PropertyFilter pf)
        {
            return Convert(propertyPath, ref propertyPathToOverridePath, false, so, pf);
        }
        /// <summary>
        /// Can return null
        /// </summary>
        public string ConvertToPropertyPath(string overridePath, SerializedObject so, PropertyFilter pf)
        {
            return Convert(overridePath, ref overridePathToPropertyPath, true, so, pf);
        }

        private string Convert(string inputPath, ref Dictionary<string, string> remap, bool inverted, SerializedObject so, PropertyFilter pf)
        {
            if (pf.emulationProperties.Count == 0)
                return inputPath;

            if (remap == null)
            {
                conversionSO = so;
                remap = new Dictionary<string, string>();
            }
            else if (conversionSO != so)
            {
                // This will be slow, as if it hits this branch it likely will alternate back and forth negating the cache's efficiency by redoing it repeatedly.

                conversionSO = so;
                remap.Clear();
            }

            //TODO:: optimize by only really doing anything when entered an emulation? same=false?

            string overridePath;
            if (!remap.TryGetValue(inputPath, out overridePath))
            {
                //TODO: should it clear propertyPathToOverridePath?

                // Completely redoes it. TODO:: optimize it so it only does the relevant ones

                var iterator = so.GetIterator();
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        Convert(remap, inverted, PathState.Same(iterator.propertyPath), iterator, pf);
                    }
                    while (iterator.Next(false));
                }

                if (!remap.TryGetValue(inputPath, out overridePath))
                {
                    overridePath = remap[inputPath] = null;
                    //Debug.LogWarning("? " + inputPath);
                }
            }
            return overridePath;
        }

        private static readonly List<PropertyKeyPair> propertyKeyPairs = new List<PropertyKeyPair>();
        private void Convert(Dictionary<string, string> remap, bool inverted, PathState pathState, SP prop, PropertyFilter pf)
        {
            var path = pathState.Path;

            if (inverted)
                remap[path] = pathState.iterationPropertyPath;
            else
                remap[pathState.iterationPropertyPath] = path;

            //if (prop == null)
            //    return;
            if (!Helper.HasChildren(prop))
                return;

            var emulation = pf.GetEmulationProperty(path);
            if (emulation != null)
            {
                propertyKeyPairs.Clear();

                emulation.GetChildren(prop, propertyKeyPairs);

                //Convert(, null, pf); + ".Array.size"

                for (int i = 0; i < propertyKeyPairs.Count; i++)
                {
                    var pair = propertyKeyPairs[i];
                    var key = pair.key;
                    if (pair.property is Property childProp)
                    {
                        var childSP = childProp.ToSP();
                        if (childSP != null)
                        {
                            var childPathState = pathState.Child(childProp.propertyPath, null, Helper.LocalKeyPath(key));
                            Convert(remap, inverted, childPathState, childSP, pf);
                        }

                        childProp.Dispose();
                    }
                    if (pair.keySP != null)
                    {
                        if (keyPropertyPaths == null)
                            keyPropertyPaths = new HashSet<string>();
                        keyPropertyPaths.Add(pair.keySP.propertyPath);
                        pair.keySP.Dispose();
                    }
                }
                propertyKeyPairs.Clear();
            }
            else
            {
                var iterator = prop.Copy();
                int depth = iterator.depth;
                if (iterator.NextChildSkipArray())
                {
                    do
                    {
                        var childPathState = pathState.Child(iterator.propertyPath);
                        Convert(remap, inverted, childPathState, iterator, pf);
                    }
                    while (iterator.Next(false, false) && depth < iterator.depth);
                }
                iterator.Dispose();
            }
        }

        public void ClearConversions()
        {
            if (keyPropertyPaths != null)
                keyPropertyPaths.Clear();
            if (propertyPathToOverridePath != null)
                propertyPathToOverridePath.Clear();
            if (overridePathToPropertyPath != null)
                overridePathToPropertyPath.Clear();
        }
    }

    internal class OverrideTree
    {
        public OverrideNode Get(string fullPath, AV[] avs)
        {
            TryInit(avs);

            OverrideNode on;
            if (pathToNode.TryGetValue(fullPath, out on))
                return on;
            return null;
        }
        public Dictionary<string, OverrideNode> pathToNode = new Dictionary<string, OverrideNode>();
        public List<OverrideNode> overrideTree = new List<OverrideNode>();
        public class OverrideNode
        {
            public bool hasDrawn = false;
            //public bool hasInitialized = false; // For Odin this means if it has iterated over the children properties and set its childrens' hasDrawn
            public bool overriden = false;
            public string relativePath;
            public string fullPath;
            public List<OverrideNode> children = new List<OverrideNode>();

            public void ResetDrawn()
            {
                hasDrawn = false;
                for (int i = 0; i < children.Count; i++)
                    children[i].ResetDrawn();
            }
        }
        private OverrideNode Create(string relativePath, string fullPath)
        {
            return pathToNode[fullPath] = new OverrideNode()
            {
                relativePath = relativePath,
                fullPath = fullPath,
            };
        }
        public void ResetDrawn()
        {
            for (int i = 0; i < overrideTree.Count; i++)
                overrideTree[i].ResetDrawn();
        }
        public OverrideNode Add(string prePath, List<OverrideNode> rvps, string path)
        {
            int index = path.IndexOf('.');
            if (index != -1)
            {
                string parentPath = path.Substring(0, index);
                string fullParentPath = prePath + parentPath;
                path = path.Substring(index + 1, path.Length - index - 1);
                for (int i = 0; i < rvps.Count; i++)
                {
                    if (rvps[i].relativePath == parentPath)
                        return Add(fullParentPath + ".", rvps[i].children, path);
                }
                var parent = Create(parentPath, fullParentPath);
                rvps.Add(parent);
                return Add(fullParentPath + ".", parent.children, path);
            }
            else
            {
                for (int i = 0; i < rvps.Count; i++)
                    if (rvps[i].relativePath == path)
                        return rvps[i]; //? should it return anything at all though?
                var node = Create(path, prePath + path);
                rvps.Add(node);
                return node;
            }
        }
        public void Remove(List<OverrideNode> rvps, string path)
        {
            int index = path.IndexOf('.');
            if (index != -1)
            {
                string parentPath = path.Substring(0, index);
                path = path.Substring(index + 1, path.Length - index - 1);
                for (int i = 0; i < rvps.Count; i++)
                {
                    if (rvps[i].relativePath == parentPath)
                    {
                        Remove(rvps[i].children, path);
                        return;
                    }
                }
            }
            else
            {
                for (int i = 0; i < rvps.Count; i++)
                {
                    if (rvps[i].relativePath == path)
                    {
                        rvps.RemoveAt(i);
                        return;
                    }
                }
            }
        }
        public static OverrideNode SearchNodes(List<OverrideNode> rvps, string path)
        {
            int index = path.IndexOf('.');
            if (index != -1)
            {
                string parentPath = path.Substring(0, index);
                path = path.Substring(index + 1, path.Length - index - 1);
                for (int i = 0; i < rvps.Count; i++)
                    if (rvps[i].relativePath == parentPath)
                        return SearchNodes(rvps[i].children, path);
            }
            else
            {
                for (int i = 0; i < rvps.Count; i++)
                    if (rvps[i].relativePath == path)
                        return rvps[i];
            }
            return null;
        }
        public bool dirty = true;
        public void TryInit(AV[] avs)
        {
            if (!dirty || avs == null)
                return;
            dirty = false;

            pathToNode.Clear();

            overrideTree.Clear();
            for (int i = 0; i < avs.Length; i++)
            {
                var oc = avs[i].GetOverridesCache();
                for (int ii = 0; ii < oc.list.Count; ii++)
                    Add("", overrideTree, oc.list[ii]).overriden = true;
            }
        }
    }

    /// <summary>
    /// Uses a HashSet to optimize when checking if a property has an override or not.
    /// Does a lot more stuff now as well.
    /// </summary>
    internal class OverridesCache
    {
        public List<string> list;
        public HashSet<string> hashSet = new HashSet<string>();

        public HashSet<string> parentPaths = new HashSet<string>();

        public bool Contains(string path)
        {
            return hashSet.Contains(path);
        }

        private void AddParentPathsTo(string path, HashSet<string> paths)
        {
            while (true)
            {
                int index = path.LastIndexOf('.');
                if (index == -1)
                    break;
                path = path.Substring(0, index);
                if (!paths.Add(path)) // Already been added, therefore further parents are also already added.
                    break;
            }
        }
        private void FixParentPaths(string path)
        {
            int index = -1;
            while (true)
            {
                index = path.IndexOf('.', index + 1);
                if (index == -1)
                    break;
                string parentPath = path.Substring(0, index);
                if (!ContainsChildrenPaths(parentPath, false))
                {
                    if (!parentPaths.Remove(parentPath)) // Doesn't exist, therefore children paths also shouldn't exist.
                        break;
                }
            }
        }

        public bool dirty = false;
        public bool userDataDirty = false;

        public bool Add(string path)
        {
            if (hashSet.Add(path))
            {
                list.Add(path);
                dirty = true;
                AddParentPathsTo(path, parentPaths);
                return true;
            }
            else
            {
                Debug.LogWarning("Override already exists: " + path + Helper.LOG_END);
                return false;
            }
        }
        public bool Remove(string path)
        {
            list.Remove(path);
            if (hashSet.Remove(path))
            {
                dirty = true;
                FixParentPaths(path);
                return true;
            }
            return false;
        }

        public void Init(List<string> list)
        {
            this.list = list;

            hashSet.Clear();
            parentPaths.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                string path = list[i];

                if (!hashSet.Add(path))
                {
                    Debug.LogWarning("Removed duplicate override: " + list[i] + Helper.LOG_END);
                    list.RemoveAt(i);
                    i--;
                    userDataDirty = true;
                }
                else
                {
                    AddParentPathsTo(path, parentPaths);
                }
            }
        }

        private static readonly HashSet<string> dirtyParentPaths = new HashSet<string>();
        public bool RemoveChildrenPaths(string path) //RemoveSubOverrides
        {
            bool dirty = false;

            dirtyParentPaths.Clear();

            string childStart = path + "."; // Albeit odin serialized children of unity serialized properties might not be matched
            for (int i = list.Count - 1; i >= 0; i--)
            {
                string o = list[i];
                if (o.StartsWith(childStart))
                {
                    list.RemoveAt(i);
                    hashSet.Remove(o);
                    this.dirty = true;
                    if (Helper.LogUnnecessary)
                        Debug.Log("Removed child override: " + o + Helper.LOG_END);
                    dirty = true;
                    parentPaths.Remove(o); // For optimization
                    AddParentPathsTo(o, dirtyParentPaths);
                }
            }
            if (dirty)
            {
                foreach (string p in dirtyParentPaths)
                {
                    if (parentPaths.Contains(p) && !ContainsChildrenPaths(p, false))
                        parentPaths.Remove(p);
                }
            }

            return dirty;
        }

        public bool ContainsChildrenPaths(string path, bool includeSelf = true) //ContainsSubOverrides
        {
            if (includeSelf && Contains(path))
                return true;

            string childStart = path + ".";
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].StartsWith(childStart))
                    return true;
            }

            return false;
        }
        public bool FastContainsChildrenPaths(string path, bool includeSelf = true) //ContainsSubOverrides
        {
            if (includeSelf && hashSet.Contains(path))
                return true;
            if (parentPaths.Contains(path))
                return true;
            return false;
        }

        public bool IsOverridden(string path)
        {
            while (true)
            {
                if (Contains(path))
                    return true;

                int index = path.LastIndexOf('.');
                if (index == -1)
                    return false;
                path = path.Substring(0, index);
                //if (path.Length == 0)
                //    return false;
            }
        }
    }
}

#endif
