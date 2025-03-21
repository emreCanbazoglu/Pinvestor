using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Random = UnityEngine.Random;

namespace Pinvestor.CardSystem
{
    public class CompanySelectionPile : DeckPile
    {
        public Action OnSlotsFilled { get; set; }
        public Action OnSlotsReset { get; set; }
        
        public CompanySelectionPile(Deck deck, DeckPileData deckPileData) 
            : base(deck, deckPileData)
        {
        }

        public async UniTask FillSlots()
        {
            ResetSlots();

            var ids = new string[Slots.Count];
            
            for (int i = 0; i < Slots.Count; i++)
            {
                var cardData = await DrawCard(i, ids);
                
                Deck.TryAddCard(
                    cardData);
                
                ids[i] = cardData.ReferenceCardId;
            }
            
            OnSlotsFilled?.Invoke();
        }
        
        private void ResetSlots()
        {
            foreach (var slot in Slots)
            {
                if(slot.IsEmpty)
                    continue;
                
                Deck.TryRemoveCard(
                    slot.Card);
            }
            
            OnSlotsReset?.Invoke();
        }
        
        private UniTask<CardData> DrawCard(
            int slotIndex,
            params string[] excludeCardIds)
        {
            var companyCards 
                = CardFactory.Instance.CardContainer
                    .GetCardDataOfType<CompanyCardDataScriptableObject>();

            CompanyCardDataScriptableObject companyCard = null;
            
            do
            {
                int randomIndex = Random.Range(0, companyCards.Count);
            
                companyCard = companyCards[randomIndex];
            } while (excludeCardIds.Contains(companyCard.UniqueId));

            
            var cardData = new CardData(
                companyCard.UniqueId,
                DeckPileType,
                slotIndex);

            return UniTask.FromResult(cardData);
        }
    }
}
