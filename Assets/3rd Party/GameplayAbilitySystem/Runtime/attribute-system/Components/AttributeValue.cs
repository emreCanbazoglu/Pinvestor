using System;
using AttributeSystem.Authoring;
using Unity.Collections;

namespace AttributeSystem.Components
{
    [Serializable]
    public struct AttributeValue
    {
        public AttributeScriptableObject Attribute;

        private float _baseValue;
        public float BaseValue
        {
            get => _baseValue;
            set => _baseValue = (float) Math.Round(value, 2);
        }

        private float _currentValue;

        public float CurrentValue
        {
            get => _currentValue;
            set => _currentValue = (float) Math.Round(value, 2);
        }
        
        public AttributeModifier Modifier;
    }

    [Serializable]
    public struct AttributeModifier
    {
        public float Add;
        public float Multiply;
        public float Override;
        public AttributeModifier Combine(AttributeModifier other)
        {
            other.Add += Add;
            other.Multiply += Multiply;
            other.Override = Override;
            return other;
        }
    }
}
