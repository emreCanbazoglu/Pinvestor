using UnityEngine;

namespace AttributeSystem.Authoring
{
    public abstract class AttributeBaseValueModifierScriptableObject : ScriptableObject
    {
        public abstract float CalculateBaseValue(
            IAttributeValueProvider attributeValueProvider, 
            object modifierObject = null);
    }
}