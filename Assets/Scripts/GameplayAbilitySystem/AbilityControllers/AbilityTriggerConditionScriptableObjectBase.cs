using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem
{
    public abstract class AbilityTriggerConditionScriptableObjectBase : ScriptableObject
    {
        
    }
    
    public abstract class AbilityTriggerConditionSpecBase
    {
        public AbilityController Controller { get; private set; }
        public AbilityTriggerConditionScriptableObjectBase ScriptableObject { get; private set; }
        
        protected AbilityTriggerConditionSpecBase(
            AbilityController abilityController,
            AbilityTriggerConditionScriptableObjectBase scriptableObject)
        {
            Controller = abilityController;
            ScriptableObject = scriptableObject;
        }
        
        public abstract bool IsConditionMet();
    }
}