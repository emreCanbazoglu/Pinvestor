using AttributeSystem.Components;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    public abstract class GameplayEffectModifierAppliedHandlerScriptableObject : ScriptableObject
    {
        public abstract GameplayEffectModifierAppliedHandlerSpec CreateSpec();
    }

    public abstract class GameplayEffectModifierAppliedHandlerSpec
    {
        public GameplayEffectModifierAppliedHandlerScriptableObject HandlerScriptableObject { get; private set; }
        
        public GameplayEffectModifierAppliedHandlerSpec(
            GameplayEffectModifierAppliedHandlerScriptableObject handlerScriptableObject)
        {
            HandlerScriptableObject = handlerScriptableObject;            
        }
        
        public abstract void HandleAppliedModifier(
            GameplayEffectSpec spec,
            AttributeValue preAttributeValue,
            AttributeValue curAttributeValue);
    }
}