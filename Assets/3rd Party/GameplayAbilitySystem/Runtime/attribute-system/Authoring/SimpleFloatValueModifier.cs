using UnityEngine;

namespace AttributeSystem.Authoring
{
    [CreateAssetMenu(menuName = "Gameplay Ability System/Attribute System/Base Value Modifiers/Simple Float Value Modifier")]
    public class SimpleFloatValueModifier : AttributeBaseValueModifierScriptableObject
    {
        [SerializeField]
        private AnimationCurve ScalingFunction;
        
        public override float CalculateBaseValue(
            IAttributeValueProvider attributeValueProvider, 
            object modifierObject = null)
        {
            int level = 1;
            
            if(modifierObject is int i)
                level = i;
            
            return ScalingFunction.Evaluate(level);
        }
        
        [Header("Debugging")]
        [SerializeField] private float _debugKey = 0f;

        [ContextMenu("Evaluate Value")]
        private void Evaluate()
        {
            float value = ScalingFunction.Evaluate(_debugKey);
            
            Debug.Log("Value: " + value);
        }
    }
    
}