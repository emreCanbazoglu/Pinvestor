using System.Collections.Generic;
using UnityEngine;

namespace AttributeSystem.Authoring
{
    [CreateAssetMenu(fileName = "ExponentialFloatValueModifier",
        menuName =
            "Gameplay Ability System/Attribute System/Base Value Modifiers/Exponential Float Value Modifier")]
    public class ExponentialFloatValueModifier : AttributeBaseValueModifierScriptableObject
    {
        [SerializeField] private float _multiplier = 1f;

        [SerializeField] private float _LevelMultiplier = 1.2f;
        
        [SerializeField] private int _decimalPlaces = 1;

        public override float CalculateBaseValue(
            IAttributeValueProvider attributeValueProvider,
            object modifierObject = null)
        {
            int baseLevel = 1;

             if (modifierObject is int level)
            {
                baseLevel = level;
            }

            float baseValue = RoundToDecimalPlaces(
                _multiplier
                * Mathf.Pow(
                    _LevelMultiplier, baseLevel - 1)
                , _decimalPlaces);

            return baseValue;
        }

        private float RoundToDecimalPlaces(float value, int decimalPlaces)
        {
            float factor = Mathf.Pow(10, decimalPlaces);
            return Mathf.Round(value * factor) / factor;
        }
    }
}