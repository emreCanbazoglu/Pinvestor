using UnityEngine;

namespace Pinvestor.CardSystem.Authoring
{
    public abstract class CardWrapperBase : MonoBehaviour
    {
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