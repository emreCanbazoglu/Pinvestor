using System.Collections.Generic;
using AttributeSystem.Authoring;

namespace Pinvestor.GameConfigSystem
{
    public abstract class ConfigModelBase
    {
        public string ConfigType { get; }

        protected ConfigModelBase(string configType)
        {
            ConfigType = configType;
        }
    }

    public sealed class GameConfigRootModel
    {
        public int SchemaVersion { get; }
        public string GeneratedAtUtc { get; }
        public IReadOnlyList<CompanyConfigModel> Companies { get; }
        public NamedConfigSectionModel Balance { get; }
        public NamedConfigSectionModel RoundCriteria { get; }
        public RunCycleConfigModel RunCycle { get; }
        public BallConfigModel Ball { get; }
        public NamedConfigSectionModel Shop { get; }

        public GameConfigRootModel(
            int schemaVersion,
            string generatedAtUtc,
            IReadOnlyList<CompanyConfigModel> companies,
            NamedConfigSectionModel balance,
            NamedConfigSectionModel roundCriteria,
            RunCycleConfigModel runCycle,
            BallConfigModel ball,
            NamedConfigSectionModel shop)
        {
            SchemaVersion = schemaVersion;
            GeneratedAtUtc = generatedAtUtc;
            Companies = companies;
            Balance = balance;
            RoundCriteria = roundCriteria;
            RunCycle = runCycle;
            Ball = ball;
            Shop = shop;
        }
    }

    public sealed class RunCycleConfigModel : ConfigModelBase
    {
        public IReadOnlyList<RoundCycleConfigEntryModel> Rounds { get; }

        public RunCycleConfigModel(IReadOnlyList<RoundCycleConfigEntryModel> rounds)
            : base("runCycle")
        {
            Rounds = rounds;
        }
    }

    public sealed class RoundCycleConfigEntryModel
    {
        public string RoundId { get; }
        public int TurnCount { get; }
        public float RequiredWorth { get; }

        public RoundCycleConfigEntryModel(
            string roundId,
            int turnCount,
            float requiredWorth)
        {
            RoundId = roundId;
            TurnCount = turnCount;
            RequiredWorth = requiredWorth;
        }
    }

    public sealed class BallConfigModel : ConfigModelBase
    {
        public float ShootSpeed { get; }
        public float PreviewLength { get; }

        public BallConfigModel(
            float shootSpeed,
            float previewLength)
            : base("ball")
        {
            ShootSpeed = shootSpeed;
            PreviewLength = previewLength;
        }
    }

    public sealed class CompanyConfigModel : ConfigModelBase
    {
        public string CompanyId { get; }
        public IReadOnlyDictionary<string, float> Attributes { get; }
        public IReadOnlyDictionary<string, float> Values { get; }
        public float MaxHP { get; }
        public float RevenuePerHit { get; }
        public float TurnlyCost { get; }
        public bool HasMaxHP { get; }
        public bool HasRevenuePerHit { get; }
        public bool HasTurnlyCost { get; }

        public CompanyConfigModel(
            string companyId,
            IReadOnlyDictionary<string, float> attributes,
            IReadOnlyDictionary<string, float> values,
            float maxHP,
            float revenuePerHit,
            float turnlyCost,
            bool hasMaxHP,
            bool hasRevenuePerHit,
            bool hasTurnlyCost)
            : base("company")
        {
            CompanyId = companyId;
            Attributes = attributes;
            Values = values;
            MaxHP = maxHP;
            RevenuePerHit = revenuePerHit;
            TurnlyCost = turnlyCost;
            HasMaxHP = hasMaxHP;
            HasRevenuePerHit = hasRevenuePerHit;
            HasTurnlyCost = hasTurnlyCost;
        }

        public bool TryGetAttributeByScriptableObject(
            AttributeScriptableObject attribute,
            out float value)
        {
            value = 0f;
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.UniqueId))
            {
                return false;
            }

            return Attributes.TryGetValue(attribute.UniqueId, out value);
        }

        public bool TryGetMaxHP(out float value)
        {
            value = MaxHP;
            return HasMaxHP;
        }

        public bool TryGetRevenuePerHit(out float value)
        {
            value = RevenuePerHit;
            return HasRevenuePerHit;
        }

        public bool TryGetTurnlyCost(out float value)
        {
            value = TurnlyCost;
            return HasTurnlyCost;
        }
    }

    public sealed class NamedConfigSectionModel : ConfigModelBase
    {
        public IReadOnlyDictionary<string, float> Values { get; }

        public NamedConfigSectionModel(
            string configType,
            IReadOnlyDictionary<string, float> values)
            : base(configType)
        {
            Values = values;
        }

        public bool TryGetValue(string key, out float value)
        {
            return Values.TryGetValue(key, out value);
        }
    }
}
