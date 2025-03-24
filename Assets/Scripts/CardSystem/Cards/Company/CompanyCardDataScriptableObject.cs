using AttributeSystem.Authoring;
using Pinvestor.CompanySystem;
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
        
        [field: SerializeField] public AttributeSetScriptableObject AttributeSet { get; private set; }
            = null;
        
        [field: SerializeField] public GameObject VisualPrefab { get; private set; } = null;
        
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