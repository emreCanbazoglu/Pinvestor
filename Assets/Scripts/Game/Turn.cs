using Cysharp.Threading.Tasks;
using Pinvestor.CardSystem;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public enum ETurnPhase
    {
        None = 0,
        Offer = 10,
        Placement = 20,
        Launch = 30,
        Resolution = 40,
    }

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
            await StartAsync(-1, -1);
        }

        public async UniTask StartAsync(
            int roundIndex,
            int turnIndex)
        {
            await ExecuteCoreTurnAsync(roundIndex, turnIndex);
        }

        public async UniTask ExecuteCoreTurnAsync(
            int roundIndex,
            int turnIndex)
        {
            _isCompanyPlaced = false;

            await RunOfferPhase(roundIndex, turnIndex);
            await RunPlacementPhase(roundIndex, turnIndex);
            await RunLaunchPhase(roundIndex, turnIndex);
            await RunResolutionPhase(roundIndex, turnIndex);
        }
        
        private async UniTask RunOfferPhase(
            int roundIndex,
            int turnIndex)
        {
            LogPhase(ETurnPhase.Offer, roundIndex, turnIndex);
            await ChooseCompanyCard();
        }

        private async UniTask RunPlacementPhase(
            int roundIndex,
            int turnIndex)
        {
            LogPhase(ETurnPhase.Placement, roundIndex, turnIndex);
            await UniTask.WaitUntil(() => _isCompanyPlaced);
        }

        private async UniTask RunLaunchPhase(
            int roundIndex,
            int turnIndex)
        {
            LogPhase(ETurnPhase.Launch, roundIndex, turnIndex);
            await BallShooter.ShootBallAsync();
        }

        private UniTask RunResolutionPhase(
            int roundIndex,
            int turnIndex)
        {
            LogPhase(ETurnPhase.Resolution, roundIndex, turnIndex);
            // Placeholder for end-of-turn resolution logic (cost deduction, collapse checks, events).
            return UniTask.CompletedTask;
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

        private static void LogPhase(
            ETurnPhase phase,
            int roundIndex,
            int turnIndex)
        {
            if (roundIndex >= 0 && turnIndex >= 0)
            {
                Debug.Log($"Turn Phase: {phase} (Round {roundIndex + 1}, Turn {turnIndex + 1})");
                return;
            }

            Debug.Log($"Turn Phase: {phase}");
        }
    }
}
