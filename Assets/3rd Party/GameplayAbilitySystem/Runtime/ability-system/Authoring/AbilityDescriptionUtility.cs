using System.Collections.Generic;
using System.Text;
using AttributeSystem.Authoring;

namespace AbilitySystem.Authoring
{
    public static class AbilityDescriptionUtility
    {
        private const string PositiveColor = "#00FF66"; // green
        private const string NegativeColor = "#FF4D4D"; // red
        private const string NeutralColor = "#FFD700"; // gold

        public static string GenerateFromEffects(
            IEnumerable<GameplayEffectScriptableObject> effects, float level = 1)
        {
            StringBuilder sb = new();

            foreach (var effect in effects)
            {
                if (effect == null || effect.gameplayEffect.Modifiers == null)
                    continue;

                sb.AppendLine(GenerateCasualEffectDescription(effect, level));
            }

            return sb.ToString().TrimEnd();
        }

        public static string GenerateCasualEffectDescription(
            GameplayEffectScriptableObject effect, float level)
        {
            StringBuilder description = new();

            foreach (var modifier in effect.gameplayEffect.Modifiers)
            {
                if (modifier.Attribute == null)
                    continue;

                string readableName = FormatAttributeName(modifier.Attribute);
                float baseValue = modifier.ModifierMagnitude?.GetPreviewValue() ?? 0f;
                float scaledValue = baseValue * modifier.Multiplier * level;

                /*AttributeEffectType effectType = modifier.Attribute.EffectType;

                string valueText = modifier.ModifierOperator == EAttributeModifier.Multiply
                    ? $"{scaledValue:F1}×"
                    : $"{(scaledValue > 0 ? "+" : "")}{scaledValue:F0}";

                string valueColor = GetColorForEffect(effectType, scaledValue);
                string coloredValue = $"<color={valueColor}>{valueText}</color>";
                string boldAttr = $"<b>{readableName}</b>";

                description.AppendLine(
                    FormatLine(
                        effectType,
                        boldAttr,
                        coloredValue,
                        modifier.ModifierOperator));*/
            }

            if (effect.gameplayEffect.DurationPolicy == EDurationPolicy.HasDuration)
            {
                float duration = effect.gameplayEffect.DurationModifier?.GetPreviewValue()
                                 ?? effect.gameplayEffect.DurationMultiplier;
                description.AppendLine($"<color={NeutralColor}>Effect lasts for {duration:F0} seconds.</color>");
            }

            return description.ToString().TrimEnd();
        }

        private static string FormatAttributeName(AttributeScriptableObject attribute)
        {
            if (!string.IsNullOrEmpty(attribute.Name))
                return attribute.Name;

            return attribute.name.Replace("_", " ").ToLower();
        }

    }
}