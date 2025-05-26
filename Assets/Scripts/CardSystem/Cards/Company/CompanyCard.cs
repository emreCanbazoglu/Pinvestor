using Pinvestor.CardSystem.Authoring;

namespace Pinvestor.CardSystem
{
    public class CompanyCard : CardBase<CompanyCardDataScriptableObject>
    {
        public CompanyCard(
            CardPlayer cardPlayer,
            CardData cardData,
            CompanyCardDataScriptableObject cardDataSo)
            : base(cardPlayer, cardData, cardDataSo)
        {
        }
        
        public string GetCompanyAbilityDescription()
        {
            return CastedCardDataSo.AbilityTriggerDefinitions.Length > 0
                ? CastedCardDataSo.AbilityTriggerDefinitions[0].Ability.GetDescription()
                : string.Empty;
        }
    }
}
