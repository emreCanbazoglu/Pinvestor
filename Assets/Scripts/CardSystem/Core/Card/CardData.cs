using System;

namespace Pinvestor.CardSystem
{
    public class CardData
    {
        public string ReferenceCardId { get; private set; }
        public EDeckPile DeckPile { get; private set; }
        public int SlotIndex { get; private set; }
        
        public Action OnDataUpdated { get; set; }
        
        public CardData()
        {
        }
        
        public CardData(
            string referenceCardId,
            EDeckPile deckPile,
            int slotIndex)
        {
            ReferenceCardId = referenceCardId;
            DeckPile = deckPile;
            SlotIndex = slotIndex;
        }
        
        public void SetSlotIndex(int slotIndex)
        {
            SlotIndex = slotIndex;
            
            DataUpdated();
        }
        
        public void SetDeckPile(
            EDeckPile deckPile)
        {
            DeckPile = deckPile;
            
            DataUpdated();
        }
        
        private void DataUpdated()
        {
            OnDataUpdated?.Invoke();
        }
    }
}