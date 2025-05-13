using System;
using AbilitySystem;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using GameplayTag.Authoring;
using UnityEngine;

namespace Pinvestor.DamagableSystem
{
    public class Damagable : MonoBehaviour
    {
        [SerializeField] private AbilitySystemCharacter _abilitySystemCharacter = null;

        [SerializeField] private AttributeScriptableObject _healthAttribute = null;
        [SerializeField] private GameplayTagScriptableObject _critGameplayTag = null;

        public Action<AbilitySystemCharacter, DamageInfo> OnTookDamage { get; set; }
        public Action<AbilitySystemCharacter, DamageInfo> OnDied { get; set; }
        
        private void OnEnable()
        {
            _abilitySystemCharacter.OnGameplayModifierAppliedToSelf += OnGameplayModifierAppliedToSelf;
        }

        private void OnDisable()
        {
            _abilitySystemCharacter.OnGameplayModifierAppliedToSelf -= OnGameplayModifierAppliedToSelf;
        }

        private void OnGameplayModifierAppliedToSelf(
            GameplayEffectSpec spec,
            AttributeValue oldValue, 
            AttributeValue newValue)
        {
            if(oldValue.Attribute != _healthAttribute)
                return;

            float damageAmount = oldValue.CurrentValue - newValue.CurrentValue;
            
            if(damageAmount <= 0)
                return;

            HandleTookDamage(
                spec.Source,
                damageAmount,
                newValue.CurrentValue,
                spec.RuntimeGameplayTags.Contains(_critGameplayTag));
        }


        //TODO: Decouple UI code
        private void HandleTookDamage(
            AbilitySystemCharacter other,
            float damageAmount,
            float currentHealth,
            bool isCrit)
        {
            damageAmount = Mathf.Round(damageAmount);
            other.TryGetComponent(out IComponentProvider<Damager> damagerProvider);
            
            Damager damager = damagerProvider?.GetComponent();
            
            damager?.HandleDealtDamage(
                _abilitySystemCharacter,
                new DamageInfo(
                    damageAmount,
                    isCrit));

            if (currentHealth > 0)
            {
                OnTookDamage?.Invoke(
                    other,
                    new DamageInfo(
                        damageAmount,
                        isCrit,
                        currentHealth));
            }
            
            if (currentHealth <= 0)
            {
                OnDied?.Invoke(
                    other,
                    new DamageInfo(
                        damageAmount,
                        isCrit));
                
                damager?.HandleKilled(
                    _abilitySystemCharacter,
                    new DamageInfo(
                        damageAmount,
                        isCrit));
            }


            /*Widget_FloatingText widgetFloatingText 
                = FloatingTextPool.Instance.Pool.Get();
            
            if(widgetFloatingText == null)
                return;

            FloatingTextSkinScriptableObject skin
                = _defaultDamageSkin;

            if (isCrit)
                skin = _critDamageSkin;

            if (_pivotTransform == null)
                return;

            widgetFloatingText.Init(
                _pivotTransform, 
                damageAmount.ToString(),
                skin);
            
            widgetFloatingText.TryActivate();*/
        }
    }
}
