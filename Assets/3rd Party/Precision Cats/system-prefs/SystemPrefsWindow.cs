#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace SystemPrefs
{
    public class SystemPrefsWindow : EditorWindow, IHasCustomMenu
    {
        public static string help = "The red elements are “invalid”, this can be if Unity is in dark mode and a ColorWrapEditorSettings asset is for light mode only.\n" +
            "Darker colored elements are “Disabled”/“Off”, which means the SystemPrefsState asset will not set their systemActive and systemPriority values to EditorProjectPrefs.\n\n" +
            "Modifying the values in the \"States\" tab will modify the SystemPrefsState assets themselves, but modifying the top (foldout) lines (the \"keys\") in the State Keys tab will modify EditorProjectPrefs' values. This is usually preferable because an update to the Asset Variants package can't overwrite your PlayerPrefs/EditorPrefs.\n" +
            "Note that you can't modify the values of a \"key\" line if a \"state\" that uses it (the items in its foldout area) is \"On\". All the states that use a systemKey will need to be disabled (\"Off\") in order for you to modify the values of the \"key\" manually.\n\n" +
            "You might want to use the Color Wraps tab and press Toggle Active Sibling to choose a color wraps preset.";

        public static float tabHeight = 20;
        public static float tabCharacterWidth = 8;
        public static float postTabSpacing = 5;

        public static float rootElementSpacing = 10;

        /// <summary>
        /// The first sorting mode executed. This will only have an effect for when defaultSortingMode2 encounters equal values.
        /// </summary>
        public static SortingMode defaultSortingMode1 = SortingMode.Alphabetical;
        public static SortingMode defaultSortingMode2 = SortingMode.Descending;
        public enum SortingMode { None, Alphabetical, ReverseAlphabetical, Ascending, Descending }

        public static float keyPriorityWidth = 100;
        public static float keyActiveWidth = 15;

        public static System.Func<bool> toggleActiveSelfOnly = () => { return Event.current.control; };
        public static System.Func<bool> shouldRepaint = () =>
        {
            var t = Event.current.type;
            var kc = Event.current.keyCode;
            return (t == EventType.KeyUp || t == EventType.KeyDown) &&
                (kc == KeyCode.LeftControl || kc == KeyCode.RightControl);
        };

        public static GUIContent enabledLabel = new GUIContent("On", "State is not Disabled");
        public static GUIContent disabledLabel = new GUIContent("Off", "State is Disabled");
        public static Color enabledColor = Color.white;
        public static Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        public static Color validColor = Color.white;
        public static Color invalidColor = new Color(1, 0.25f, 0.25f, 0.75f);

        public static Color darkModeBackgroundTint = new Color(0.5f, 0.5f, 0.5f, 1);

        public static float stateMarginLeft = 0;
        public static float stateDisabledWidth = 30;
        public static float statePriorityWidth = 100;
        public static float stateActiveWidth = 15;
        public static float stateVariantsButtonWidth = 35;
        public static float stateMarginRight = 10;

        public static float stateVariantsBeforeSpacing = 15;
        public static float stateVariantsAfterSpacing = 30;

        public static float groupToggleActiveSiblingWidth = 160;
        public static float groupSelectWidth = 50;

        public static List<Tab> tabs = new List<Tab>()
        {
            new StatesTab() { title = new GUIContent("States", "Shows every SystemPrefsState asset.") },
            new StateKeysTab() { title = new GUIContent("State Keys", "Shows every Key that is used by at least one SystemPrefsState asset.") },
            new HelpTab(),
        };

        [MenuItem("Window/System Prefs", priority = 990000)]
        private static void Init()
        {
            SystemPrefsWindow window = (SystemPrefsWindow)GetWindow(typeof(SystemPrefsWindow), false, "System Prefs");
            window.Show();
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            GUIContent content = new GUIContent("Reload");
            menu.AddItem(content, false, OnEnable);
        }

        internal static Color SetColor(bool valid, bool enabled)
        {
            var prev = GUI.color;
            GUI.color = (valid ? validColor : invalidColor) * (enabled ? enabledColor : disabledColor);
            return prev;
        }

        public class StateKeysTab : Tab
        {
            public StateKeysTab()
            {
                useGroups = false;
            }

            private static readonly List<string> alreadyKeys = new List<string>();
            private void Create(SystemPrefsAsset asset)
            {
                if (asset is SystemPrefsState)
                {
                    var key = ((SystemPrefsState)asset).SystemKey;
                    if (!alreadyKeys.Contains(key))
                    {
                        alreadyKeys.Add(key);
                        elements.Add(CreateKey(key));
                    }
                }
                else if (asset is SystemPrefsGroup)
                {
                    var group = (SystemPrefsGroup)asset;
                    for (int i = 0; i < group.children.Count; i++)
                        Create(group.children[i]);
                }
            }
            protected override void CreateElements(List<SystemPrefsAsset> assets)
            {
                alreadyKeys.Clear();
                for (int i = 0; i < assets.Count; i++)
                    Create(assets[i]);
            }
        }

        public class StatesTab : Tab
        {
            protected override void CreateElements(List<SystemPrefsAsset> assets)
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    var element = CreateElement(assets[i]);
                    if (element != null)
                        elements.Add(element);
                }
            }
        }

        public class HelpTab : Tab
        {
            public HelpTab()
            {
                minimumWidth = 30;
                characterCountOverride = 0;
            }

            public static List<Info> infos = new List<Info>()
            {
                new Info(help) { IsHelp = true }
            };

            public class Info : Element
            {
                public override string SortString => "!";
                public override int SortInt => int.MinValue;

                public string text;

                public bool IsHelp { get; internal set; } = false;

                public GUIStyle style;

                public Info(string text)
                {
                    this.text = text;
                }

                public override void OnGUI()
                {
                    if (style == null)
                    {
                        style = new GUIStyle(EditorStyles.label);
                        style.wordWrap = true;
                        style.fontSize *= 3;
                        style.fontSize /= 2;
                    }
                    GUILayout.Label(text, style);
                }
            }

            private bool initialized = false;
            public override void UpdateTitle()
            {
                if (!initialized)
                {
                    initialized = true;

                    title = new GUIContent();
                    title.image = (Texture2D)typeof(EditorGUIUtility).GetProperty("infoIcon", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                    if (title.image == null)
                        title.text = "(!)";
                    else
                        title.text = "";
                    title.tooltip = help;
                }
            }

            protected override void CreateElements(List<SystemPrefsAsset> assets)
            {
                for (int i = 0; i < infos.Count; i++)
                {
                    var info = infos[i];
                    if (info.IsHelp)
                        info.text = help;
                }
                elements.AddRange(infos);
            }
        }

        public abstract class Tab
        {
            public GUIContent title;
            public int characterCountOverride = -1;
            public float minimumWidth = 0;
            public string search = "t:SystemPrefsAsset";
            public System.Type[] assetTypes = new System.Type[0]; // { typeof(SystemPrefsAsset) };
            /// <summary>
            /// The first sorting mode executed. This will only have an effect for when sortingMode2 encounters equal values.
            /// </summary>
            public SortingMode sortingMode1 = defaultSortingMode1;
            public SortingMode sortingMode2 = defaultSortingMode2;
            public bool useGroups = true;

            public int CharacterCount => characterCountOverride != -1 ? characterCountOverride : title.text.Length;

            public List<Element> elements = new List<Element>();
            public Dictionary<string, List<SystemPrefsState>> keyStates = new Dictionary<string, List<SystemPrefsState>>();

            public State variantsExpandedState;

            private void Sort(List<Element> children, SortingMode sortingMode)
            {
                if (sortingMode == SortingMode.None)
                    return;

                if (sortingMode == SortingMode.Alphabetical)
                {
                    children.Sort((x, y) => string.Compare(x.SortString, y.SortString));
                }
                else if (sortingMode == SortingMode.ReverseAlphabetical)
                {
                    children.Sort((x, y) => string.Compare(y.SortString, x.SortString));
                }
                else if (sortingMode == SortingMode.Ascending)
                {
                    var newChildren = children.OrderBy((x) => x.SortInt).ToList();
                    children.Clear();
                    children.AddRange(newChildren);
                }
                else if (sortingMode == SortingMode.Descending)
                {
                    var newChildren = children.OrderByDescending((x) => x.SortInt).ToList();
                    children.Clear();
                    children.AddRange(newChildren);
                }

                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] is Foldout)
                        Sort(((Foldout)children[i]).children, sortingMode);
                }
            }

            public abstract class Element
            {
                public Tab tab;

                public abstract string SortString { get; }
                public abstract int SortInt { get; }

                public abstract void OnGUI();
            }
            public class Foldout : Element
            {
                public string name;

                public override string SortString => name;
                public override int SortInt => int.MinValue; //?

                public bool isExpanded = false;

                public List<Element> children = new List<Element>();

                protected void FoldoutOnGUI(Rect rect, bool drawBackground = true, bool alwaysExpanded = false)
                {
                    if (drawBackground)
                    {
                        var indentedRect = EditorGUI.IndentedRect(rect);
                        var backgroundColor = GUI.color;
                        if (EditorGUIUtility.isProSkin)
                            backgroundColor *= darkModeBackgroundTint;
                        backgroundColor.a *= 0.5f;
                        EditorGUI.DrawRect(indentedRect, backgroundColor);
                    }

                    if (alwaysExpanded)
                    {
                        EditorGUI.LabelField(rect, name);
                        isExpanded = true;
                    }
                    else
                        isExpanded = EditorGUI.Foldout(rect, isExpanded, name, true);
                }
                protected void ChildrenOnGUI()
                {
                    if (isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < children.Count; i++)
                            children[i].OnGUI();
                        EditorGUI.indentLevel--;
                    }
                }

                public override void OnGUI()
                {
                    FoldoutOnGUI(EditorGUILayout.GetControlRect());
                    ChildrenOnGUI();
                }
            }
            public class Key : Foldout
            {
                public bool alwaysExpanded;

                public Key(string systemKey, bool alwaysExpanded)
                {
                    name = systemKey;
                    isExpanded = true;
                    this.alwaysExpanded = alwaysExpanded;
                }

                public override int SortInt => SystemPrefsUtility.GetPriority(name);

                public override void OnGUI()
                {
                    var rect = EditorGUILayout.GetControlRect();

                    rect.width -= keyPriorityWidth + keyActiveWidth;
                    var foldoutRect = rect;

                    bool anyValidAndEnabled = false;
                    for (int i = 0; i < children.Count; i++)
                    {
                        var sps = ((State)children[i]).systemPrefsState;
                        if (sps.Valid && sps.Enabled)
                            anyValidAndEnabled = true;
                    }

                    bool prevGUIEnabled = GUI.enabled;
                    if (anyValidAndEnabled)
                        GUI.enabled = false;

                    bool active = SystemPrefsUtility.GetActive(name);
                    int priority = SystemPrefsUtility.GetPriority(name);

                    int prevIndentLevel = EditorGUI.indentLevel;

                    rect.x += rect.width;
                    rect.width = keyPriorityWidth;
                    EditorGUI.indentLevel = 1; // Gives it a little space to drag to change the value
                    EditorGUI.BeginChangeCheck();
                    int newPriority = EditorGUI.IntField(rect, GUIContent.none, priority);
                    if (EditorGUI.EndChangeCheck())
                        SystemPrefsUtility.SetPriority(name, newPriority);

                    rect.x += rect.width;
                    rect.width = keyActiveWidth;
                    EditorGUI.indentLevel = 0;
                    EditorGUI.BeginChangeCheck();
                    bool newActive = EditorGUI.Toggle(rect, active);
                    if (EditorGUI.EndChangeCheck())
                        SystemPrefsUtility.SetActive(name, newActive);

                    EditorGUI.indentLevel = prevIndentLevel;

                    FoldoutOnGUI(foldoutRect, false, alwaysExpanded);

                    GUI.enabled = prevGUIEnabled;

                    ChildrenOnGUI();
                }
            }
            public class State : Element
            {
                public readonly SystemPrefsState systemPrefsState;
                public State(SystemPrefsState systemPrefsState)
                {
                    this.systemPrefsState = systemPrefsState;
                }

                public override string SortString => systemPrefsState.SystemKey;
                public override int SortInt => systemPrefsState.SystemPriority;

                public override void OnGUI()
                {
                    var s = systemPrefsState;

                    var rect = EditorGUILayout.GetControlRect();
                    rect.x += stateMarginLeft;
                    rect.width -= stateMarginLeft + stateDisabledWidth + statePriorityWidth + stateActiveWidth + stateMarginRight;

                    int stateCount = -1;
                    List<SystemPrefsState> states = null;
                    if (tab != null && tab.keyStates.TryGetValue(s.SystemKey, out states))
                    {
                        if (states.Count > 1)
                        {
                            stateCount = states.Count;
                            rect.width -= stateVariantsButtonWidth;
                        }
                    }

                    bool disabled = s.Disabled;

                    Color prevGUIColor = SetColor(s.Valid, !disabled);

                    var disabledRect = EditorGUI.IndentedRect(rect);
                    disabledRect.width = stateDisabledWidth;
                    if (GUI.Button(disabledRect, disabled ? disabledLabel : enabledLabel))
                    {
                        SystemPrefsUtility.SetDisabled(s, !disabled);
                    }
                    rect.x += stateDisabledWidth;

                    bool prevGUIEnabled = GUI.enabled;
                    GUI.enabled = false;
                    EditorGUI.ObjectField(rect, s, typeof(SystemPrefsState), false);
                    GUI.enabled = prevGUIEnabled;

                    var prevIndentLevel = EditorGUI.indentLevel;

                    rect.x += rect.width;
                    rect.width = statePriorityWidth;
                    EditorGUI.indentLevel = 1; // Gives it a little space to drag to change the value
                    EditorGUI.BeginChangeCheck();
                    var newPriority = EditorGUI.IntField(rect, GUIContent.none, s.systemPriority);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(s, "Change System Priority");
                        s.systemPriority = newPriority;
                        s.UpdateSystemPriority();
                        EditorUtility.SetDirty(s);
                    }

                    rect.x += rect.width;
                    rect.width = stateActiveWidth;
                    EditorGUI.indentLevel = 0;
                    EditorGUI.BeginChangeCheck();
                    bool newSystemActive = EditorGUI.Toggle(rect, s.SystemActive);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(s, "Toggle Active");
                        s.SystemActive = newSystemActive;
                        EditorUtility.SetDirty(s);
                    }

                    EditorGUI.indentLevel = prevIndentLevel;

                    GUI.color = prevGUIColor;

                    if (stateCount != -1)
                    {
                        rect.x += rect.width;
                        rect.width = stateVariantsButtonWidth;

                        int validEnabledCount = 0;
                        for (int i = 0; i < states.Count; i++)
                        {
                            if (states[i].Valid && states[i].Enabled)
                                validEnabledCount++;
                        }

                        const string info = "Press to drop down a Key element, to show all the SystemPrefsState assets that have the same systemKey (including this one).\n\n" +
                            "X/Y - X is the count of valid and enabled, Y is the total count. X shouldn't be more than 1, otherwise they will likely conflict, as they all want to set their own active/priority values.\n\n" +
                            "If you don't see this button, that means either: there is only one state asset in total (this one), or: the parent element is already a Key element.";
                        if (GUI.Button(rect, new GUIContent(validEnabledCount + "/" + stateCount, info)))
                        {
                            if (tab.variantsExpandedState == this)
                                tab.variantsExpandedState = null;
                            else
                                tab.variantsExpandedState = this;
                        }

                        if (tab.variantsExpandedState == this)
                        {
                            GUILayout.Space(stateVariantsBeforeSpacing);
                            var key = tab.CreateKey(s.SystemKey, true);
                            key.OnGUI();
                            GUILayout.Space(stateVariantsAfterSpacing);
                        }
                    }
                }
            }
            public class Group : Foldout
            {
                public readonly SystemPrefsGroup systemPrefsGroup;
                public Group(SystemPrefsGroup systemPrefsGroup)
                {
                    this.systemPrefsGroup = systemPrefsGroup;
                    name = systemPrefsGroup.name;
                }

                public static Group Find(List<Group> groups, SystemPrefsGroup spg)
                {
                    for (int i = 0; i < groups.Count; i++)
                        if (groups[i].systemPrefsGroup == spg)
                            return groups[i];
                    return null;
                }

                public override void OnGUI()
                {
                    if (systemPrefsGroup.Null)
                        return;

                    var rect = EditorGUILayout.GetControlRect();

                    Color prevGUIColor = SetColor(systemPrefsGroup.Valid, systemPrefsGroup.Enabled);

                    var buttonRect = rect;
                    buttonRect.width = groupToggleActiveSiblingWidth;
                    buttonRect = EditorGUI.IndentedRect(buttonRect);
                    if (toggleActiveSelfOnly())
                    {
                        if (GUI.Button(buttonRect, "Toggle Active"))
                            systemPrefsGroup.ToggleActive();
                    }
                    else
                    {
                        if (GUI.Button(buttonRect, "Toggle Active Sibling"))
                            systemPrefsGroup.ToggleActiveSibling();
                    }
                    rect.x += groupToggleActiveSiblingWidth;
                    rect.width -= groupToggleActiveSiblingWidth;

                    rect.width -= groupSelectWidth;

                    FoldoutOnGUI(rect);

                    rect.x += rect.width;
                    rect.width = groupSelectWidth;
                    if (GUI.Button(rect, "Select"))
                    {
                        EditorGUIUtility.PingObject(systemPrefsGroup);
                        Selection.objects = new Object[] { systemPrefsGroup };
                        Selection.activeObject = systemPrefsGroup;
                    }

                    GUI.color = prevGUIColor;

                    ChildrenOnGUI();
                }
            }

            protected Element CreateElement(SystemPrefsAsset asset)
            {
                if (asset is SystemPrefsState)
                {
                    return new State((SystemPrefsState)asset)
                    {
                        tab = this
                    };
                }

                if (asset is SystemPrefsGroup)
                {
                    var spg = (SystemPrefsGroup)asset;
                    var group = new Group(spg)
                    {
                        tab = this
                    };
                    for (int i = 0; i < spg.children.Count; i++)
                    {
                        var child = CreateElement(spg.children[i]);
                        if (child != null)
                            group.children.Add(child);
                    }
                    return group;
                }

                return null;
            }

            protected Element CreateKey(string systemKey, bool alwaysExpanded = false)
            {
                var key = new Key(systemKey, alwaysExpanded) { tab = this };
                List<SystemPrefsState> states;
                if (keyStates.TryGetValue(systemKey, out states))
                {
                    for (int ii = 0; ii < states.Count; ii++)
                    {
                        var state = CreateElement(states[ii]);
                        state.tab = null;
                        if (state == null)
                        {
                            Debug.LogWarning("?");
                            continue;
                        }
                        key.children.Add(state);
                    }
                }
                return key;
            }

            private static void RemoveChildren(List<SystemPrefsAsset> assets, SystemPrefsGroup group, ref bool removed)
            {
                for (int i = 0; i < group.children.Count; i++)
                {
                    var child = group.children[i];
                    if (assets.Remove(child))
                    {
                        removed = true;
                        if (child is SystemPrefsGroup)
                            RemoveChildren(assets, (SystemPrefsGroup)child, ref removed);
                    }
                }
            }

            public void Build()
            {
                variantsExpandedState = null;

                var assets = SystemPrefsUtility.FindAssets<SystemPrefsAsset>(search);

                if (assetTypes.Length != 0)
                {
                    for (int i = assets.Count - 1; i >= 0; i--)
                    {
                        var assetType = assets[i].GetType();
                        bool found = false;
                        for (int ii = 0; ii < assetTypes.Length; ii++)
                        {
                            if (assetTypes[ii].IsAssignableFrom(assetType))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            assets.RemoveAt(i);
                    }
                }

                keyStates.Clear();
                for (int i = 0; i < assets.Count; i++)
                {
                    var asset = assets[i];
                    //Debug.Log(asset.GetType());
                    if (asset is SystemPrefsState)
                    {
                        var state = (SystemPrefsState)asset;
                        List<SystemPrefsState> states;
                        if (!keyStates.TryGetValue(state.SystemKey, out states))
                        {
                            states = new List<SystemPrefsState>();
                            keyStates.Add(state.SystemKey, states);
                        }
                        states.Add(state);
                    }
                }

                if (useGroups)
                {
                    bool removed;
                    do
                    {
                        removed = false;
                        for (int i = 0; i < assets.Count; i++)
                        {
                            if (assets[i] is SystemPrefsGroup)
                                RemoveChildren(assets, (SystemPrefsGroup)assets[i], ref removed);
                        }
                    }
                    while (removed); // Repeats until done (because the indices can shift)
                }

                elements.Clear();
                CreateElements(assets);

                Sort(elements, sortingMode1);
                Sort(elements, sortingMode2);
            }
            protected abstract void CreateElements(List<SystemPrefsAsset> assets);

            public virtual void OnSwitchTo()
            {
                Build();
            }

            public Vector3 scrollPosition;

            public virtual void UpdateTitle() { }

            public virtual void OnGUI()
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                for (int i = 0; i < elements.Count; i++)
                {
                    elements[i].OnGUI();
                    GUILayout.Space(rootElementSpacing);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        [SerializeField]
        private int serializedTabID = -1;
        public int SerializedTabID => serializedTabID;

        private Tab currentTab = null;
        public void SwitchToTab(Tab tab)
        {
            serializedTabID = tabs.IndexOf(tab);
            if (serializedTabID != -1)
            {
                currentTab = tab;
                tab.OnSwitchTo();
            }
        }

        private void OnPrefsChanged(string systemKey)
        {
            Repaint();
        }

        private void OnEnable()
        {
            // Moves HelpTab/s to the end/rightmost.
            List<Tab> helpTabs = new List<Tab>();
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] is HelpTab)
                {
                    helpTabs.Add(tabs[i]);
                    tabs.RemoveAt(i);
                    i--;
                }
            }
            tabs.AddRange(helpTabs);

            //TODO:::?
            //titleContent.tooltip = help; 
            titleContent.tooltip = "";

            if (serializedTabID >= 0 && serializedTabID < tabs.Count)
            {
                currentTab = tabs[serializedTabID];
                currentTab.OnSwitchTo();
            }
            else if (currentTab != null)
            {
                currentTab.OnSwitchTo();
            }
            else if (tabs.Count > 0)
            {
                currentTab = tabs[0];
                currentTab.OnSwitchTo();
            }

            SystemPrefsUtility.OnChanged -= OnPrefsChanged;
            SystemPrefsUtility.OnChanged += OnPrefsChanged;
            SystemPrefsUtility.OnDisabledChanged -= OnPrefsChanged;
            SystemPrefsUtility.OnDisabledChanged += OnPrefsChanged;
        }
        private void OnDisable()
        {
            SystemPrefsUtility.OnChanged -= OnPrefsChanged;
            SystemPrefsUtility.OnDisabledChanged -= OnPrefsChanged;
        }

        private float width = 0;

        public bool needsRepaint;

        public static GUIStyle tabStyle = null;

        public virtual void OnGUI()
        {
            if (shouldRepaint())
                Repaint();

            for (int i = 0; i < tabs.Count; i++)
                tabs[i].UpdateTitle();

            if (tabStyle == null)
            {
                tabStyle = GUI.skin.button.name + "mid"; //"EditModeSingleButton"
                if (tabStyle == null)
                    tabStyle = GUI.skin.button;
                tabStyle = new GUIStyle(tabStyle);
            }

            if (tabs.Count != 1)
            {
                var tabsRect = EditorGUILayout.GetControlRect(false, -EditorGUIUtility.standardVerticalSpacing);
                if (tabsRect.width > 1)
                {
                    width = tabsRect.width;
                    Repaint();
                    needsRepaint = true;
                }
                else
                    tabsRect.width = width;

                tabsRect.height = tabHeight;

                int tabFrom = 0;
                float totalHeight = 0;
                while (tabFrom < tabs.Count)
                {
                    var rect = tabsRect;
                    float w = width;

                    int tabTo = tabFrom;

                    int charSum = 0;
                    for (int i = tabFrom; i < tabs.Count; i++)
                    {
                        int chars = tabs[i].CharacterCount;
                        float ww = w - tabs[i].minimumWidth;
                        if (i > tabFrom && ((charSum + chars) * tabCharacterWidth > ww))
                            break;
                        w = ww;
                        tabTo = i;
                        charSum += chars;
                    }

                    if (charSum == 0)
                        charSum = 1;

                    for (int i = tabFrom; i <= tabTo; i++)
                    {
                        var tab = tabs[i];
                        rect.width = w * tabs[i].CharacterCount / charSum + tab.minimumWidth;
                        EditorGUI.BeginChangeCheck();
                        GUI.Toggle(rect, tab == currentTab, tab.title, tabStyle);
                        if (EditorGUI.EndChangeCheck())
                            SwitchToTab(tab);
                        rect.x += rect.width;
                    }

                    tabFrom = tabTo + 1;

                    float actualHeight = tabHeight - 2; // Closes the gap
                    tabsRect.y += actualHeight;
                    totalHeight += actualHeight;
                }
                if (width == 0)
                    GUILayout.Space(tabHeight);
                else
                    GUILayout.Space(totalHeight);
                GUILayout.Space(postTabSpacing);
            }

            if (!tabs.Contains(currentTab))
                currentTab = null;

            if (currentTab != null)
                currentTab.OnGUI();

            serializedTabID = tabs.IndexOf(currentTab);
        }
    }
}
#endif
