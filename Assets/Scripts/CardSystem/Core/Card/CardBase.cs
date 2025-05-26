using System;
using Pinvestor.AbilitySystem.Abilities;
using Pinvestor.CardSystem.Authoring;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    public abstract class CardBase : IDisposable
    {
        public CardPlayer Owner { get; private set; }
        public CardData CardData { get; private set; }
        public abstract CardDataScriptableObject CardDataScriptableObject { get; }
        
        
        protected CardBase(
            CardPlayer owner,
            CardData cardData)
        {
            Owner = owner;
            CardData = cardData;
        }
        
        public void SetCardRemovedFromSlot()
        {
            SetSlotIndex(Slot.EMPTY_SLOT_INDEX);
            
            SetCardRemovedFromSlotCore();
        }
        
        protected virtual void SetCardRemovedFromSlotCore()
        {
            
        }
        
        public void SetCardAddedToSlot(
            int slotIndex, 
            object additionalData = null)
        {
            SetSlotIndex(slotIndex);
            
            SetCardAddedToSlotCore();
        }
        
        protected virtual void SetCardAddedToSlotCore(
            object additionalData = null)
        {
            
        }
        
        private void SetSlotIndex(int slotIndex)
        {
            CardData.SetSlotIndex(slotIndex);
        }
        
        public void SetDeckPile (
            EDeckPile deckPile)
        {
            CardData.SetDeckPile(deckPile);
        }

        public abstract void Dispose();
    }
    
    public abstract class CardBase<TCardDataScriptableObject> : CardBase
        where TCardDataScriptableObject : CardDataScriptableObject
    {
        public CardWrapperBase CardWrapper { get; private set; }
        public TCardDataScriptableObject CastedCardDataSo { get; private set; }
        
        public sealed override CardDataScriptableObject CardDataScriptableObject => CastedCardDataSo;
        
        protected CardBase(
            CardPlayer owner,
            CardData cardData,
            TCardDataScriptableObject cardDataSo) : base(owner, cardData)
        {
            CastedCardDataSo = cardDataSo;
        }

        public CardWrapperBase CreateWrapper()
        {
            var cardWrapper
                = GameObject.Instantiate(
                    CardDataScriptableObject.CardWrapperPrefab);
            
            cardWrapper.WrapCard(this);
            
            CardWrapper = cardWrapper;
            CardWrapper.OnDisposed += OnWrapperDisposed;
            
            return cardWrapper;
        }
        
        private void OnWrapperDisposed()
        {
            CardWrapper.OnDisposed -= OnWrapperDisposed;
            CardWrapper = null;
        }

        public sealed override void Dispose()
        {
            if (CardWrapper != null)
            {
                CardWrapper.OnDisposed -= OnWrapperDisposed;
                CardWrapper = null;
            }
            
            DisposeCore();
        }

        protected virtual void DisposeCore()
        {
            
        }
    }
}