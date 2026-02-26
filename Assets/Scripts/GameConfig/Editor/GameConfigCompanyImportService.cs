using System;
using System.Collections.Generic;
using AttributeSystem.Authoring;
using Pinvestor.CardSystem;
using UnityEditor;
using UnityEngine;

namespace Pinvestor.GameConfigSystem.Editor
{
    public sealed class GameConfigCompanyImportService
    {
        private const string DefaultAuthoringAssetPath = "Assets/ScriptableObjects/GameConfig/GameConfigAuthoring.asset";

        [MenuItem("Pinvestor/Game Config/Import Companies To Authoring Asset")]
        public static void ImportCompaniesToAuthoringAssetMenu()
        {
            GameConfigAuthoringAsset authoringAsset = GetOrCreateAuthoringAsset();
            var service = new GameConfigCompanyImportService();
            if (service.TryImportCompanies(authoringAsset, out string message))
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public bool TryImportCompanies(
            GameConfigAuthoringAsset authoringAsset,
            out string message)
        {
            if (authoringAsset == null)
            {
                message = "Authoring asset is null.";
                return false;
            }

            string[] guids = AssetDatabase.FindAssets("t:CompanyCardDataScriptableObject");
            var importedCompaniesById = new Dictionary<string, ImportedCompanyRow>(StringComparer.Ordinal);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CompanyCardDataScriptableObject cardData
                    = AssetDatabase.LoadAssetAtPath<CompanyCardDataScriptableObject>(path);
                if (cardData == null || cardData.CompanyId == null)
                {
                    continue;
                }

                ImportedCompanyRow row = BuildRow(cardData);
                if (string.IsNullOrWhiteSpace(row.CompanyId))
                {
                    continue;
                }

                if (importedCompaniesById.TryGetValue(row.CompanyId, out ImportedCompanyRow existing))
                {
                    MergeRows(existing, row);
                    continue;
                }

                importedCompaniesById[row.CompanyId] = row;
            }

            var importedCompanies = new List<ImportedCompanyRow>(importedCompaniesById.Values);
            importedCompanies.Sort((a, b) => string.Compare(a.CompanyId, b.CompanyId, StringComparison.Ordinal));

            ApplyToAuthoringAsset(authoringAsset, importedCompanies);

            EditorUtility.SetDirty(authoringAsset);
            AssetDatabase.SaveAssets();

            message = $"Imported {importedCompanies.Count} companies from existing CompanyCardData ScriptableObjects.";
            return true;
        }

        private static GameConfigAuthoringAsset GetOrCreateAuthoringAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameConfigAuthoringAsset");
            if (guids != null && guids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameConfigAuthoringAsset existing = AssetDatabase.LoadAssetAtPath<GameConfigAuthoringAsset>(existingPath);
                if (existing != null)
                {
                    return existing;
                }
            }

            string directory = System.IO.Path.GetDirectoryName(DefaultAuthoringAssetPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                EnsureFolderPath(directory);
            }

            GameConfigAuthoringAsset created = ScriptableObject.CreateInstance<GameConfigAuthoringAsset>();
            AssetDatabase.CreateAsset(created, DefaultAuthoringAssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static void EnsureFolderPath(string fullAssetFolderPath)
        {
            string[] parts = fullAssetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static ImportedCompanyRow BuildRow(CompanyCardDataScriptableObject cardData)
        {
            var row = new ImportedCompanyRow
            {
                CompanyId = cardData.CompanyId.CompanyId
            };

            AttributeSetScriptableObject attributeSet = cardData.AttributeSet;
            if (attributeSet == null || attributeSet.AttributeDefinitions == null)
            {
                return row;
            }

            foreach (AttributeDefinition definition in attributeSet.AttributeDefinitions)
            {
                if (definition == null || definition.Attribute == null)
                {
                    continue;
                }

                if (ShouldSkipAttributeForCompanyEditor(definition.Attribute))
                {
                    continue;
                }

                string key = BuildStableAttributeKey(definition.Attribute);

                float value = 0f;
                try
                {
                    if (definition.BaseValueModifier != null)
                    {
                        value = definition.BaseValueModifier.CalculateBaseValue(attributeSet, null)
                                * definition.Multiplier;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"GameConfig company import: failed to evaluate attribute '{key}' for company '{row.CompanyId}'. " +
                        $"Defaulting to 0. Error: {e.Message}");
                    value = 0f;
                }

                row.Attributes[key] = value;
            }

            return row;
        }

        private static string BuildStableAttributeKey(AttributeScriptableObject attribute)
        {
            if (attribute == null)
            {
                return "unknown_attribute";
            }

            if (!string.IsNullOrWhiteSpace(attribute.UniqueId))
            {
                return attribute.UniqueId;
            }

            if (!string.IsNullOrWhiteSpace(attribute.name))
            {
                return attribute.name;
            }

            if (!string.IsNullOrWhiteSpace(attribute.Name))
            {
                return attribute.Name;
            }

            return "unknown_attribute";
        }

        private static bool ShouldSkipAttributeForCompanyEditor(AttributeScriptableObject attribute)
        {
            if (attribute == null)
            {
                return true;
            }

            // HP is runtime-backed by MaxHP in this project; author only MaxHP.
            if (attribute.name == "Attribute.HP")
            {
                return true;
            }

            return false;
        }

        private static void MergeRows(
            ImportedCompanyRow target,
            ImportedCompanyRow source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, float> kvp in source.Attributes)
            {
                target.Attributes[kvp.Key] = kvp.Value;
            }
        }

        private static void ApplyToAuthoringAsset(
            GameConfigAuthoringAsset authoringAsset,
            List<ImportedCompanyRow> importedCompanies)
        {
            SerializedObject so = new SerializedObject(authoringAsset);
            SerializedProperty companiesProp = so.FindProperty("_companies");
            if (companiesProp == null)
            {
                return;
            }

            companiesProp.arraySize = importedCompanies.Count;

            for (int i = 0; i < importedCompanies.Count; i++)
            {
                SerializedProperty companyProp = companiesProp.GetArrayElementAtIndex(i);
                ImportedCompanyRow imported = importedCompanies[i];

                companyProp.FindPropertyRelative("companyId").stringValue = imported.CompanyId ?? string.Empty;

                SerializedProperty valuesProp = companyProp.FindPropertyRelative("values");
                if (valuesProp != null)
                {
                    valuesProp.arraySize = 0;
                }

                SerializedProperty attrsProp = companyProp.FindPropertyRelative("attributes");
                if (attrsProp == null)
                {
                    continue;
                }

                attrsProp.arraySize = imported.Attributes.Count;
                int a = 0;
                foreach (KeyValuePair<string, float> kvp in imported.Attributes)
                {
                    SerializedProperty attrProp = attrsProp.GetArrayElementAtIndex(a);
                    attrProp.FindPropertyRelative("key").stringValue = kvp.Key ?? string.Empty;
                    attrProp.FindPropertyRelative("value").floatValue = kvp.Value;
                    a++;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class ImportedCompanyRow
        {
            public string CompanyId;
            public Dictionary<string, float> Attributes = new Dictionary<string, float>(StringComparer.Ordinal);
        }
    }
}
