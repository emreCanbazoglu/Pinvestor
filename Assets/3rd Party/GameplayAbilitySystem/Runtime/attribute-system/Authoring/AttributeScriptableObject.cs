using System;
using System.Collections.Generic;
using AbilitySystem.Authoring;
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
    }
}
