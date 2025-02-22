using System.Collections.Generic;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay Ability System/Attribute Event Handler/Clamp Attribute")]
public class ClampAttributeEventHandler : AbstractAttributeEventHandler
{

    [SerializeField]
    private AttributeScriptableObject PrimaryAttribute;
    [SerializeField]
    private AttributeScriptableObject MaxAttribute;

    [SerializeField] private float _minValue = Mathf.NegativeInfinity;
    public override void PreAttributeChange(AttributeSystemComponent attributeSystem, List<AttributeValue> prevAttributeValues, ref List<AttributeValue> currentAttributeValues)
    {
        var attributeCacheDict = attributeSystem.AttributeIndexCache;
        ClampAttributeToMax(PrimaryAttribute, MaxAttribute, currentAttributeValues, attributeCacheDict);
    }

    private void ClampAttributeToMax(
        AttributeScriptableObject attribute1,
        AttributeScriptableObject attribute2, 
        List<AttributeValue> attributeValues, 
        Dictionary<AttributeScriptableObject, int> attributeCacheDict)
    {
        if (attributeCacheDict.TryGetValue(attribute1, out var primaryAttributeIndex))
        {
            var primaryAttribute = attributeValues[primaryAttributeIndex];

            float maxCurrentValue = primaryAttribute.CurrentValue;
            float maxBaseValue = primaryAttribute.BaseValue;

            if (attributeCacheDict.TryGetValue(attribute2, out var maxAttributeIndex))
            {
                var maxAttribute = attributeValues[maxAttributeIndex];
                maxCurrentValue = maxAttribute.CurrentValue;
                maxBaseValue = maxAttribute.BaseValue;
            }
            
            
            // Clamp current and base values
            primaryAttribute.CurrentValue =
                Mathf.Clamp(primaryAttribute.CurrentValue, _minValue, maxCurrentValue);
            primaryAttribute.BaseValue =
                Mathf.Clamp(primaryAttribute.BaseValue, _minValue, maxCurrentValue);
            
            attributeValues[primaryAttributeIndex] = primaryAttribute;
        }
    }
}
