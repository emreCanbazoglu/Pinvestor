using System;
using AbilitySystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem
{
    public class AbilityController : MonoBehaviour
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;
        
        private AbilityTriggerDefinitionSpec[] _abilityTriggerDefinitionSpecs
            = Array.Empty<AbilityTriggerDefinitionSpec>();
        
        private void OnDestroy()
        {
            RemoveAbilities();
            
            DeactivateTriggers();
        }

        public void Initialize(
            AbilityTriggerDefinitionScriptableObject[] abilityTriggerDefinitions)
        {
            InitializeDefinitionSpecs(
                abilityTriggerDefinitions);
            
            InitializeAsync().Forget();
        }

        private void InitializeDefinitionSpecs(
            AbilityTriggerDefinitionScriptableObject[] abilityTriggerDefinitions)
        {
            _abilityTriggerDefinitionSpecs 
                = new AbilityTriggerDefinitionSpec[abilityTriggerDefinitions.Length];

            for (int i = 0; i < abilityTriggerDefinitions.Length; i++)
            {
                var definition = abilityTriggerDefinitions[i];
                _abilityTriggerDefinitionSpecs[i] 
                    = definition.CreateSpec(this);

                _abilityTriggerDefinitionSpecs[i].OnCastTrigger += OnCastTrigger;
                _abilityTriggerDefinitionSpecs[i].OnCancelTrigger += OnCancelTrigger;
            }
        }

        private async UniTask InitializeAsync()
        {
            await AbilitySystemCharacter.WaitUntilInitializeAsync();

            GrantAbilities();
            
            ActivateTriggers();
        }

        private void GrantAbilities()
        {
            foreach (var definitionSpec in _abilityTriggerDefinitionSpecs)
            {
                AbilitySystemCharacter.GrantAbility(
                    definitionSpec.CreateAbilitySpec());
            }
        }

        private void RemoveAbilities()
        {
            foreach (var definitionSpec in _abilityTriggerDefinitionSpecs)
            {
                AbilitySystemCharacter.RemoveAbility(
                    definitionSpec.AbilitySpec);
                
                definitionSpec.OnCastTrigger -= OnCastTrigger;
                definitionSpec.OnCancelTrigger -= OnCancelTrigger;
            }
        }

        private void ActivateTriggers()
        {
            foreach (var definitionSpec in _abilityTriggerDefinitionSpecs)
                definitionSpec.Activate();
        }
        
        private void DeactivateTriggers()
        {
            foreach (var definitionSpec in _abilityTriggerDefinitionSpecs)
                definitionSpec.Deactivate();
        }
        
        private void OnCastTrigger(
            AbilityTriggerDefinitionSpec triggerSpec)
        {
            AbilitySystemCharacter.TryActivateAbility(
                triggerSpec.AbilitySpec);
        }

        private void OnCancelTrigger(
            AbilityTriggerDefinitionSpec triggerSpec)
        {
            triggerSpec.AbilitySpec.CancelAbility();
        }
    }
}
