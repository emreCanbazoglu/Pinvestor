#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using PrecisionCats;
using EditorReplacement;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace AssetVariants
{
    [CustomEditor(typeof(AVSettings))]
    internal class AVSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SettingsBaseEditorGUI<AVSettings>.DrawGUI(target);

            //serializedObject.Update();

            if (serializedObject.FindProperty("onlySaveUserDataOnLeave").boolValue)
            {
                EditorGUILayout.HelpBox("The setting \"onlySaveUserDataOnLeave\" is set to true.\nThis negatively affects undoability.", MessageType.Warning, true);
                GUILayout.Space(20);
            }

            DrawDefaultInspector();
        }
    }

    public sealed class AVSettings : SettingsBase<AVSettings>
    {
        public const string disable_ASSET_VARIANTS_DO_ODIN_PROPERTIES_key = "disable_ASSET_VARIANTS_DO_ODIN_PROPERTIES";

        static AVSettings()
        {
            defaultGUID = "2330b1b085fe31144afab4a34eb64d47";
        }

        public override void OnSettingsChanged()
        {
#if EDITOR_REPLACEMENT
            ERManager.Rebuild();
#endif
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            LoadSettings();

            SettingsBaseEditorGUI.OnCreateSettings -= OnCreateSettings;
            SettingsBaseEditorGUI.OnCreateSettings += OnCreateSettings;
        }
        private static void OnCreateSettings(string defaultPath, string fullCurrentPath)
        {
            AVUtility.ChangeParent(fullCurrentPath, defaultPath);
        }

        /// <summary>
        /// This is to avoid a bug/bad design in unity before (I believe) 2022.1 (it runs normally without this code in that version)
        /// where the performance of Undo.FlushUndoRecordObjects is O(n^2)
        /// </summary>
        public static int REGULAR_RECORD_UNDO_MAX_CHANGES = 100;

        public static Func<bool> currentlyAllowSubOverrides = () => { return Event.current.control; };
#if UNITY_2019_1_OR_NEWER
        public static ManipulatorActivationFilter allowSubOverridesMAF = new ManipulatorActivationFilter() { modifiers = EventModifiers.Control };
#endif

        // public static Func<UnityEngine.Object, bool> disablePropagation = (asset) => { return Application.isPlaying; };
        public static Func<UnityEngine.Object, bool> disablePropagation = (asset) => { return false; }; // (I hope it doesn't break anything to remove the Application.isPlaying condition)

        public static Func<UnityEngine.Object, bool> shouldRenameOverrides = (o) => { return !S.disableRenamingOverrides; };

        public static Func<bool> currentlyRawViewDrawNiceNames = () => { return !Event.current.alt; };

        [Space(10, order = 0)]
        [Header("Filtering", order = 1)]
        [Space(4, order = 2)]

        [Tooltip("scriptableObjectOnly and materialOnly can both be true")]
        public bool scriptableObjectOnly = false;
        private static int prevScriptableObjectOnly = -1;
        [Tooltip("scriptableObjectOnly and materialOnly can both be true")]
        public bool materialOnly = false;
        private static int prevMaterialOnly = -1;

        [Space(5)]
        [Tooltip("In 2022.1+ material variants are natively implemented. Ignored if materialOnly=true")]
        public bool disableMaterialVariants = false; //TODO3: make this disable AV as well? make it return false in ValidType()?
        private static int prevDisableMaterialVariants = -1;

        [Space(5)]
        [Tooltip("Full (namespaced) type names.")]
        [UnityEngine.Serialization.FormerlySerializedAs("targetTypeFilter")]
        public StringTypeFilter typeFilter = new StringTypeFilter();

        [Space(10, order = 0)]
        [Header("Styling", order = 1)]
        [Space(4, order = 2)]

        public bool boldOverriddenIMGUI = true; //TODO2: specific one for Odin?
        public bool boldOverriddenUIToolkit = true;
        [Tooltip("(This cannot differentiate between sub-properties)")]
        public bool boldOverriddenMaterial = true;

        [Space(2)]
        [UnityEngine.Serialization.FormerlySerializedAs("overrideButtonStyle"), UnityEngine.Serialization.FormerlySerializedAs("overrideStyle")]
        public OverrideIndicatorStyle overrideIndicatorStyle = new OverrideIndicatorStyle();
        [Serializable]
        public class OverrideIndicatorStyle
        {
            public Color overriddenColor = new Color(0, 0.65f, 1.0f, 0.75f);
            public Color childrenOverriddenColor;
            public Color notOverriddenColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            public Color mixedColor = new Color(1, 0.65f, 0, 0.75f);

            public OverrideIndicatorStyle()
            {
                childrenOverriddenColor = Color.LerpUnclamped(notOverriddenColor, overriddenColor, 0.4f);
            }

            [Space(10)]

            [Tooltip("(BTW thin bars are never used in Raw View) - For UI Toolkit")]
            public bool useThinBarsUIToolkit = true;
            [Space(2)]
            [Tooltip("Set this to false if you want a UI Toolkit composite field's children fields to always use buttons, never thin bars.\n" +
            "The reason for this is because those fields don't have context menu support by Unity (afaik), and therefore it's not possible to revert specific children fields without an override button or Raw View.")] //TODO2:
            public bool useThinBarsForUITCompositeChildren = true;

#if !ODIN_INSPECTOR
            [HideInInspector]
#endif
            [Space(2)]
            [Tooltip("(BTW thin bars are never used in Raw View)")]
            public bool useThinBarsOdin = true;

            [Space(2)]
            [Tooltip("Set this to false so you can know when a field is unwrapped and you can't see if it's overridden (except in Raw View). This can be ignored depending on ForceRootThinBarsVisible.")]
            public bool thinBarsNotOverriddenInvisible = true;
            public float thinBarWidth = 2;
            public Margins thinBarMargins = new Margins() { left = -3, right = 1, top = 0, bottom = 0, };

            [Space(10)]

            [Tooltip("Only works for Materials, UI Toolkit, and Odin, not IMGUI")]
            public float width = 10;

            [Tooltip("Button width while Raw View is active")]
            public float rawViewWidth = 12;

            public Margins margins = new Margins();

#if !ODIN_INSPECTOR
            [HideInInspector]
#endif
            [Space(10)]
            [Tooltip("(For Odin Inspector only)")]
            public float collectionSizeWidth = 10;
#if !ODIN_INSPECTOR
            [HideInInspector]
#endif
            [Space(5)]
            [Tooltip("(For Odin Inspector only)")]
            public float odinCollectionSizeXShift = -80;

            [Serializable]
            public class Margins
            {
                public float left = 2;
                public float right = 2;
                public float top = 1;
                public float bottom = 1;
            }

            [Space(10)]

            [Tooltip("For Color/Vector material properties. This is the horizontal spacing between the main override button and the 4 channels' override buttons.")]
            public float materialChannelsFirstSpacing = 10;
            [Tooltip("For Color/Vector material properties. This is the horizontal spacing between each of the 4 channels' override buttons.")]
            public float materialChannelsSpacing = 2;
            [Tooltip("For Texture material properties there isn't much room for an override button. This can move it to be further away from the X/Y draggable labels, although for the Ys it would increase the overlap with the Xs' text fields.")]
            public float materialTilingOffsetXOffset = -8;
        }

        [Space(5)]
        [Tooltip("The width reserved for the \"Parent\" text label in the parent selection field at the top of an AVEditor.")]
        public float parentLabelWidth = 45;
        [Space(5)]
        [Tooltip("If defaultInlineRawViewButton=true, then this is the horizontal spacing to the left of the Raw View button.")]
        public float rawViewButtonSpacing = 5;
        [Tooltip("If defaultInlineRawViewButton=true, then this is the width of the Raw View button. You could for example change this to 25 and change inlineRawViewButtonLabel to \"RV\" to save space.")]
        public float rawViewButtonWidth = 70;
        [Tooltip("If defaultInlineRawViewButton=true, then this is the label text of the Raw View button. You could for example change this to \"RV\" and change inlineRawViewButtonWidth to 25 to save space.")]
        public string rawViewButtonLabel = "Raw View";
        [Space(5)]
        [Tooltip("The width of the \"Children\" foldout of a parent AVEditor.")]
        public float childrenFoldoutWidth = 60;
        [Tooltip("For a parent AVEditor, this is the width of the \"X\" buttons to the right of a child in the \"Children\" foldout, which when pressed reparents the child to null.")]
        public float removeChildButtonWidth = 20;

        [Space(5)]
        public int rawViewWindowLeftPadding = 16;
        [Tooltip("Gets rid of the \"Edit in Raw View\" context menu item.")]
        public bool disabledRawViewPopup = false;
        
        [Space(5)]
        [Tooltip("In Raw View")] //TODO:::fill in
        public float spacingBeforeUndrawnOverrides = 5;
        [Tooltip("In Raw View")]
        public float spacingAfterUndrawnOverrides = 5;
        [Tooltip("The same as spacingBeforeUndrawnOverrides but at the root level")]
        public float spacingBeforeUnclaimedOverrides = 10;
        [Tooltip("The same as spacingAfterUndrawnOverrides but at the root level")]
        public float spacingAfterUnclaimedOverrides = 10;
        [Space(5)]
#if !ODIN_INSPECTOR
        [HideInInspector]
#endif
        public OdinUndrawnOverridesMode odinUndrawnOverridesMode = OdinUndrawnOverridesMode.EveryProperty;
        public enum OdinUndrawnOverridesMode { None = 0, DictionariesAndHashSets = 1, EveryProperty = 2, }
        [Space(5)]
#if !ODIN_INSPECTOR
        [HideInInspector]
#endif
        public float odinSpacingBeforeUndrawnOverrides = 2;
#if !ODIN_INSPECTOR
        [HideInInspector]
#endif
        public float odinSpacingAfterUndrawnOverrides = 2;
        //#if !ODIN_INSPECTOR
        //        [HideInInspector]
        //#endif
        //        [Tooltip("When this is true, then before drawing dummy children overrides for a field, it will check if any of them match the paths of its currently existing children properties.\n" + 
        //        "This means that the dummy overrides drawn are only ones for fields that don't exist. Such non-existent fields include the \"removal overrides\" of a Dictionary or HashSet.\n" + 
        //        "It's important to be able to see when a field has been removed by an override, which is the main reason why dummy overrides are drawn in the first place. (The other being that you can see and clean up overrides for fields that you have removed from your code).\n" + 
        //        "If this is false, then you'll see dummy overrides drawn for any \"not yet drawn\" children,\n" + 
        //        "which means if you haven't expanded a foldout yet it will list below the field its direct children override paths,\n" + 
        //        "which probably take up some unwanted room, without providing the benefit of the similar behaviour of Raw View, which compared to this will also list out grandchildren, great grandchildren, etc. override paths.")]
        //        public bool odinUndrawnUseRemoveChildrenInitialization = true;

        [Space(10, order = 0)]
        [Header("Override Creation", order = 1)]
        [Space(4, order = 2)]

        [Tooltip("When assigning a parent to an asset, if overrides should be created for a field if all its children fields are invisible in the inspector.\n\n" + "For example Color has 4 hidden children (r, g, b, and a). By default (if you don't change the overrides) should each channel individually be overridden if they differ, or should the parent Color be overridden as a whole?\n\n" +
        "If every child is different, the override will still be created for the parent.\n\n" + "Keep in mind that if they are overridden individually you can't see it in the regular inspector, only in Raw View.\n" + "But by overriding the parent itself without holding the Ctrl key, you can easily get rid of all children overrides.")]
        public bool createParentOverrideIfChildrenAreInvisible = true;

        [Tooltip("If the source property doesn't exist (e.g. for array elements outside of the parent array's size).\n" +
        "Whether it should skip entirely (as it's not (yet) necessary for an override to exist),\n" +
        "or to always create (because in the future the parent's array might have its size increased and then an override will be necessary).")]
        public MissingSourceImplicitCreationType missingSourceImplicitCreationType = MissingSourceImplicitCreationType.AlwaysCreateOverride;
        public enum MissingSourceImplicitCreationType { NeverCreateOverride, AlwaysCreateOverride }

        [Tooltip("Minimum time between implicit override creation. This is helpful when typing a string as there are many consecutive modifications, and implicit override creation can be slow.")]
        public float implicitOverrideCreationInterval = 0.15f;

        [Space(10, order = 0)]
        [Header("Serialized Properties", order = 1)]
        [Space(4, order = 2)]

        [Tooltip("You can set this to true for it to behave more like prefabs. It will treat an object as whole if it has no visible children. This means Color fields can not have individual channels overridden.")]
        public bool considerWholeAllInvisibleChildren = false;
        [Tooltip("(For example, strings are actually saved as arrays of characters in unity.)\n\n" + "Properties matching these types are told to draw (and copy in Asset Variants) as a whole, even though they have children that are invisible.\n" +
        "Adding a type to this will allow it to be more human readable in Raw View. For Asset Variants it can also benefit performance, as it doesn't have to check for overrides for each child and copy them over individually.\n\n" + "For Asset Variants you can remove these types if you want more control, to override individual children properties.\n" + "You could use that to modify individual KeyFrames of a Gradient")]
        public Consider considerWhole = new Consider()
        {
            serializedPropertyTypes = new SerializedPropertyType[]
            {
                SerializedPropertyType.String, SerializedPropertyType.Quaternion, SerializedPropertyType.ObjectReference,
#if UNITY_2021_1_OR_NEWER
                SerializedPropertyType.Hash128,
#endif
#if UNITY_2022_1_OR_NEWER
                SerializedPropertyType.Gradient
#endif
            },
            types = new string[1] { "UnityEvent" },
            arrayElementTypes = new string[0],
        };
        [Tooltip("Basically the opposite of considerWhole. It will check for matches in this first. If there are no children at all, it will still be considered whole even if this contains its type/s.")]
        public Consider considerSeparate = new Consider() { serializedPropertyTypes = new SerializedPropertyType[0], types = new string[0], arrayElementTypes = new string[0] };
        [Serializable]
        public struct Consider
        {
            [Tooltip("Will check the serializedProperty.propertyType if it's included in this array.")]
            public SerializedPropertyType[] serializedPropertyTypes;
            [Tooltip("When serializedProperty.propertyType==SerializedPropertyType.Generic/ManagedReference it will check the serializedProperty.type if it's included in this array.")]
            public string[] types;
            [Tooltip("When serializedProperty.isArray==true it will check the serializedProperty.arrayElementType if it's included in this array.")]
            public string[] arrayElementTypes;

            public bool Contains(SerializedProperty prop)
            {
                var propertyType = prop.propertyType;

                for (int i = 0; i < serializedPropertyTypes.Length; i++)
                    if (propertyType == serializedPropertyTypes[i])
                        return true;

#if UNITY_2019_3_OR_NEWER
                if (propertyType == SerializedPropertyType.Generic || propertyType == SerializedPropertyType.ManagedReference)
#else
                if (propertyType == SerializedPropertyType.Generic)
#endif
                {
                    string type = prop.type;
                    for (int i = 0; i < types.Length; i++)
                        if (type == types[i])
                            return true;
                    //Debug.Log(prop.propertyPath + " - " + type);
                }

                if (prop.isArray)
                {
                    string arrayElementType = prop.arrayElementType;
                    for (int i = 0; i < arrayElementTypes.Length; i++)
                        if (arrayElementType == arrayElementTypes[i])
                            return true;
                }

                return false;
            }
        }

        [Space(5)]
        [Tooltip("Unity Arrays/Lists are saved with a child field arrayName\".Array\"\n" +
        "If you want that child property to be visible and overrideable separate from the main field, then set this to false.\n" + "That could be useful if you've inherited from List<T> and have other fields in it.")]
        public bool skipArrayDotArraySubProperty = true;

        [Space(10, order = 0)]
        [Header("Debug", order = 1)]
        [Space(4, order = 2)]

        [Tooltip("Setting this to false will disable some cluttersome console logging.")]
        public bool logUnnecessary = true;
        [Tooltip("If true then every copy of a property (from parent to child usually) will be logged to the console.")]
        public bool logPropertyCopies = false;

        [Space(4)]
        [Tooltip("(Only for debugging purposes). If this is true, then it will draw labels for the type and parent type.")]
        public bool debugTypes = false;

        [Space(10, order = 0)]
        [Header("Other Behaviour", order = 1)]
        [Space(4, order = 2)]

        [Tooltip("If a descendant asset is open in another Editor (such as with a second locked inspector window), should changes propagate to it immediately?")]
        public bool immediatelyPropagateToOpenDescendants = true;

        [Space(2)]
        [Tooltip("Setting this to true can improve performance if override renaming (checking for FormerlySerializedAsAttributes) becomes excessively slow, at the expense that overrides won't be renamed.")]
        public bool disableRenamingOverrides = false;
        [Tooltip("What asset type bases to search for FormerlySerializedAsAttributes in order to rename old overrides.")]
        public string[] formerlySerializedAsBaseAssetTypes = new string[] { "UnityEngine.ScriptableObject" };

        [Space(2)]
        [Tooltip("If set to false, then e.g. the x and y of a Vector4 property could be copied to or from a Vector2 property.\n\nSuch a case would require that the parent and child class types are not directly related,\nbut have fields of the same name and different type.")]
        public bool propertyTypesNeedToMatch = false;

        [Space(2)]
        [Tooltip("Set to true if you want to ensure either that the child's class type inherits from the parent's class type, or that the parent's class type inherits from the child's class type.")]
        public bool directInheritanceRequired = false;

        [Space(2)]
        [Tooltip("WARNING: Setting this to true will prevent undoability of overrides.\n\nSet to true if you don't want overrides/parent/children to be saved until the variant gets deselected.\nIf this is false, it will save the userData JSON after every modification of the parent id/children id list/override paths list.")]
        public bool onlySaveUserDataOnLeave = false;

        //[Space(2)]
        //[Tooltip("If an AssetImporter is dirty, should it call SaveAndReimport when that asset variant gets deselected?")]
        //public bool saveAndReimportOnLeave = true;

        [Space(2)]
#if !ODIN_INSPECTOR
        [HideInInspector]
#endif
        [Tooltip("RawPropertyResolvers are used to bypass problematic OdinValueDrawers. If you enable this then override buttons might not show up for certain types, like Rect or Bounds.")]
        public bool disableOdinRawPropertyResolver = false;

        [Space(10)]
#if !ODIN_INSPECTOR
        [HideInInspector]
#endif
        [Tooltip("If you have Odin Inspector, but don't want Asset Variants to deal with Odin serialized properties (for example Dictionaries and the like).\nSetting this to true will hide override buttons of and prevent transfer of Odin serialized fields.")]
        public bool disable_ASSET_VARIANTS_DO_ODIN_PROPERTIES = false;

        private bool SetPrefsBool(string key, bool value, bool defaultPrefsValue = false)
        {
            if (value != EditorProjectPrefs.GetBool(key, defaultPrefsValue))
            {
                EditorProjectPrefs.SetBool(key, value);
                return true;
            }
            return false;
        }

        public void OnValidate()
        {
            typeFilter.ClearTypeFilter();

            typeFilter.ClearTypeFilter();

            // Just in case e.g. considerWhole was modified
            AVTargets.ClearAllConsideredWhole();
            OverrideIndicator.ClearCaches();

            EditorApplication.delayCall += () => // Delayed to prevent a Unity freeze in AssetDatabase.FindAssets()/ToArray()/ToArray()/FillSet()/UnionWith()/MoveNext()/<FindAllAssets>MoveNext()/<FindEverywhere>MoveNext()/<FindEverywhere>MoveNext()
            {
                LoadSettings();
                if (this == S)
                {
                    AVCommonHelper.UpdateSetting(scriptableObjectOnly, ref prevScriptableObjectOnly);
                    AVCommonHelper.UpdateSetting(materialOnly, ref prevMaterialOnly);

                    AVCommonHelper.UpdateSetting(disableMaterialVariants, ref prevDisableMaterialVariants);

                    if (SetPrefsBool(disable_ASSET_VARIANTS_DO_ODIN_PROPERTIES_key, disable_ASSET_VARIANTS_DO_ODIN_PROPERTIES))
                    {
                        DefineSetter.Update();
                    }
                }
            };
        }
    }
}
#endif
