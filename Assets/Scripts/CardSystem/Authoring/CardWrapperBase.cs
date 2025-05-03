using System;
using UnityEngine;

namespace Pinvestor.CardSystem.Authoring
{
    public abstract class CardWrapperBase : MonoBehaviour
    {
        public CardBase Card { get; private set; }
        
        public Action OnDisposed { get; set; }

        public void WrapCard(CardBase card)
        {
            Card = card;
            
            WrapCardCore();
        }

        protected virtual void WrapCardCore()
        {
            
        }

        public void DestroyWrapper()
        {
            Dispose();
            
            Destroy(gameObject);
        }
        
        private void Dispose()
        {
            Card = null;
            
            DisposeCore();
            
            OnDisposed?.Invoke();
        }
        
        protected virtual void DisposeCore()
        {
            
        }
    }
}