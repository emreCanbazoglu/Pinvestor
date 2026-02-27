using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.CompanySystem;
using UnityEngine;

namespace Pinvestor.GameConfigSystem
{
    public sealed class CompanyAttributeBaseValueOverrideResolver : IAttributeBaseValueOverrideResolver
    {
        private readonly CompanyIdScriptableObject _companyId;
        private readonly CompanyAttributeConfigResolver _companyConfigResolver;

        public CompanyAttributeBaseValueOverrideResolver(
            CompanyIdScriptableObject companyId,
            CompanyAttributeConfigResolver companyConfigResolver)
        {
            _companyId = companyId;
            _companyConfigResolver = companyConfigResolver;
        }

        public bool TryResolveBaseValue(
            AttributeSystemComponent attributeSystemComponent,
            AttributeDefinition attributeDefinition,
            object modifierObject,
            out float value)
        {
            value = 0f;

            if (attributeDefinition == null || attributeDefinition.Attribute == null)
            {
                return false;
            }

            if (_companyConfigResolver == null || _companyId == null)
            {
                return false;
            }

            if (!_companyConfigResolver.TryResolveCompanyConfig(_companyId, out CompanyConfigModel companyConfig))
            {
                Debug.LogWarning(
                    $"CompanyAttributeBaseValueOverrideResolver: Could not resolve company config for '{_companyId.CompanyId}'.");
                return false;
            }

            string attributeKey = attributeDefinition.Attribute.UniqueId;
            if (string.IsNullOrWhiteSpace(attributeKey))
            {
                Debug.LogWarning(
                    $"CompanyAttributeBaseValueOverrideResolver: Attribute '{attributeDefinition.Attribute.name}' has no UniqueId.");
                return false;
            }

            if (companyConfig.Attributes.TryGetValue(attributeKey, out value))
            {
                return true;
            }

            // HP is runtime-backed by MaxHP; if HP is requested and only MaxHP is authored, use MaxHP.
            if (attributeDefinition.Attribute.name == "Attribute.HP")
            {
                if (companyConfig.TryGetMaxHP(out value))
                {
                    return true;
                }
            }

            Debug.LogWarning(
                $"CompanyAttributeBaseValueOverrideResolver: Missing config value for company '{_companyId.CompanyId}' " +
                $"attribute '{attributeDefinition.Attribute.name}' (key='{attributeKey}').");
            return false;
        }

    }
}
