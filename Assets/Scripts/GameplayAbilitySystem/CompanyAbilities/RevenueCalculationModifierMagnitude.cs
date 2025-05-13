using AbilitySystem;
using AbilitySystem.ModifierMagnitude;
using AttributeSystem.Authoring;
using GameplayTag.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinvestor.GameplayAbilitySystem.Modifiers
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Magnitude Modifiers/Revenue Calculation",
        fileName = "MagnitudeModifier.RevenueCalculation.asset")]
    public class RevenueCalculationModifierMagnitude : ModifierMagnitudeScriptableObject
    {
        [SerializeField] private AttributeScriptableObject _rphAttribute = null;
        [SerializeField] private AttributeScriptableObject _critChanceAttribute = null;
        [SerializeField] private AttributeScriptableObject _critMultiplierAttribute = null;
        
        [SerializeField] private GameplayTagScriptableObject _critGT = null;

        public override float? CalculateMagnitude(
            GameplayEffectSpec spec)
        {
            spec.Source.AttributeSystem
                .TryGetAttributeValue(
                    _rphAttribute,
                    out var rptValue);

            float critMultiplier = CalculateCritMultiplier(spec);
            
            return rptValue.CurrentValue * critMultiplier;
        }
        
        private float CalculateCritMultiplier(
            GameplayEffectSpec spec)
        {
            float critMultiplier = 1f;

            if (_critChanceAttribute == null)
                return critMultiplier;
            
            if (spec.Source.AttributeSystem
                .TryGetAttributeValue(
                    _critChanceAttribute,
                    out var critChanceAttValue))
            {
                float randomValue = Random.Range(0.0f, 1.0f);

                if (randomValue < critChanceAttValue.CurrentValue)
                {
                    spec.RuntimeGameplayTags.Add(_critGT);
                    
                    critMultiplier = GetCritMultiplier(spec);
                }
            }

            return critMultiplier;
        }
        
        private float GetCritMultiplier(GameplayEffectSpec spec)
        {
            float critMultiplier = 2.0f;

            if (_critMultiplierAttribute == null)
                return critMultiplier;

            if (spec.Source.AttributeSystem
                .TryGetAttributeValue(
                    _critMultiplierAttribute,
                    out var critMultiplierAttValue))
            {
                critMultiplier = critMultiplierAttValue.CurrentValue;
            }
                
            return critMultiplier;
        }
    }
}