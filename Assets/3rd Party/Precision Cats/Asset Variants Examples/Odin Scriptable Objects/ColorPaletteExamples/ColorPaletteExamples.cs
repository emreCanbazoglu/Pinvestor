#if ODIN_INSPECTOR
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Asset Variants Odin Examples/ColorPaletteExamples")]
public class ColorPaletteExamples : ScriptableObject
{
    [ColorPalette]
    public Color ColorOptions;

    [ColorPalette("Underwater")]
    public Color UnderwaterColor;

    [ColorPalette("Fall"), HideLabel]
    public Color WideColorPalette;

    [ColorPalette("My Palette")]
    public Color MyColor;

    [ColorPalette("Clovers")]
    public Color[] ColorArray;
}
#endif
