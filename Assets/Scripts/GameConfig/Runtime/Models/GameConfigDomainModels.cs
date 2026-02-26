using System.Collections.Generic;

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
        public NamedConfigSectionModel Ball { get; }
        public NamedConfigSectionModel Shop { get; }

        public GameConfigRootModel(
            int schemaVersion,
            string generatedAtUtc,
            IReadOnlyList<CompanyConfigModel> companies,
            NamedConfigSectionModel balance,
            NamedConfigSectionModel roundCriteria,
            NamedConfigSectionModel ball,
            NamedConfigSectionModel shop)
        {
            SchemaVersion = schemaVersion;
            GeneratedAtUtc = generatedAtUtc;
            Companies = companies;
            Balance = balance;
            RoundCriteria = roundCriteria;
            Ball = ball;
            Shop = shop;
        }
    }

    public sealed class CompanyConfigModel : ConfigModelBase
    {
        public string CompanyId { get; }
        public IReadOnlyDictionary<string, float> Attributes { get; }
        public IReadOnlyDictionary<string, float> Values { get; }

        public CompanyConfigModel(
            string companyId,
            IReadOnlyDictionary<string, float> attributes,
            IReadOnlyDictionary<string, float> values)
            : base("company")
        {
            CompanyId = companyId;
            Attributes = attributes;
            Values = values;
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
