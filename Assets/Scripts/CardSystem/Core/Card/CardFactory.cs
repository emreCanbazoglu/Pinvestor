using UnityEngine;
using UnityEngine.Serialization;

namespace Pinvestor.CardSystem
{
    public class CardFactory : Singleton<CardFactory>
    {
        [field: SerializeField] public CardContainerScriptableObject CardContainer { get; private set; } = null;

        public bool TryCreateCard(
            CardPlayer cardPlayer,
            CardData cardData,
            out CardBase card)
        {
            card = null;

            if (!CardContainer.TryGetCardData(
                    cardData.ReferenceCardId,
                    out CardDataScriptableObject cardDataSo))
                return false;

            card = cardDataSo.CreateCard(cardPlayer, cardData);

            return true;
        }
    }
}