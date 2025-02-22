using AttributeSystem.Components;
using UnityEngine;

namespace AttributeSystem.Authoring
{
    [CreateAssetMenu(menuName =
        "Gameplay Ability System/Attribute System/Base Value Modifiers/Attribut eBackedModifier")]
    public class AttributeBackedModifier : AttributeBaseValueModifierScriptableObject
    {
        [SerializeField] private AnimationCurve ScalingFunction;

        [SerializeField] private AttributeScriptableObject CaptureAttributeWhich;

        [SerializeField] private bool IsInverse;

        public override float CalculateBaseValue(
            IAttributeValueProvider attributeValueProvider,
            object modifierObject = null)
        {
            float value = ScalingFunction.Evaluate(
                GetCapturedAttribute(attributeValueProvider)
                    .GetValueOrDefault().CurrentValue);

            if (!IsInverse)
                return value;

            return 1.0f / value;
        }

        private AttributeValue? GetCapturedAttribute(
            IAttributeValueProvider attributeValueProvider)
        {
            attributeValueProvider.TryGetAttributeValue(
                CaptureAttributeWhich,
                out AttributeValue sourceAttributeValue);

            return sourceAttributeValue;
        }
    }
}