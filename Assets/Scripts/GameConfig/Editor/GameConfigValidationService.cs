using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.GameConfigSystem.Editor
{
    public sealed class GameConfigValidationService
    {
        public GameConfigValidationResult Validate(GameConfigAuthoringAsset asset)
        {
            var errors = new List<string>();
            if (asset == null)
            {
                errors.Add("Authoring asset is null.");
                return new GameConfigValidationResult(errors);
            }

            if (asset.SchemaVersion <= 0)
            {
                errors.Add("SchemaVersion must be greater than 0.");
            }

            var companyIds = new HashSet<string>();
            foreach (CompanyAuthoringEntry company in asset.Companies ?? System.Array.Empty<CompanyAuthoringEntry>())
            {
                if (company == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(company.companyId))
                {
                    errors.Add("Company entry has empty companyId.");
                    continue;
                }

                if (!companyIds.Add(company.companyId))
                {
                    errors.Add($"Duplicate companyId: {company.companyId}");
                }

                ValidateNamedEntries($"company:{company.companyId}:attributes", company.attributes, errors);
                ValidateNamedEntries($"company:{company.companyId}:values", company.values, errors);
            }

            ValidateNamedEntries("balance", asset.Balance, errors);
            ValidateNamedEntries("roundCriteria", asset.RoundCriteria, errors);
            ValidateRunCycle("runCycle", asset.RunCycleRounds, errors);
            ValidateBallConfig("ball", asset.Ball, errors);
            ValidateNamedEntries("shop", asset.Shop, errors);

            return new GameConfigValidationResult(errors);
        }

        private static void ValidateNamedEntries(
            string sectionName,
            NamedFloatAuthoringEntry[] entries,
            List<string> errors)
        {
            var seen = new HashSet<string>();
            foreach (NamedFloatAuthoringEntry entry in entries ?? System.Array.Empty<NamedFloatAuthoringEntry>())
            {
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    errors.Add($"{sectionName}: entry has empty key.");
                    continue;
                }

                if (!seen.Add(entry.key))
                {
                    errors.Add($"{sectionName}: duplicate key '{entry.key}'.");
                }
            }
        }

        private static void ValidateBallConfig(
            string sectionName,
            BallAuthoringConfig ballConfig,
            List<string> errors)
        {
            if (ballConfig == null)
            {
                errors.Add($"{sectionName}: config is null.");
                return;
            }

            if (ballConfig.shootSpeed <= 0f)
            {
                errors.Add($"{sectionName}: shootSpeed must be > 0.");
            }

            if (ballConfig.previewLength <= 0f)
            {
                errors.Add($"{sectionName}: previewLength must be > 0.");
            }
        }

        private static void ValidateRunCycle(
            string sectionName,
            RoundCycleAuthoringEntry[] rounds,
            List<string> errors)
        {
            var seenRoundIds = new HashSet<string>();
            foreach (RoundCycleAuthoringEntry round in rounds ?? System.Array.Empty<RoundCycleAuthoringEntry>())
            {
                if (round == null)
                    continue;

                if (string.IsNullOrWhiteSpace(round.roundId))
                {
                    errors.Add($"{sectionName}: round has empty roundId.");
                    continue;
                }

                if (!seenRoundIds.Add(round.roundId))
                    errors.Add($"{sectionName}: duplicate roundId '{round.roundId}'.");

                if (round.turnCount < 0)
                    errors.Add($"{sectionName}:{round.roundId}: turnCount cannot be negative.");

                if (round.requiredWorth < 0f)
                    errors.Add($"{sectionName}:{round.roundId}: requiredWorth cannot be negative.");
            }
        }
    }

    public readonly struct GameConfigValidationResult
    {
        public readonly IReadOnlyList<string> Errors;
        public bool IsValid => Errors == null || Errors.Count == 0;

        public GameConfigValidationResult(IReadOnlyList<string> errors)
        {
            Errors = errors;
        }

        public string ToMultilineString()
        {
            if (IsValid)
            {
                return "GameConfig validation passed.";
            }

            return string.Join("\n", Errors);
        }
    }
}
