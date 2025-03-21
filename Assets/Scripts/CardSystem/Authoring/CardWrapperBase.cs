using UnityEngine;

namespace Pinvestor.CardSystem.Authoring
{
    public abstract class CardWrapperBase : MonoBehaviour
    {
        [field: SerializeField] public ECardType CardType { get; private set; } = ECardType.None;

        public CardBase Card { get; private set; }

        public void WrapCard(CardBase card)
        {
            Card = card;
            
            WrapCardCore();
        }

        protected virtual void WrapCardCore()
        {
            
        }
    }
}