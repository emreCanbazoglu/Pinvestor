using Cysharp.Threading.Tasks;
using Pinvestor.CardSystem;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public class Turn
    {
        public CardPlayer Player { get; private set; }
        public BallShooter BallShooter { get; private set; }
        
        private EventBinding<CompanyPlacedEvent> _companyPlacedEventBinding;
        
        private bool _isCompanyPlaced;
        
        public Turn(
            CardPlayer player,
            BallShooter ballShooter)
        {
            Player = player;
            BallShooter = ballShooter;
        }
        
        public async UniTask StartAsync()
        {
            _isCompanyPlaced = false;
            
            await ChooseCompanyCard();
            
            await UniTask.WaitUntil(() => _isCompanyPlaced);

            await BallShooter.ShootBallAsync();
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
            
            Player.Deck.TryGetDeckPile<CompanySelectionPile>(
                out var chooseCompanyPile);

            chooseCompanyPile.ResetSlots();
            
            _isCompanyPlaced = true;
        }
    }
}