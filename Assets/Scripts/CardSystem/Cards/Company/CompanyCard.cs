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

    }
}
