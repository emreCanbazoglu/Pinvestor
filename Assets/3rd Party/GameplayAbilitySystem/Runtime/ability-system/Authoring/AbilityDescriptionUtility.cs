using System.Collections.Generic;
using System.Text;
using AttributeSystem.Authoring;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    public interface IAbilityTargetFilter
    {
        string GetSubjectQualifier(); // e.g. "of companies"
        string GetLocationQualifier(); // e.g. "on the same row"
    }

    public static class AbilityDescriptionUtility
    {
        private const string PositiveColor = "#00FF66";
        private const string NegativeColor = "#FF4D4D";
        private const string NeutralColor = "#FFD700";

        public static string GenerateFullAbilityDescription(
            AbstractAbilityScriptableObject ability,
            IEnumerable<GameplayEffectScriptableObject> effects,
            IEnumerable<IAbilityTargetFilter> filters,
            float level = 1)
        {
            StringBuilder sb = new();

            var (subjectQualifier, locationQualifier) = BuildTargetingPhrases(filters);

            foreach (var effect in effects)
            {
                if (effect == null || effect.gameplayEffect.Modifiers == null)
                    continue;

                sb.AppendLine(GenerateEffectDescription(effect, subjectQualifier, locationQualifier, level));
            }

            return sb.ToString().TrimEnd();
        }

        public static string GenerateEffectDescription(
            GameplayEffectScriptableObject effect,
            string subjectQualifier,
            string locationQualifier,
            float level)
        {
            var modifiers = effect.gameplayEffect.Modifiers;
            var metas = effect.ModifierDescriptions;

            if (modifiers == null || modifiers.Length == 0)
                return string.Empty;

            StringBuilder sb = new();

            for (int i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i];
                var meta = (i < metas.Count)
                    ? metas[i]
                    : new EffectDescriptionMeta { Verb = "affects", Tone = EDescriptionTone.Neutral };

                string attrName = FormatAttributeName(modifier.Attribute);
                float baseValue = modifier.ModifierMagnitude?.GetPreviewValue() ?? 0f;
                float scaledValue = baseValue * modifier.Multiplier * level;

                string valueText = modifier.ModifierOperator == EAttributeModifier.Multiply
                    ? $"{scaledValue:P0}" // e.g., 0.2 → 20%
                    : $"{(scaledValue > 0 ? "+" : "")}{scaledValue:F0}";

                string color = GetColor(meta.Tone);
                string coloredValue = $"<color={color}>{valueText}</color>";

                string fullAttr = $"the {attrName}";
                if (!string.IsNullOrWhiteSpace(subjectQualifier))
                    fullAttr += $" {subjectQualifier}";
                if (!string.IsNullOrWhiteSpace(locationQualifier))
                    fullAttr += $" {locationQualifier}";

                string boldAttr = $"<b>{fullAttr}</b>";
                string line = $"{meta.Verb} {boldAttr} by {coloredValue}.";

                sb.AppendLine(line);
            }

            if (effect.gameplayEffect.DurationPolicy == EDurationPolicy.HasDuration)
            {
                float duration = effect.gameplayEffect.DurationModifier?.GetPreviewValue()
                                 ?? effect.gameplayEffect.DurationMultiplier;

                sb.AppendLine($"<color={NeutralColor}>Effect lasts for {duration:F0} seconds.</color>");
            }

            return sb.ToString().TrimEnd();
        }
        
        public static string GenerateActionDescriptions(
            List<AbilityActionDescription> actions,
            GameplayEffectScriptableObject effect,
            float duration = 0)
        {
            var modifiers = effect?.gameplayEffect.Modifiers ?? null;
            var metas = effect?.ModifierDescriptions ?? null;

            StringBuilder sb = new();

            foreach (var action in actions)
            {
                string line = action.Template;

                if (modifiers != null && action.ModifierIndex < modifiers.Length)
                {
                    var mod = modifiers[action.ModifierIndex];
                    var meta = (metas != null && action.ModifierIndex < metas.Count)
                        ? metas[action.ModifierIndex]
                        : new EffectDescriptionMeta { Verb = "affects", Tone = EDescriptionTone.Neutral };

                    float baseValue = mod.ModifierMagnitude?.GetPreviewValue() ?? 0f;
                    float scaledValue = baseValue * mod.Multiplier;
                    string valueText = mod.ModifierOperator == EAttributeModifier.Multiply
                        ? $"{scaledValue:P0}" : $"{(scaledValue > 0 ? "+" : "")}{scaledValue:F0}";
                    string color = GetColor(meta.Tone);
                    string coloredValue = $"<color={color}>{valueText}</color>";

                    string attrName = FormatAttributeName(mod.Attribute);

                    line = line.Replace("{attribute}", attrName);
                    line = line.Replace("{value}", coloredValue);
                }

                sb.AppendLine(line);

                if (action.ShowDuration && duration > 0)
                {
                    sb.AppendLine($"<color=#FFD700>Effect lasts for {duration:F0} seconds.</color>");
                }
            }

            return sb.ToString().TrimEnd();
        }


        public static (string subjectQualifier, string locationQualifier) BuildTargetingPhrases(
            IEnumerable<IAbilityTargetFilter> filters)
        {
            List<string> subjects = new();
            List<string> locations = new();

            foreach (var filter in filters)
            {
                if (filter == null) continue;

                var subject = filter.GetSubjectQualifier();
                var location = filter.GetLocationQualifier();

                if (!string.IsNullOrWhiteSpace(subject))
                    subjects.Add(subject);
                if (!string.IsNullOrWhiteSpace(location))
                    locations.Add(location);
            }

            return (
                subjectQualifier: string.Join(" ", subjects), // e.g., "of companies"
                locationQualifier: string.Join(" ", locations) // e.g., "on the same row"
            );
        }

        private static string FormatAttributeName(AttributeScriptableObject attribute)
        {
            if (attribute == null) return "<missing attribute>";

            string name = !string.IsNullOrWhiteSpace(attribute.Name)
                ? attribute.Name
                : !string.IsNullOrWhiteSpace(attribute.Name)
                    ? attribute.Name
                    : attribute.name.Replace("_", " ").ToLower();

            string colorHex = ColorUtility.ToHtmlStringRGB(attribute.AttributeColor);
            return $"<color=#{colorHex}>{name}</color>";
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