using System;
using AbilitySystem;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using UnityEngine;

namespace Pinvestor.RevenueGeneratorSystem.Core
{
    public class RevenueGenerator : MonoBehaviour
    {
        [SerializeField] private AbilitySystemCharacter _abilitySystemCharacter = null;
        [SerializeField] private AttributeScriptableObject _balanceAttribute = null;
        
        public Action<AbilitySystemCharacter, float, float> OnRevenueGenerated { get; set; }
        
        private void OnEnable()
        {
            _abilitySystemCharacter.OnGameplayModifierAppliedToOther += OnGameplayModifierAppliedToSelf;
        }

        private void OnDisable()
        {
            _abilitySystemCharacter.OnGameplayModifierAppliedToOther -= OnGameplayModifierAppliedToSelf;
        }
        
        private void OnGameplayModifierAppliedToSelf(
            GameplayEffectSpec spec,
            AttributeValue oldValue, 
            AttributeValue newValue)
        {
            if(oldValue.Attribute != _balanceAttribute)
                return;

            float revenueAmount = newValue.CurrentValue - oldValue.CurrentValue;
            
            if(revenueAmount <= 0)
                return;
            
            OnRevenueGenerated?.Invoke(
                spec.Source,
                revenueAmount,
                newValue.CurrentValue);
        }
    }
}
