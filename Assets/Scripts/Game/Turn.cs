using Cysharp.Threading.Tasks;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.Game.BallSystem;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        public Board Board { get; private set; }

        /// <summary>
        /// Service for executing player-initiated cashouts during the Offer Phase.
        /// Available after construction; UI callers can retrieve this from the active Turn.
        /// </summary>
        public Game.Economy.CashoutService CashoutService { get; private set; }

        private const string BalanceAttributeName = "Balance";
        private const string TurnlyCostAttributeName = "TurnlyCost";
        private const string HpAttributeName = "HP";

        private EventBinding<CompanyPlacedEvent> _companyPlacedEventBinding;

        private bool _isCompanyPlaced;

        public Turn(
            CardPlayer player,
            BallShooter ballShooter,
            Board board)
        {
            Player = player;
            BallShooter = ballShooter;
            Board = board;
            CashoutService = new Game.Economy.CashoutService(this);
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
            EventBus<TurnResolutionStartedEvent>.Raise(
                new TurnResolutionStartedEvent(roundIndex, turnIndex));

            float totalTurnlyCost = ApplyTurnlyCosts();
            int collapsedCompanyCount = RemoveCollapsedCompanies();
            EventBus<TurnResolutionCompletedEvent>.Raise(
                new TurnResolutionCompletedEvent(
                    roundIndex,
                    turnIndex,
                    totalTurnlyCost,
                    collapsedCompanyCount));

            Debug.Log(
                $"Resolution Summary: totalTurnlyCost={totalTurnlyCost}, collapsedCompanies={collapsedCompanyCount}");

            return UniTask.CompletedTask;
        }

        private float ApplyTurnlyCosts()
        {
            if (Board == null)
                return 0f;

            float totalCost = 0f;
            List<BoardItem_Company> companies = CollectCompanyBoardItems();
            for (int i = 0; i < companies.Count; i++)
            {
                if (TryGetTurnlyCost(companies[i], out float companyTurnlyCost))
                    totalCost += companyTurnlyCost;
            }

            if (totalCost <= 0f)
                return 0f;

            if (!TryGetPlayerBalance(out AttributeSystemComponent playerAttributeSystem, out AttributeScriptableObject balanceAttribute))
                return 0f;

            playerAttributeSystem.ModifyBaseValue(
                balanceAttribute,
                new AttributeModifier { Add = -totalCost },
                out _);

            return totalCost;
        }

        private int RemoveCollapsedCompanies()
        {
            if (Board == null)
                return 0;

            int collapsedCount = 0;
            List<BoardItem_Company> companies = CollectCompanyBoardItems();
            for (int i = 0; i < companies.Count; i++)
            {
                BoardItem_Company companyBoardItem = companies[i];
                if (!IsCollapsed(companyBoardItem))
                    continue;

                if (!companyBoardItem.TryGetPropertySpec(out BoardItemPropertySpec_Destroyable destroyableSpec))
                    continue;

                if (destroyableSpec.IsDestroying)
                    continue;

                // Capture company identity before destroy (wrapper reference becomes invalid after).
                string companyId = string.Empty;
                var boardPosition = new Vector2Int(
                    companyBoardItem.BoardItemData.Col,
                    companyBoardItem.BoardItemData.Row);

                if (companyBoardItem.Wrapper is BoardItemWrapper_Company companyWrapper)
                {
                    companyId = companyWrapper.Company?.CompanyId?.CompanyId ?? string.Empty;
                }

                destroyableSpec.Destroy(null);
                collapsedCount++;

                // Emit collapse event — investment capital is NOT refunded.
                EventBus<CompanyCollapsedEvent>.Raise(
                    new CompanyCollapsedEvent(companyId, boardPosition));

                Debug.Log($"[spec-006] Company '{companyId}' collapsed at {boardPosition}. Investment lost.");
            }

            return collapsedCount;
        }

        private List<BoardItem_Company> CollectCompanyBoardItems()
        {
            if (Board == null)
                return new List<BoardItem_Company>();

            return Board.BoardItems
                .OfType<BoardItem_Company>()
                .ToList();
        }

        private bool TryGetTurnlyCost(
            BoardItem_Company companyBoardItem,
            out float turnlyCost)
        {
            turnlyCost = 0f;

            BoardItemWrapper_Company wrapper = companyBoardItem?.Wrapper as BoardItemWrapper_Company;
            if (wrapper == null || wrapper.AttributeSystemComponent == null)
                return false;

            AttributeSystemComponent attributeSystem = wrapper.AttributeSystemComponent;
            if (!attributeSystem.AttributeSet.TryGetAttributeByName(
                    TurnlyCostAttributeName,
                    out AttributeScriptableObject turnlyCostAttribute))
                return false;

            if (!attributeSystem.TryGetAttributeValue(turnlyCostAttribute, out AttributeValue turnlyCostValue))
                return false;

            turnlyCost = turnlyCostValue.CurrentValue;
            return true;
        }

        private bool IsCollapsed(BoardItem_Company companyBoardItem)
        {
            BoardItemWrapper_Company wrapper = companyBoardItem?.Wrapper as BoardItemWrapper_Company;
            if (wrapper == null || wrapper.AttributeSystemComponent == null)
                return false;

            AttributeSystemComponent attributeSystem = wrapper.AttributeSystemComponent;
            if (!attributeSystem.AttributeSet.TryGetAttributeByName(
                    HpAttributeName,
                    out AttributeScriptableObject hpAttribute))
                return false;

            if (!attributeSystem.TryGetAttributeValue(hpAttribute, out AttributeValue hpValue))
                return false;

            return hpValue.CurrentValue <= 0f;
        }

        private bool TryGetPlayerBalance(
            out AttributeSystemComponent attributeSystem,
            out AttributeScriptableObject balanceAttribute)
        {
            attributeSystem = null;
            balanceAttribute = null;

            if (Player == null || Player.AbilitySystemCharacter == null)
                return false;

            attributeSystem = Player.AbilitySystemCharacter.AttributeSystem;
            if (attributeSystem == null || attributeSystem.AttributeSet == null)
                return false;

            return attributeSystem.AttributeSet.TryGetAttributeByName(
                BalanceAttributeName,
                out balanceAttribute);
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
