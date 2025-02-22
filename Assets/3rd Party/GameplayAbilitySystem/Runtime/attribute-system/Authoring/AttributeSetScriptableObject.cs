using System;
using AttributeSystem.Components;
using UnityEngine;

namespace AttributeSystem.Authoring
{
    [System.Serializable]
    public class AttributeDefinition
    {
        [field: SerializeField] public AttributeScriptableObject Attribute { get; private set; }
        [field: SerializeField] public bool IsPrimaryAttribute { get; private set; }
        [field: SerializeField] public float Multiplier { get; private set; }
        [field: SerializeField] public AttributeBaseValueModifierScriptableObject BaseValueModifier { get; private set; }
        //[field: SerializeField] public bool UseLevelProviderForAttributeSet { get; private set; } = false;
        //[field: SerializeField] public AttributeLevelProviderScriptableObject LevelProvider { get; private set; }
        [field: SerializeField] public bool UseModifierObjectProviderForAttributeSet { get; private set; } = false;

        [field: SerializeField] public AttributeModifierObjectProviderScriptableObject ModifierObjectProvider { get; private set; }
        
    }
    
    [CreateAssetMenu(menuName = "Gameplay Ability System/Attribute Set", fileName = "AttributeSet", order = 0)]
    public class AttributeSetScriptableObject : ScriptableObject,
        IAttributeValueProvider
    {
        [field: SerializeField] public AttributeDefinition[] AttributeDefinitions { get; private set; }
            = Array.Empty<AttributeDefinition>();
        
        public bool TryGetAttributeDefinition(
            AttributeScriptableObject attribute, 
            out AttributeDefinition attributeDefinition)
        {
            foreach (var definition in AttributeDefinitions)
            {
                if (definition.Attribute == attribute)
                {
                    attributeDefinition = definition;
                    return true;
                }
            }

            attributeDefinition = null;
            return false;
        }


        public bool TryGetAttributeValue(
            AttributeScriptableObject attribute,
            out AttributeValue value,
            object modifierObject = null)
        {
            if (TryGetAttributeDefinition(attribute, out var definition))
            {
                if (modifierObject == null)
                {
                    if(definition.ModifierObjectProvider != null 
                       && definition.UseModifierObjectProviderForAttributeSet)
                        modifierObject = definition.ModifierObjectProvider.GetObject(null, attribute);
                }
                
                var baseValue = definition.BaseValueModifier.CalculateBaseValue(
                    this, modifierObject) * definition.Multiplier;

                value = new AttributeValue()
                {
                    Attribute = attribute,
                    BaseValue = baseValue,
                    CurrentValue = baseValue,
                    Modifier = new AttributeModifier()
                };
                
                return true;
            }

            value = default;
            return false;
        }
    }
}
