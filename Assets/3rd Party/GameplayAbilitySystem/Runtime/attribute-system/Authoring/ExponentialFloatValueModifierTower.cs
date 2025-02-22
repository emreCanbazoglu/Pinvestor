using System.Collections.Generic;
using UnityEngine;

namespace AttributeSystem.Authoring
{
    [CreateAssetMenu(fileName = "ExponentialFloatValueModifierTower",
        menuName =
            "Gameplay Ability System/Attribute System/Base Value Modifiers/Exponential Float Value Modifier Tower")]
    public class ExponentialFloatValueModifierTower : AttributeBaseValueModifierScriptableObject
    {
        [SerializeField] private float _multiplier = 1f;

        [SerializeField] private float _baseLevelMultiplier = 1.2f;

        [SerializeField] private float _inGameLevelMultiplier = 1.2f;

        [SerializeField] private int _decimalPlaces = 1;

        public override float CalculateBaseValue(
            IAttributeValueProvider attributeValueProvider,
            object modifierObject = null)
        {
            int baseLevel = 1;
            int inGameLevel = 1;

            if (modifierObject is List<int> towerLevels)
            {
                baseLevel = towerLevels[0];
                inGameLevel = towerLevels[1];
            }
            else if (modifierObject is int level)
            {
                baseLevel = level;
            }

            float baseValue = RoundToDecimalPlaces(
                _multiplier
                * Mathf.Pow(
                    _baseLevelMultiplier, baseLevel - 1)
                , _decimalPlaces);

            float finalValue = RoundToDecimalPlaces(
                baseValue
                * Mathf.Pow(
                    _inGameLevelMultiplier, inGameLevel - 1)
                , _decimalPlaces);

            return finalValue;
        }

        private float RoundToDecimalPlaces(float value, int decimalPlaces)
        {
            float factor = Mathf.Pow(10, decimalPlaces);
            return Mathf.Round(value * factor) / factor;
        }
    }
}