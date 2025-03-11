using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public class Turn
    {
        public CardPlayer Player { get; private set; }
        
        public Turn(
            CardPlayer player)
        {
            Player = player;
        }
        
        public async UniTask StartAsync()
        {
            await ChooseCompanyCard();
        }
        
        private async UniTask ChooseCompanyCard()
        {
            Player.Deck.TryGetDeckPile<CompanySelectionPile>(
                out var chooseCompanyPile);

            await chooseCompanyPile.FillSlots();

            var companyCardOptions
                = chooseCompanyPile.GetCardsInSlots();
            
            List<CompanyCard> companyCards
                = new List<CompanyCard>();
            
            foreach (var companyCardOption in companyCardOptions)
            {
                companyCards.Add(
                    companyCardOption as CompanyCard);
            }
            
            Debug.Log("Company cards: " + companyCards.Count);
            
            var companySelectionRequestEvent
                = new CompanySelectionRequestEvent(
                    companyCards,
                    OnCompanyCardSelected);
            
            EventBus<CompanySelectionRequestEvent>
                .Raise(companySelectionRequestEvent);
        }
        
        private void OnCompanyCardSelected(
            CompanyCard companyCard)
        {
            Debug.Log("Company card selected: " + companyCard.CastedCardDataSo.CompanyName);
        }
    }
}