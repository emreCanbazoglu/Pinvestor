using Pinvestor.AbilitySystem.Abilities;
using Pinvestor.CardSystem.Authoring;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    public abstract class CardDataScriptableObject : UniqueScriptableObject
    {
        [field: SerializeField] public PlayCardAbilityScriptableObject CardAbilityScriptableObject { get; private set; }
        
        [field: SerializeField] public CardWrapperBase CardWrapperPrefab { get; private set; }
        
        public abstract ECardType CardType { get; }
        
        public abstract CardBase CreateCard(
            CardPlayer cardPlayer,
            CardData cardData);
    }
}