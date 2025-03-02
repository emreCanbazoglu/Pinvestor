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
        
        [field: SerializeField] public string CompanyName { get; private set; } = string.Empty;
        
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