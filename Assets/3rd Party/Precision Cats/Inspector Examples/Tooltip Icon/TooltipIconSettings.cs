#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
#if ASSET_VARIANTS
using AssetVariants;
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif
using Object = UnityEngine.Object;

namespace PrecisionCats
{
    [CreateAssetMenu(menuName = "TooltipIconSettings")]
    [System.Serializable]
    public class TooltipIconSettings : ScriptableObject
    {
#if ASSET_VARIANTS
        [InitializeOnLoadMethod]
        private static void Init()
        {
            AssetVariantsSettingsWindow.tabs.Add(new TooltipTab()
            {
                title = new GUIContent("TISettings", "Tooltip Icon Setting.asset\nNote that tooltip icons can sometimes be missing especially with built-in types, as certain properties might not be wrapped with BeginProperty() and EndProperty().")
            });
        }
        public class TooltipTab : AssetVariantsSettingsWindow.EditorTab
        {
            protected override Object Target => S;
        }
#endif

        private static TooltipIconSettings s;
        public static TooltipIconSettings S
        {
            get
            {
                if (s == null)
                {
                    const string guid = "f2317e21de1fff842ae4fdbc590e79ff";
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    s = AssetDatabase.LoadAssetAtPath<TooltipIconSettings>(path);
                    if (s == null)
                    {
                        s = new TooltipIconSettings();
                        Debug.LogWarning("TooltipIconSettings.s == null");
                    }
                }
                return s;
            }
            set
            {
                s = value;
            }
        }

        public bool disabled = false;
        public Side side = Side.Right;
        public Texture2D lightTexture;
        public Texture2D darkTexture;
        public float subtract = 0;
        [Tooltip("Does not work in IMGUI.")]
        public float spacing = 1;
        public float yOffset = 4;
        public Vector2 size = new Vector2(10, 10);

        public enum Side { Left, Right }
    }

    public class TooltipIcon : PropertyModifier
    {
        public override bool Disabled => TooltipIconSettings.S.disabled;

        public override string SystemKey => "TooltipIcon";

        private static readonly MethodInfo getFieldInfoFromPropertyPathMI = AVCommonHelper.FindType("UnityEditor.ScriptAttributeUtility").GetMethod("GetFieldInfoFromPropertyPath", BindingFlags.NonPublic | BindingFlags.Static);
        private static object[] args = new object[3];
        public static bool HasTooltip(SerializedProperty prop)
        {
            //TODO: what is the performance of this? Use caching?
            if (!string.IsNullOrEmpty(prop.tooltip))
                return true;
            args[0] = prop.serializedObject.targetObject.GetType();
            args[1] = prop.propertyPath;
            var fieldInfo = (FieldInfo)getFieldInfoFromPropertyPathMI.Invoke(null, args);
            if (fieldInfo == null)
                return false;
            //var ta = fieldInfo.GetCustomAttribute<TooltipAttribute>();
            //if (ta == null)
            //    return;
            //if (string.IsNullOrEmpty(ta.tooltip))
            //    return;
            return System.Attribute.IsDefined(fieldInfo, typeof(TooltipAttribute));
        }

        public override void Draw(Rect r, SerializedProperty prop)
        {
            var s = TooltipIconSettings.S;

            if (s.disabled)
                return;

            if (!HasTooltip(prop))
                return;

            if (s.side == TooltipIconSettings.Side.Left)
                r.x += s.subtract;
            r.width -= s.subtract;

            r = EditorGUI.IndentedRect(r);

            var tooltipRect = r;
            if (s.side == TooltipIconSettings.Side.Left)
                tooltipRect.x -= s.spacing + s.size.x; //+ alignmentL
            else
                tooltipRect.x += r.width + s.spacing; // + alignmentR
            tooltipRect.width = s.size.x;
            tooltipRect.height = s.size.y;
            tooltipRect.y += s.yOffset;

            GUI.DrawTexture(tooltipRect, EditorGUIUtility.isProSkin ? s.darkTexture : s.lightTexture);
        }

#if UNITY_2019_1_OR_NEWER
        public override bool ShouldModify(SerializedObject serializedObject)
        {
            return true;
        }
        public override void Modify(PropertyField propertyField, SerializedProperty prop)
        {
            if (!HasTooltip(prop))
                return;

            var s = TooltipIconSettings.S;

            var icon = new VisualElement();
            icon.style.position = Position.Absolute;
            icon.style.backgroundImage = EditorGUIUtility.isProSkin ? s.darkTexture : s.lightTexture;
            icon.style.width = s.size.x;
            icon.style.height = s.size.y;
            var lr = -s.spacing - s.size.x;
            if (s.side == TooltipIconSettings.Side.Left)
            {
                propertyField.style.marginLeft = s.subtract;
                icon.style.left = lr + alignmentL;
            }
            else
            {
                propertyField.style.marginRight = s.subtract;
                icon.style.right = lr + alignmentR;
            }
            icon.style.top = s.yOffset;

            propertyField.Add(icon);
        }
#endif
    }
}
#endif
