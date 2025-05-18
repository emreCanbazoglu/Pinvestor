using System;
using AbilitySystem.Authoring;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem
{
    [CreateAssetMenu(
        fileName = "AbilityTriggerDefinition",
        menuName = "Pinvestor/Ability System/Ability Trigger Definition")]
    public class AbilityTriggerDefinitionScriptableObject : ScriptableObject
    {
        [field: SerializeField] public AbstractAbilityScriptableObject Ability { get; private set; } = null;
        [field: SerializeField] public AbilityTriggerScriptableObjectBase CastTrigger { get; private set; } = null;
        [field: SerializeField] public AbilityTriggerScriptableObjectBase CancelTrigger { get; private set; } = null;

        [field: SerializeField]
        public AbilityTriggerConditionScriptableObjectBase[] Conditions { get; private set; }
            = Array.Empty<AbilityTriggerConditionScriptableObjectBase>();
        
        public AbilityTriggerDefinitionSpec CreateSpec(
            AbilityController abilityController)
        {
            return new AbilityTriggerDefinitionSpec(
                abilityController, this);
        }
    }

    public class AbilityTriggerDefinitionSpec
    {
        public AbilityController Controller { get; private set; }
        public AbilityTriggerDefinitionScriptableObject ScriptableObject { get; private set; }
        
        public AbilityTriggerSpecBase CastTriggerSpec { get; private set; }
        public AbilityTriggerSpecBase CancelTriggerSpec { get; private set; }
        
        public AbstractAbilitySpec AbilitySpec { get; private set; }
        
        public Action<AbilityTriggerDefinitionSpec> OnCastTrigger { get; set; }
        public Action<AbilityTriggerDefinitionSpec> OnCancelTrigger { get; set; }
        
        public AbilityTriggerDefinitionSpec(
            AbilityController abilityController,
            AbilityTriggerDefinitionScriptableObject scriptableObject)
        {
            Controller = abilityController;
            ScriptableObject = scriptableObject;
        }

        private void CreateTriggerSpecs()
        {
            if (ScriptableObject.CastTrigger != null)
            {
                CastTriggerSpec
                    = ScriptableObject.CastTrigger
                        .CreateSpec(Controller);
            }

            if (ScriptableObject.CancelTrigger != null)
            {
                CancelTriggerSpec
                    = ScriptableObject.CancelTrigger
                        .CreateSpec(Controller);
            }
        }
        
        public AbstractAbilitySpec CreateAbilitySpec()
        {
            if (ScriptableObject.Ability != null)
            {
                AbilitySpec
                    = ScriptableObject.Ability
                        .CreateSpec(Controller.AbilitySystemCharacter, null);
            }

            return AbilitySpec;
        }
        
        public void Activate()
        {
            CreateTriggerSpecs();
            
            if(CastTriggerSpec != null)
                CastTriggerSpec.OnTrigger += OnCastTriggered;
            
            if (CancelTriggerSpec != null)  
                CancelTriggerSpec.OnTrigger += OnCancelTriggered;
            
            CastTriggerSpec?.Activate();
            CancelTriggerSpec?.Activate();
        }
        
        public void Deactivate()
        {
            CastTriggerSpec?.Deactivate();
            CancelTriggerSpec?.Deactivate();
            
            if(CastTriggerSpec != null)
                CastTriggerSpec.OnTrigger -= OnCastTriggered;
            
            if (CancelTriggerSpec != null)
                CancelTriggerSpec.OnTrigger -= OnCancelTriggered;
        }
        
        private void OnCastTriggered()
        {
            OnCastTrigger?.Invoke(this);
        }
        
        private void OnCancelTriggered()
        {
            OnCancelTrigger?.Invoke(this);
        }
    }
}