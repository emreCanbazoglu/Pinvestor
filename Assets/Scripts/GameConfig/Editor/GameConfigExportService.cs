using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pinvestor.GameConfigSystem.Editor
{
    public sealed class GameConfigExportService
    {
        public const string ExportAssetPath = "Assets/Resources/GameConfig/game-config.json";

        private readonly GameConfigValidationService _validationService
            = new GameConfigValidationService();

        public bool TryExport(
            GameConfigAuthoringAsset asset,
            out string message)
        {
            GameConfigValidationResult validation = _validationService.Validate(asset);
            if (!validation.IsValid)
            {
                message = validation.ToMultilineString();
                return false;
            }

            string json = JsonUtility.ToJson(ToDto(asset), true);
            string directory = Path.GetDirectoryName(ExportAssetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(ExportAssetPath, json);
            AssetDatabase.Refresh();

            message = $"GameConfig exported to {ExportAssetPath}";
            return true;
        }

        private static GameConfigRootJsonDto ToDto(GameConfigAuthoringAsset asset)
        {
            return new GameConfigRootJsonDto
            {
                schemaVersion = asset.SchemaVersion,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                company = new CompanyConfigSectionJsonDto
                {
                    entries = ToCompanyDtos(asset.Companies)
                },
                balance = new NamedValueSectionJsonDto { values = ToNamedDtos(asset.Balance) },
                roundCriteria = new NamedValueSectionJsonDto { values = ToNamedDtos(asset.RoundCriteria) },
                ball = new NamedValueSectionJsonDto { values = ToNamedDtos(asset.Ball) },
                shop = new NamedValueSectionJsonDto { values = ToNamedDtos(asset.Shop) }
            };
        }

        private static CompanyConfigEntryJsonDto[] ToCompanyDtos(CompanyAuthoringEntry[] companies)
        {
            if (companies == null || companies.Length == 0)
            {
                return Array.Empty<CompanyConfigEntryJsonDto>();
            }

            var result = new CompanyConfigEntryJsonDto[companies.Length];
            for (int i = 0; i < companies.Length; i++)
            {
                CompanyAuthoringEntry source = companies[i] ?? new CompanyAuthoringEntry();
                result[i] = new CompanyConfigEntryJsonDto
                {
                    companyId = source.companyId ?? string.Empty,
                    attributes = ToNamedDtos(source.attributes),
                    values = ToNamedDtos(source.values)
                };
            }

            return result;
        }

        private static NamedFloatJsonDto[] ToNamedDtos(NamedFloatAuthoringEntry[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Array.Empty<NamedFloatJsonDto>();
            }

            var result = new NamedFloatJsonDto[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                NamedFloatAuthoringEntry source = values[i] ?? new NamedFloatAuthoringEntry();
                result[i] = new NamedFloatJsonDto
                {
                    key = source.key ?? string.Empty,
                    value = source.value
                };
            }

            return result;
        }
    }
}
