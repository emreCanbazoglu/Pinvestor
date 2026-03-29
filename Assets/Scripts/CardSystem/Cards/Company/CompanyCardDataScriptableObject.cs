using System;
using AttributeSystem.Authoring;
using Pinvestor.CompanySystem;
using Pinvestor.GameplayAbilitySystem;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Deck System/Card Data/Company Card",
        fileName = "CardData.Company.asset")]
    public class CompanyCardDataScriptableObject : CardDataScriptableObject
    {
        
        [field: SerializeField] public ECompanyCategory CompanyCategory { get; private set; } 
            = ECompanyCategory.None;
        [field: SerializeField] public CompanyIdScriptableObject CompanyId { get; private set; } = null;
        
        [field: SerializeField] public Sprite CompanyArtwork { get; private set; } = null;
        
        [field: SerializeField] public AttributeSetScriptableObject AttributeSet { get; private set; }
            = null;

        [field: SerializeField]
        public AbilityTriggerDefinitionScriptableObject[] AbilityTriggerDefinitions { get; private set; }
            = Array.Empty<AbilityTriggerDefinitionScriptableObject>();
        
        public override ECardType CardType => ECardType.Company;
        
        public override CardBase CreateCard(
            CardPlayer cardPlayer,
            CardData cardData)
        {
            return new CompanyCard(
                cardPlayer,
                cardData,
                this);
        }
    }
}