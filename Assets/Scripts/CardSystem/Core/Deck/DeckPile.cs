using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    public interface IDeckPile
    {
        
    }
    
    public class DeckPile : IDeckPile
    {
        private readonly List<CardBase> _cards = new List<CardBase>();
        public IReadOnlyList<CardBase> Cards => _cards;
        
        private readonly List<Slot> _slots = new List<Slot>();
        public IReadOnlyList<Slot> Slots => _slots;
        
        public Deck Deck { get; private set; }
        public EDeckPile DeckPileType { get; protected set; }
        
        public DeckPile(
            Deck deck,
            DeckPileData deckPileData)
        {
            Deck = deck;
            DeckPileType = deckPileData.DeckPileType;
            
            InitializeSlots(deckPileData.SlotCapacity);
        }

        private void InitializeSlots(
            int slotCapacity)
        {
            for (int i = 0; i < slotCapacity; i++)
            {
                _slots.Add(
                    new Slot(i, this));
            }
        }
        
        public bool TryGetEmptySlot(
            out Slot slot)
        {
            slot = Slots.FirstOrDefault(
                val => val.IsEmpty);
            
            return slot != null;
        }

        public bool TryGetSlot(
            int index,
            out Slot slot)
        {
            slot = Slots.FirstOrDefault(
                val => val.Index == index);
            
            return slot != null;
        }
        
        public bool TryGetCardInSlot(
            int index,
            out CardBase card)
        {
            card = null;
            
            Slot slot = Slots.FirstOrDefault(
                val => val.Index == index);

            if (slot == null)
                return false;
            
            card = slot.Card;
            
            return card != null;
        }
        
        public bool TryAddCard(
            int index,
            CardBase card)
        {
            if(_cards.Contains(card))
                return false;
            
            _cards.Add(card);
            
            if(!TryGetSlot(index, out Slot slot))
                return false;
            
            slot.SetCard(card);
            
            Debug.Log("Card with reference id: " + card.CardData.ReferenceCardId + " added to slot: " + card.CardData.SlotIndex);

            return true;
        }
        
        public bool TryRemoveCard(
            CardBase card)
        {
            Slot slot = Slots.FirstOrDefault(
                val => val.Card == card);
            
            if (slot == null)
                return false;
            
            if (!slot.TryRemoveCard())
                return false;
            
            card.SetDeckPile(EDeckPile.None);
            
            Debug.Log("Card removed from slot: " + card.CardData.SlotIndex);
            
            return _cards.Remove(card);
        }
        

        public bool HasSlot(int index)
        {
            return Slots.Any(
                val => val.Index == index);
        }
        
        public CardBase[] GetCardsInSlots()
        {
            return Slots
                .Where(val => !val.IsEmpty)
                .Select(val => val.Card)
                .ToArray();
        }
        
    }
}