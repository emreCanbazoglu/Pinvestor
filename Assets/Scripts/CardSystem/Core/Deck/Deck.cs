using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.CardSystem
{
    public class Deck
    {
        public CardPlayer Owner { get; private set; }
        
        public List<DeckPile> DeckPiles { get; private set; }
            = new List<DeckPile>();
        
        private IDeckDataProvider _deckDataProvider;
        
        public Action<CardBase> OnCardAdded { get; set; }
        public Action<CardBase> OnCardRemoved { get; set; }
        
        public Deck(
            CardPlayer owner,
            IDeckDataProvider deckDataProvider)
        {
            Owner = owner;
            _deckDataProvider = deckDataProvider;
            
            Initialize();
        }

        private void Initialize()
        {
            IReadOnlyList<DeckPileData> deckPileData
                = _deckDataProvider.GetPileData();

            for (int i = 0; i < deckPileData.Count; i++)
            {
                DeckPile deckPile
                    = DeckPileFactory.Instance.CreateDeckPile(
                        this,
                        deckPileData[i]);

                DeckPiles.Add(deckPile);
            }

            IReadOnlyList<CardData> allCardData
                = _deckDataProvider.GetCardData();

            foreach (var cardData in allCardData)
            {
                if (!CardFactory.Instance.TryCreateCard(
                        Owner,
                        cardData,
                        out CardBase card))
                    continue;

                if (!TryAddCard(card, setDeckData: false))
                {
                    Debug.LogError("Failed to add card to deck pile: " + cardData.ReferenceCardId);
                }
            }
        }

        public bool TryAddCard(
            CardData cardData,
            bool setDeckData = true)
        {
            if (!CardFactory.Instance.TryCreateCard(
                    Owner,
                    cardData,
                    out CardBase card))
                return false;

            TryAddCard(card, setDeckData);

            return true;
        }
        
        private bool TryAddCard(
            CardBase card,
            bool setDeckData = true)
        {
            if (!TryGetDeckPile(
                    card.CardData.DeckPile,
                    out DeckPile deckPile))
                return false;

            deckPile.TryAddCard(
                card.CardData.SlotIndex,
                card);

            //if (setDeckData)
                //UpdateDeckData();
            
            OnCardAdded?.Invoke(card);
            
            return true;
        }
        
        public bool TryAddCard(
            CardBase card,
            DeckPile deckPile,
            bool setDeckData = true)
        {
            if(!DeckPiles.Contains(deckPile))
                return false;

            if (card.CardData.DeckPile != EDeckPile.None)
            {
                if (!TryRemoveCard(
                        card,
                        setDeckData: false))
                    return false;
            }
            
            if (!TryGetDeckPile(
                    card.CardData.DeckPile,
                    out DeckPile newDeckPile))
                return false;
            
            newDeckPile.TryAddCard(
                card.CardData.SlotIndex,
                card);

            //if (setDeckData)
            //UpdateDeckData();
            
            OnCardAdded?.Invoke(card);
            
            return true;
        }

        public bool TryRemoveCard(
            CardBase card,
            bool setDeckData = true)
        {
            if(!TryGetDeckPile(
                   card.CardData.DeckPile,
                   out DeckPile deckPile))
                return false;

            if (!deckPile.TryRemoveCard(card))
                return false;
            
            //if (setDeckData)
                //UpdateDeckData();

            OnCardRemoved?.Invoke(card);

            return true;
        }
        
        public bool TryGetDeckPile(
            EDeckPile deckPileType,
            out DeckPile deckPile)
        {
            deckPile = null;

            foreach (var pile in DeckPiles)
            {
                if (pile.DeckPileType != deckPileType)
                    continue;

                deckPile = pile;

                return true;
            }

            return false;
        }
        
        public bool TryGetDeckPile<T>(
            out T deckPile)
            where T : DeckPile
        {
            deckPile = null;

            foreach (var pile in DeckPiles)
            {
                if (pile is not T castedPile)
                    continue;

                deckPile = castedPile;

                return true;
            }

            return false;
        }
        
        public List<CardBase> GetAllCards()
        {
            List<CardBase> allCards = new List<CardBase>();

            foreach (var pile in DeckPiles)
                allCards.AddRange(pile.Cards);

            return allCards;
        }
    }
}