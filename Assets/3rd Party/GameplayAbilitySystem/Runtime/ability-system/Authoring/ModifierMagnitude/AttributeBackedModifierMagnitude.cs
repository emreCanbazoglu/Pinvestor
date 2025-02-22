using AttributeSystem.Authoring;
using AttributeSystem.Components;
using UnityEngine;

namespace AbilitySystem.ModifierMagnitude
{
    [CreateAssetMenu(menuName = "Gameplay Ability System/Gameplay Effect/Modifier Magnitude/Attribute Backed")]
    public class AttributeBackedModifierMagnitude : ModifierMagnitudeScriptableObject
    {
        [SerializeField]
        private AnimationCurve ScalingFunction;

        [SerializeField]
        private AttributeScriptableObject CaptureAttributeWhich;

        [SerializeField]
        private ECaptureAttributeFrom CaptureAttributeFrom;

        [SerializeField]
        private ECaptureAttributeWhen CaptureAttributeWhen;
        
        [SerializeField] 
        private bool IsInverse;
        
        [SerializeField] private AttributeSetScriptableObject CombineWithAttributeSet = null;

        public override void Initialise(GameplayEffectSpec spec)
        {
            spec.Source.AttributeSystem.TryGetAttributeValue(CaptureAttributeWhich, out AttributeValue sourceAttributeValue);
            spec.SourceCapturedAttribute = sourceAttributeValue;
        }

        public override float? CalculateMagnitude(GameplayEffectSpec spec)
        {
            float value = ScalingFunction.Evaluate(GetCapturedAttribute(spec).GetValueOrDefault().CurrentValue);
            
            if(CombineWithAttributeSet != null)
            {
                CombineWithAttributeSet.TryGetAttributeValue(
                    CaptureAttributeWhich, out AttributeValue combineWithAttributeValue);
                value *= combineWithAttributeValue.CurrentValue;
            }
            
            if(!IsInverse)
                return value;

            return 1.0f / value;
        }

        private AttributeValue? GetCapturedAttribute(GameplayEffectSpec spec)
        {
            if (CaptureAttributeWhen == ECaptureAttributeWhen.OnApplication && CaptureAttributeFrom == ECaptureAttributeFrom.Source)
            {
                return spec.SourceCapturedAttribute;
            }

            switch (CaptureAttributeFrom)
            {
                case ECaptureAttributeFrom.Source:
                    spec.Source.AttributeSystem.TryGetAttributeValue(CaptureAttributeWhich, out AttributeValue sourceAttributeValue);
                    return sourceAttributeValue;
                case ECaptureAttributeFrom.Target:
                    spec.Target.AttributeSystem.TryGetAttributeValue(CaptureAttributeWhich, out AttributeValue targetAttributeValue);
                    return targetAttributeValue;
                default:
                    return null;
            }
        }
    }
}
