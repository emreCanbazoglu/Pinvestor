using System;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem
{
    public abstract class AbilityTriggerScriptableObjectBase : ScriptableObject
    {
        public abstract AbilityTriggerSpecBase CreateSpec(
            AbilityController abilityController);
    }
    
    public abstract class AbilityTriggerSpecBase
    {
        public AbilityController Controller { get; private set; }
        public AbilityTriggerScriptableObjectBase ScriptableObject { get; private set; }
        
        public Action OnTrigger { get; set; }
        
        protected AbilityTriggerSpecBase(
            AbilityController abilityController,
            AbilityTriggerScriptableObjectBase scriptableObject)
        {
            Controller = abilityController;
            ScriptableObject = scriptableObject;
        }
        
        public abstract void Activate();
        public abstract void Deactivate();
    }
}