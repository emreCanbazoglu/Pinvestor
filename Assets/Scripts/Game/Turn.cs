using Cysharp.Threading.Tasks;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public class Turn
    {
        public CardPlayer Player { get; private set; }
        
        private EventBinding<CompanyPlacedEvent> _companyPlacedEventBinding;
        
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

            _companyPlacedEventBinding
                = new EventBinding<CompanyPlacedEvent>(
                    OnCompanyPlaced);
            
            EventBus<CompanyPlacedEvent>
                .Register(
                    _companyPlacedEventBinding);
        }

        private void OnCompanyPlaced(
            CompanyPlacedEvent e)
        {
            EventBus<CompanyPlacedEvent>
                .Deregister(
                    _companyPlacedEventBinding);
            
            Debug.Log("Company placed: " + e.Company.Company.CompanyId.CompanyId);
        }
    }
}