using UnityEngine;

namespace Pinvestor.CardSystem
{
    public abstract class CardDataScriptableObject : UniqueScriptableObject
    {
        public abstract ECardType CardType { get; }
        
        public abstract CardBase CreateCard(
            CardPlayer cardPlayer,
            CardData cardData);
    }
}