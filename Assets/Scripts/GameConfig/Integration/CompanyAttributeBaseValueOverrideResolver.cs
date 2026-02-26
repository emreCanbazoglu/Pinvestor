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
                return false;
            }

            string attributeKey = attributeDefinition.Attribute.UniqueId;
            if (string.IsNullOrWhiteSpace(attributeKey))
            {
                return false;
            }

            if (companyConfig.Attributes.TryGetValue(attributeKey, out value))
            {
                return true;
            }

            // HP is runtime-backed by MaxHP; if HP is requested and only MaxHP is authored, use MaxHP.
            if (attributeDefinition.Attribute.name == "Attribute.HP")
            {
                string maxHpKey = FindMaxHpAttributeKey(attributeSystemComponent);
                if (!string.IsNullOrWhiteSpace(maxHpKey)
                    && companyConfig.Attributes.TryGetValue(maxHpKey, out value))
                {
                    return true;
                }
            }

            return false;
        }

        private static string FindMaxHpAttributeKey(AttributeSystemComponent attributeSystemComponent)
        {
            if (attributeSystemComponent == null)
            {
                return null;
            }

            AttributeSetScriptableObject attributeSet = GetAttributeSet(attributeSystemComponent);
            if (attributeSet == null || attributeSet.AttributeDefinitions == null)
            {
                return null;
            }

            for (int i = 0; i < attributeSet.AttributeDefinitions.Length; i++)
            {
                AttributeDefinition def = attributeSet.AttributeDefinitions[i];
                if (def == null || def.Attribute == null)
                {
                    continue;
                }

                if (def.Attribute.name != "Attribute.MaxHP")
                {
                    continue;
                }

                return def.Attribute.UniqueId;
            }

            return null;
        }

        private static AttributeSetScriptableObject GetAttributeSet(AttributeSystemComponent attributeSystemComponent)
        {
            return attributeSystemComponent != null
                ? attributeSystemComponent.AttributeSet
                : null;
        }
    }
}
