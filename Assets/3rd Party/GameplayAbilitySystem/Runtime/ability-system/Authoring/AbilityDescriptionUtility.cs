using System.Collections.Generic;
using System.Text;
using AttributeSystem.Authoring;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    public static class AbilityDescriptionUtility
    {
        private const string PositiveColor = "#00FF66";
        private const string NegativeColor = "#FF4D4D";
        private const string NeutralColor = "#FFD700";

        public static string GenerateManualDescription(
            string template,
            GameplayEffectModifier[] modifiers,
            float duration = 0)
        {
            StringBuilder sb = new();
            string line = ReplaceTokens(template, modifiers);
            sb.AppendLine(line);

            if (duration > 0)
                sb.AppendLine($"<color=#FFD700>Effect lasts for {duration:F0} seconds.</color>");

            return sb.ToString().TrimEnd();
        }

        private static string ReplaceTokens(string template, GameplayEffectModifier[] modifiers)
        {
            foreach (var mod in modifiers)
            {
                if (string.IsNullOrWhiteSpace(mod.DescriptionKey))
                    continue;

                string attrName = FormatAttributeName(mod.Attribute);
                float baseValue = mod.ModifierMagnitude?.GetPreviewValue() ?? 0f;
                float scaledValue = baseValue * mod.Multiplier;

                string valueText = mod.ModifierOperator == EAttributeModifier.Multiply
                    ? $"{scaledValue:P0}"
                    : $"{(scaledValue > 0 ? "+" : "")}{scaledValue:F0}";

                string toneColor = GetColor(mod.Tone);
                string valueRich = $"<b><color={toneColor}>{valueText}</color></b>";

                string attrColor = ColorUtility.ToHtmlStringRGB(mod.Attribute.AttributeColor);
                string attrRich = $"<b><color=#{attrColor}>{attrName}</color></b>";

                template = template.Replace($"{{attribute:{mod.DescriptionKey}}}", attrRich);
                template = template.Replace($"{{value:{mod.DescriptionKey}}}", valueRich);
            }

            return template;
        }

        private static string FormatAttributeName(AttributeScriptableObject attribute)
        {
            if (attribute == null) return "<missing attribute>";

            if (!string.IsNullOrWhiteSpace(attribute.Name))
                return attribute.Name;

            return attribute.name.Replace("_", " ").ToLower();
        }

        private static string GetColor(EDescriptionTone tone)
        {
            return tone switch
            {
                EDescriptionTone.Positive => PositiveColor,
                EDescriptionTone.Negative => NegativeColor,
                _ => NeutralColor
            };
        }
    }
}