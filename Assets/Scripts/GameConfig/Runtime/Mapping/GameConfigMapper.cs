using System.Collections.Generic;

namespace Pinvestor.GameConfigSystem
{
    public static class GameConfigMapper
    {
        public static GameConfigRootModel Map(GameConfigRootJsonDto dto)
        {
            if (dto == null)
            {
                dto = new GameConfigRootJsonDto();
            }

            var companies = new List<CompanyConfigModel>();
            var companyEntries = dto.company?.entries ?? System.Array.Empty<CompanyConfigEntryJsonDto>();
            foreach (CompanyConfigEntryJsonDto entry in companyEntries)
            {
                if (entry == null)
                {
                    continue;
                }

                companies.Add(new CompanyConfigModel(
                    entry.companyId ?? string.Empty,
                    ToDictionary(entry.attributes),
                    ToDictionary(entry.values)));
            }

            return new GameConfigRootModel(
                dto.schemaVersion,
                dto.generatedAtUtc ?? string.Empty,
                companies,
                new NamedConfigSectionModel("balance", ToDictionary(dto.balance?.values)),
                new NamedConfigSectionModel("roundCriteria", ToDictionary(dto.roundCriteria?.values)),
                new NamedConfigSectionModel("ball", ToDictionary(dto.ball?.values)),
                new NamedConfigSectionModel("shop", ToDictionary(dto.shop?.values)));
        }

        private static Dictionary<string, float> ToDictionary(NamedFloatJsonDto[] values)
        {
            var result = new Dictionary<string, float>();
            if (values == null)
            {
                return result;
            }

            foreach (NamedFloatJsonDto item in values)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.key))
                {
                    continue;
                }

                result[item.key] = item.value;
            }

            return result;
        }
    }
}
