using System;
using System.Collections.Generic;
using AttributeSystem.Components;
using UnityEngine;
using UnityEngine.Serialization;


namespace AttributeSystem.Authoring
{ 
    /// <summary>
    /// This asset defines a single player attribute
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay Ability System/Attribute")]
    public class AttributeScriptableObject : UniqueScriptableObject
    {
        [ScriptableObjectId] public string AttributeId;
        
        [field: SerializeField] public bool HigherIsBetter { get; private set; } = true;
        
        /// <summary>
        /// Friendly name of this attribute.  Used for display purposes only.
        /// </summary>
        public string Name;

        public virtual AttributeValue CalculateInitialValue(AttributeValue attributeValue, List<AttributeValue> otherAttributeValues)
        {
            return attributeValue;
        }

        public virtual AttributeValue CalculateCurrentAttributeValue(
            AttributeSystemComponent attributeSystemComponent,
            AttributeValue attributeValue,
            List<AttributeValue> otherAttributeValues)
        {
            attributeValue.CurrentValue = (attributeValue.BaseValue + attributeValue.Modifier.Add) * (attributeValue.Modifier.Multiply + 1);

            if (attributeValue.Modifier.Override != 0)
            {
                attributeValue.CurrentValue = attributeValue.Modifier.Override;
            }
            return attributeValue;
        }
        
        public override bool Equals(object obj)
        {
            // Check for null and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            AttributeScriptableObject other = (AttributeScriptableObject)obj;
            return AttributeId == other.AttributeId;
        }

        public override int GetHashCode()
        {
            return (AttributeId != null ? AttributeId.GetHashCode() : 0);
        }

        public static bool operator ==(AttributeScriptableObject a, AttributeScriptableObject b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(AttributeScriptableObject a, AttributeScriptableObject b)
        {
            return !(a == b);
        }
    }
}
