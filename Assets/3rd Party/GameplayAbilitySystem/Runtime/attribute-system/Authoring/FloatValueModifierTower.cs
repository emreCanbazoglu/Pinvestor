using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AttributeSystem.Authoring
{
    [CreateAssetMenu(menuName = "Gameplay Ability System/Attribute System/Base Value Modifiers/Float Value Modifier Tower")]
    public class FloatValueModifierTower : AttributeBaseValueModifierScriptableObject
    {
        [SerializeField]
        private AnimationCurve _scalingFunctionForBaseLevel;
        
        [SerializeField]
        private AnimationCurve _scalingFunctionForInGameLevel;
        
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
            if (modifierObject is int i)
            {
                baseLevel = i;
            }
            return _scalingFunctionForBaseLevel.Evaluate(baseLevel) * _scalingFunctionForInGameLevel.Evaluate(inGameLevel);
        }
    }  
}