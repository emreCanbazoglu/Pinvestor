#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(NegativeColorAttribute))]
public class NegativeColorDrawer : PropertyDrawer
{
    public static float BUTTON_FRACTION = 0.4f;

    public int type;

#if !UNITY_2018_1_OR_NEWER
    private static readonly ColorPickerHDRConfig colorPickerHDRConfig = new ColorPickerHDRConfig(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue);
#endif

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, label, property); //label = 
        {
            rect = EditorGUI.PrefixLabel(rect, label);


            //Finds Button Label
            string buttonLabel;
            if (type == 0)
                buttonLabel = "";
            else if (type == 1)
                buttonLabel = "V";
            else if (type == 2)
                buttonLabel = "R";
            else if (type == 3)
                buttonLabel = "G";
            else if (type == 4)
                buttonLabel = "B";
            else //if (type == 5)
                buttonLabel = "A";


            //Button
            var r = rect;
            r.width *= BUTTON_FRACTION;
            if (GUI.Button(r, buttonLabel))
                type = (type + 1) % 6;
            rect.x += r.width;
            rect.width *= 1 - BUTTON_FRACTION;


            //Modifies
            EditorGUI.BeginChangeCheck();
            var val = property.colorValue;
            if (type == 0)
#if !UNITY_2018_1_OR_NEWER
                val = EditorGUI.ColorField(rect, GUIContent.none, val, false, true, true, colorPickerHDRConfig);
#else
                val = EditorGUI.ColorField(rect, GUIContent.none, val, false, true, true);
#endif
            else if (type == 1)
            {
                var b = EditorGUI.FloatField(rect, GUIContent.none, (val.r + val.g + val.b) * 0.3333333333333f);
                val = new Color(b, b, b, val.a);
            }
            else
                val[type - 2] = EditorGUI.FloatField(rect, GUIContent.none, val[type - 2]);


            //Applies
            if (EditorGUI.EndChangeCheck())
            {
                property.colorValue = val;
            }
        }
        EditorGUI.EndProperty();
    }
}

/*
                //Pre Color
                const float preColor = 0.25f;
                var r = rect;
                r.width *= preColor;
                val = EditorGUI.ColorField(r, GUIContent.none, val, false, true, true);
                rect.x += r.width;
                rect.width *= 1 - preColor;

                //Float Pieces
                rect.width *= 0.25f;
                void Do(ref float f)
                {
                    f = EditorGUI.FloatField(rect, GUIContent.none, f);
                    rect.x += rect.width;
                }

                Do(ref val.r);
                Do(ref val.g);
                Do(ref val.b);
                Do(ref val.a);
*/
#endif