using Pinvestor.CompanySystem;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.UI
{
    /// <summary>
    /// Compact MVP visual language shared by world-space gameplay UI and HUD widgets.
    /// Colors are paired with text/glyph labels so gameplay state is never color-only.
    /// </summary>
    public static class MvpVisualTheme
    {
        public static Color TextPrimary => new Color32(247, 243, 248, 255);
        public static Color TextMuted => new Color32(190, 180, 198, 255);
        public static Color Revenue => new Color32(76, 227, 154, 255);
        public static Color Cash => new Color32(247, 201, 72, 255);
        public static Color Danger => new Color32(255, 91, 110, 255);
        public static Color Deferred => new Color32(143, 112, 255, 255);

        public static Color GetCategoryColor(ECompanyCategory category)
        {
            return category switch
            {
                ECompanyCategory.SocialMedia => new Color32(255, 103, 154, 255),
                ECompanyCategory.ConsumerTech => new Color32(247, 201, 72, 255),
                ECompanyCategory.FinTech => new Color32(65, 209, 222, 255),
                ECompanyCategory.EnterpriseTech => new Color32(143, 112, 255, 255),
                _ => new Color32(126, 135, 150, 255),
            };
        }

        public static Color GetPhaseColor(ETurnPhase phase)
        {
            return phase switch
            {
                ETurnPhase.Offer => Cash,
                ETurnPhase.Placement => new Color32(65, 209, 222, 255),
                ETurnPhase.Launch => Revenue,
                ETurnPhase.Resolution => Deferred,
                _ => TextMuted,
            };
        }

        public static string GetCategoryLabel(ECompanyCategory category)
        {
            return category switch
            {
                ECompanyCategory.SocialMedia => "SOCIAL",
                ECompanyCategory.ConsumerTech => "CONSUMER",
                ECompanyCategory.FinTech => "FINTECH",
                ECompanyCategory.EnterpriseTech => "ENTERPRISE",
                _ => "UNCATEGORIZED",
            };
        }

        public static string GetCategoryGlyph(ECompanyCategory category)
        {
            return category switch
            {
                ECompanyCategory.SocialMedia => "#",
                ECompanyCategory.ConsumerTech => "C",
                ECompanyCategory.FinTech => "$",
                ECompanyCategory.EnterpriseTech => "E",
                _ => "?",
            };
        }

        public static string HumanizeCompanyName(string companyId)
        {
            if (string.IsNullOrWhiteSpace(companyId))
                return "Unknown Company";

            var result = new System.Text.StringBuilder(companyId.Length + 4);
            for (int i = 0; i < companyId.Length; i++)
            {
                char current = companyId[i];
                if (i > 0
                    && char.IsUpper(current)
                    && (char.IsLower(companyId[i - 1])
                        || (i + 1 < companyId.Length && char.IsLower(companyId[i + 1]))))
                {
                    result.Append(' ');
                }

                result.Append(current);
            }

            return result.ToString();
        }

        public static string GetMonogram(string companyId)
        {
            string humanized = HumanizeCompanyName(companyId);
            string[] words = humanized.Split(
                new[] { ' ' },
                System.StringSplitOptions.RemoveEmptyEntries);

            if (words.Length >= 2)
                return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";

            if (words.Length == 1 && words[0].Length >= 2)
                return words[0].Substring(0, 2).ToUpperInvariant();

            return humanized.Substring(0, 1).ToUpperInvariant();
        }
    }
}
