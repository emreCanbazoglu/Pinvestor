#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetVariants
{
    public class RawDrawer : PropertyDrawer
    {
        public static bool currentlyDrawNiceNames;

        public PropertyDrawer original;

        protected virtual bool VisibleOnly => false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool visibleOnly = VisibleOnly;

            if (Helper.HasChildren(property, visibleOnly))
            {
                // Foldout
                float height = EditorGUIUtility.singleLineHeight;

                // Children
                int childCount = 0;
                if (property.isExpanded)
                {
                    using (var prop = property.Copy())
                    {
                        int depth = prop.depth;
                        if (prop.NextChildSkipArray(visibleOnly))
                        {
                            do
                            {
                                var childLabel = prop.GetLabel(currentlyDrawNiceNames);
                                height += GetPropertyHeight(prop, childLabel);
                                childCount++;
                            }
                            while (prop.Next(false, visibleOnly) && depth < prop.depth);
                        }
                    }
                }
                height += childCount * EditorGUIUtility.standardVerticalSpacing;

                return height;
            }
            else
            {
                if (original != null)
                    return original.GetPropertyHeight(property, property.GetLabel(currentlyDrawNiceNames));
                else
                    return EditorGUI.GetPropertyHeight(property, property.GetLabel(currentlyDrawNiceNames), true);
            }
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool visibleOnly = VisibleOnly;

            //using (var prop = property.Copy())
            //{
            //    int depth = prop.depth;
            //    if (prop.NextChildSkipArray(visibleOnly))
            //    {
            //        do
            //        {
            //            Debug.Log("CHILD PROPERTY: " + prop.propertyPath);
            //        }
            //        while (prop.Next(false) && depth < prop.depth);
            //    }
            //}

            if (Helper.HasChildren(property, visibleOnly))
            {
                float svs = EditorGUIUtility.standardVerticalSpacing;

                // Foldout
                Rect rect = position;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginProperty(rect, label, property);
                //bool guiChanged = GUI.changed;
                property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
                //GUI.changed = guiChanged; //TODO3: ? Would any code ever care?
                EditorGUI.EndProperty();
                rect.y += rect.height + svs;

                // Children
                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    using (var prop = property.Copy())
                    {
                        int depth = prop.depth;
                        if (prop.NextChildSkipArray(visibleOnly))
                        {
                            do
                            {
                                var childLabel = prop.GetLabel(currentlyDrawNiceNames);
                                rect.height = GetPropertyHeight(prop, childLabel);
                                OnGUI(rect, prop, childLabel);
                                rect.y += rect.height + svs;
                            }
                            while (prop.Next(false, visibleOnly) && depth < prop.depth);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                //TODO: see if in any significant number of older versions have the problem with reorderable lists not calling BeginProperty and EndProperty and therefore no override indicators shown for them?

                if (original != null)
                    original.OnGUI(position, property, property.GetLabel(currentlyDrawNiceNames));
                else
                    EditorGUI.PropertyField(position, property, property.GetLabel(currentlyDrawNiceNames), true); //label
            }
        }
    }

    //TODO: move?
    internal class RawViewDrawer : RawDrawer
    {
        internal AVTargets avTargets;
        internal int frame = 0;
        internal int forceRemoveFromParentsFrame = -1;

        private static readonly GUIContent content = new GUIContent();

        private static GUIStyle style;

        internal static void ResetCurrentWidth()
        {
            currentWidth = EditorGUIUtility.currentViewWidth - AVSettings.S.rawViewWindowLeftPadding - 13;
        }
        internal static float currentWidth;
        private static float CurrentWidth => currentWidth - indentPerLevel * EditorGUI.indentLevel;
        private static readonly float indentPerLevel = (float)typeof(EditorGUI).GetField("kIndentPerLevel", R.nps).GetValue(null);

        internal static float GetUndrawnHeight(List<string> undrawnLabels, int type)
        {
            if (undrawnLabels.Count > 0)
            {
                if (style == null)
                {
                    style = new GUIStyle(EditorStyles.boldLabel);
                    style.wordWrap = true;
                }

                //float slh = EditorGUIUtility.singleLineHeight;
                float svs = EditorGUIUtility.standardVerticalSpacing;
                var s = AVSettings.S;

                float w = CurrentWidth;

                float sum = 0;
                for (int i = 0; i < undrawnLabels.Count; i++)
                {
                    content.text = undrawnLabels[i];
                    sum += style.CalcHeight(content, w) + svs;
                }

                if (type == 0)
                    sum += s.spacingBeforeUndrawnOverrides + s.spacingAfterUndrawnOverrides;
                else if (type == 1)
                    sum += s.spacingBeforeUnclaimedOverrides + s.spacingAfterUnclaimedOverrides;
                else if (type == 2)
                    sum += s.odinSpacingBeforeUndrawnOverrides + s.odinSpacingAfterUndrawnOverrides;

                return sum;
            }
            else
                return 0;
        }
        internal static void DrawUndrawn(AVTargets avTargets, Rect rect, SerializedObject so, List<string> undrawnLabels, List<string> undrawnPaths, int type)
        {
            if (undrawnLabels.Count == 0)
                return;

            //float slh = EditorGUIUtility.singleLineHeight;
            float svs = EditorGUIUtility.standardVerticalSpacing;

            //rect.height = slh;


            if (type == 0)
                rect.y += AVSettings.S.spacingBeforeUndrawnOverrides;
            else if (type == 1)
                rect.y += AVSettings.S.spacingBeforeUnclaimedOverrides;
            else if (type == 2)
                rect.y += AVSettings.S.odinSpacingBeforeUndrawnOverrides;


            var sss = AVSettings.S.overrideIndicatorStyle;

            float w = CurrentWidth;

            //int index = 0; // path == "" ? 0 : path.Length + 1;
            for (int i = 0; i < undrawnLabels.Count; i++)
            {
                var undrawnPath = undrawnPaths[i];
                var undrawnLabel = undrawnLabels[i];

                if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use(); //???
                    GenericMenu menu = new GenericMenu();
                    OverrideIndicator.OnPropertyContextMenu(menu, undrawnPath, so);
                    menu.ShowAsContext();
                }

                var indentedRect = EditorGUI.IndentedRect(rect);

                content.text = undrawnLabel;
                indentedRect.height = style.CalcHeight(content, w);

                var buttonRect = indentedRect;
                buttonRect.width = sss.rawViewWidth;
                buttonRect.x -= sss.rawViewWidth + sss.margins.right;
                buttonRect.height += svs;
                Color discard;
                OverrideIndicator.DrawIndicator(avTargets, false, ref buttonRect, false, out discard, undrawnPath, false, false, null);

                //if (index == 0)
                GUI.Label(indentedRect, undrawnLabel, style);
                //else
                //GUI.Label(indentedRect, undrawnLabel.Substring(index, undrawnLabel.Length - index), style);

                //rect.y += slh + svs;
                rect.y += indentedRect.height + svs;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = base.GetPropertyHeight(property, label);

            var path = avTargets.ConvertToOverridePath(property.propertyPath, property.serializedObject);

            var undrawn = avTargets.RVPrepare(path, frame == forceRemoveFromParentsFrame);
            if (property.isExpanded || !Helper.HasChildren(property)) //TODO:use cached haschildren from base?
            {
                // Note that it claims after base.GetPropertyHeight(), i.e. children get the chance to claim first
                EditorGUI.indentLevel++;
                avTargets.RVClaim(path, avTargets.rvUnclaimedPaths, undrawn);
                h += GetUndrawnHeight(undrawn, 0);
                EditorGUI.indentLevel--;
            }

            return h;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            currentWidth = position.width;

            float baseHeight = base.GetPropertyHeight(property, label);
            Rect rect = position;
            rect.height = baseHeight;

            bool prevExpanded = property.isExpanded;

            EditorGUI.BeginChangeCheck();

            base.OnGUI(rect, property, label);

            var so = property.serializedObject;

            var propertyPath = property.propertyPath;
            var path = avTargets.ConvertToOverridePath(propertyPath, so);

            EditorGUI.indentLevel++;

            rect.y += rect.height;

            List<string> undrawn;
            if (avTargets.rvClaimedUndrawnPaths.TryGetValue(path, out undrawn))
            {
                if (EditorGUI.EndChangeCheck() && prevExpanded != property.isExpanded)
                {
                    //var parentPath = avTargets.ConvertToOverridePath(Helper.ParentPropertyPath(propertyPath), property.serializedObject);
                    var parentPath = Helper.ParentOverridePath(path, true);
                    if (prevExpanded)
                    {
                        //if (false) //TODO: setting for enabling this
                        //{
                        //    //Debug.Log("Giving " + undrawn.Count + " " + parentPath);
                        //    if (parentPath != null)
                        //        avTargets.rvClaimedUndrawnPaths[parentPath].AddRange(undrawn);
                        //    else
                        //        avTargets.rvUnclaimedPaths.AddRange(undrawn);
                        //    undrawn.Clear();
                        //}
                    }
                    else if (parentPath != null)
                    {
                        //Debug.Log(parentPath + "\n" + path + "\n" + Helper.ParentPropertyPath(path));
                        var parentUndrawn = avTargets.rvClaimedUndrawnPaths[parentPath];
                        avTargets.RVClaim(path, parentUndrawn, undrawn);
                        forceRemoveFromParentsFrame = frame + 1;
                    }
                    //Debug.Log(parentPath + "\n" + path + "\n" + Helper.ParentPropertyPath(path));
                }

                if (property.isExpanded || !Helper.HasChildren(property)) //TODO:use cached haschildren from base?
                    DrawUndrawn(avTargets, rect, so, undrawn, undrawn, 0);
            }
            else
            {
                EditorGUI.EndChangeCheck();
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif
