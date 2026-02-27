using System;
using System.Collections.Generic;

namespace Pinvestor.GameConfigSystem
{
    public interface IGameConfigModuleParser<in TJson, out TModel>
    {
        TModel Parse(TJson json);
    }

    public sealed class CompanyConfigModuleParser
        : IGameConfigModuleParser<CompanyConfigSectionJsonDto, IReadOnlyList<CompanyConfigModel>>
    {
        public IReadOnlyList<CompanyConfigModel> Parse(CompanyConfigSectionJsonDto json)
        {
            var companies = new List<CompanyConfigModel>();
            var entries = json != null
                ? json.entries
                : Array.Empty<CompanyConfigEntryJsonDto>();

            if (entries == null)
            {
                return companies;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                CompanyConfigEntryJsonDto entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                Dictionary<string, float> attributes = ParseNamedValues(entry.attributes);
                Dictionary<string, float> values = ParseNamedValues(entry.values);

                bool hasMaxHp = attributes.TryGetValue(CompanyConfigAttributeKeys.MaxHP, out float maxHp);
                bool hasRph = attributes.TryGetValue(CompanyConfigAttributeKeys.RPH, out float rph);
                bool hasTurnlyCost = attributes.TryGetValue(CompanyConfigAttributeKeys.TurnlyCost, out float turnlyCost);

                companies.Add(new CompanyConfigModel(
                    entry.companyId ?? string.Empty,
                    attributes,
                    values,
                    maxHp,
                    rph,
                    turnlyCost,
                    hasMaxHp,
                    hasRph,
                    hasTurnlyCost));
            }

            return companies;
        }

        private static Dictionary<string, float> ParseNamedValues(NamedFloatJsonDto[] values)
        {
            var result = new Dictionary<string, float>();
            if (values == null)
            {
                return result;
            }

            for (int i = 0; i < values.Length; i++)
            {
                NamedFloatJsonDto item = values[i];
                if (item == null || string.IsNullOrWhiteSpace(item.key))
                {
                    continue;
                }

                result[item.key] = item.value;
            }

            return result;
        }
    }

    public sealed class NamedValueModuleParser
        : IGameConfigModuleParser<NamedValueSectionJsonDto, NamedConfigSectionModel>
    {
        private readonly string _configType;

        public NamedValueModuleParser(string configType)
        {
            _configType = configType;
        }

        public NamedConfigSectionModel Parse(NamedValueSectionJsonDto json)
        {
            var values = new Dictionary<string, float>();
            NamedFloatJsonDto[] entries = json != null ? json.values : Array.Empty<NamedFloatJsonDto>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    NamedFloatJsonDto item = entries[i];
                    if (item == null || string.IsNullOrWhiteSpace(item.key))
                    {
                        continue;
                    }

                    values[item.key] = item.value;
                }
            }

            return new NamedConfigSectionModel(_configType, values);
        }
    }

    public sealed class BallConfigModuleParser
        : IGameConfigModuleParser<BallConfigJsonDto, BallConfigModel>
    {
        public BallConfigModel Parse(BallConfigJsonDto json)
        {
            if (json == null)
            {
                return new BallConfigModel(10f, 10f);
            }

            return new BallConfigModel(
                json.shootSpeed,
                json.previewLength);
        }
    }

    public sealed class RunCycleConfigModuleParser
        : IGameConfigModuleParser<RunCycleConfigJsonDto, RunCycleConfigModel>
    {
        public RunCycleConfigModel Parse(RunCycleConfigJsonDto json)
        {
            var rounds = new List<RoundCycleConfigEntryModel>();
            RoundCycleConfigJsonDto[] entries = json != null
                ? json.rounds
                : Array.Empty<RoundCycleConfigJsonDto>();

            if (entries == null)
                return new RunCycleConfigModel(rounds);

            for (int i = 0; i < entries.Length; i++)
            {
                RoundCycleConfigJsonDto entry = entries[i];
                if (entry == null)
                    continue;

                rounds.Add(new RoundCycleConfigEntryModel(
                    entry.roundId ?? string.Empty,
                    entry.turnCount,
                    entry.requiredWorth));
            }

            return new RunCycleConfigModel(rounds);
        }
    }
}
