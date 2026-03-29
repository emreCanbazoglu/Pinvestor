using Cysharp.Threading.Tasks;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Economy;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;
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

        private readonly TurnRevenueAccumulator _revenueAccumulator;
        private readonly EconomyService _economyService;

        private const string BalanceAttributeName = "Balance";
        private const string TurnlyCostAttributeName = "TurnlyCost";
        private const string HpAttributeName = "HP";

        private EventBinding<CompanyPlacedEvent> _companyPlacedEventBinding;

        private bool _isCompanyPlaced;

        // Offer phase dependencies.
        private readonly RunCompanyPool _companyPool;
        private readonly CompanyConfigService _companyConfigService;

        /// <summary>
        /// The company selected during the offer phase for this turn.
        /// Available to the placement phase after RunOfferPhase completes.
        /// </summary>
        public CompanyConfigModel SelectedCompany { get; private set; }


        public Turn(
            CardPlayer player,
            BallShooter ballShooter,
            Board board,
            RunCompanyPool companyPool = null,
            CompanyConfigService companyConfigService = null)
            : this(player, ballShooter, board, companyPool, companyConfigService, null, null)
        {
        }

        public Turn(
            CardPlayer player,
            BallShooter ballShooter,
            Board board,
            RunCompanyPool companyPool,
            CompanyConfigService companyConfigService,
            TurnRevenueAccumulator revenueAccumulator,
            EconomyService economyService)
        {
            Player = player;
            BallShooter = ballShooter;
            Board = board;
            _companyPool = companyPool;
            _companyConfigService = companyConfigService;
            _revenueAccumulator = revenueAccumulator;
            _economyService = economyService;
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

            // Reset accumulator and subscribe to all active company revenue generators
            // before this turn's launch phase begins.
            ResetAndSubscribeAccumulator();

            await RunOfferPhase(roundIndex, turnIndex);
            await RunPlacementPhase(roundIndex, turnIndex);
            await RunLaunchPhase(roundIndex, turnIndex);

            // Unsubscribe after Launch Phase — no more hits should count for this turn.
            UnsubscribeAccumulator();

            await RunResolutionPhase(roundIndex, turnIndex);
        }

        private async UniTask RunOfferPhase(
            int roundIndex,
            int turnIndex)
        {
            LogPhase(ETurnPhase.Offer, roundIndex, turnIndex);

            if (_companyPool == null || _companyConfigService == null)
            {
                Debug.LogError("[Turn] RunOfferPhase: CompanyPool or CompanyConfigService is null. Cannot run offer phase.");
                return;
            }

            await RunNewOfferPhase();
        }

        /// <summary>
        /// New offer phase flow:
        /// 1. Draw 3 companies from RunCompanyPool via CompanyOfferDrawer.
        /// 2. Populate OfferPhaseContext and open the UI panel.
        /// 3. Await player selection.
        /// 4. Mark unselected companies as discarded.
        /// 5. Store selected company on SelectedCompany for the placement phase.
        /// </summary>
        private async UniTask RunNewOfferPhase()
        {
            CompanyOfferDrawer drawer = new CompanyOfferDrawer(_companyPool, _companyConfigService);
            List<CompanyConfigModel> offered = drawer.DrawOffer();

            if (offered.Count == 0)
            {
                Debug.LogWarning("[Turn] RunNewOfferPhase: No companies available to offer. Skipping offer phase.");
                return;
            }

            OfferPhaseContext context = new OfferPhaseContext(offered);

            // Open the UI panel and wait for selection.
            EventBus<ShowCompanyOfferPanelEvent>.Raise(
                new ShowCompanyOfferPanelEvent(context));

            CompanyConfigModel result = await context.SelectionTask;

            // Fallback guard (T020): if somehow the task resolved without a real selection, force-pick first.
            if (result == null)
            {
                Debug.LogWarning("[Turn] RunNewOfferPhase: Selection resolved with null. Auto-selecting first company.");
                context.ForceSelectFirst();
                result = context.ConfirmedSelection;
            }

            // Close the offer panel.
            EventBus<HideCompanyOfferPanelEvent>.Raise(new HideCompanyOfferPanelEvent());

            // Mark the 2 unselected cards as discarded (T010).
            foreach (var company in offered)
            {
                if (company.CompanyId != result.CompanyId)
                    _companyPool.MarkDiscarded(company.CompanyId);
            }

            // Store selected company — placement phase will read it (T011).
            SelectedCompany = result;

            // Clear context to prevent stale state.
            context.Clear();

            // Register the company-placed binding so RunPlacementPhase can await _isCompanyPlaced.
            _companyPlacedEventBinding = new EventBinding<CompanyPlacedEvent>(OnCompanyPlaced);
            EventBus<CompanyPlacedEvent>.Register(_companyPlacedEventBinding);

            Debug.Log($"[Turn] Offer phase complete. Selected: {result.CompanyId}");
        }

        private async UniTask RunPlacementPhase(
            int roundIndex,
            int turnIndex)
        {
            LogPhase(ETurnPhase.Placement, roundIndex, turnIndex);

            if (SelectedCompany == null)
            {
                Debug.LogError("[Turn] RunPlacementPhase: SelectedCompany is null. Cannot run placement phase.");
                return;
            }

            // Create board item directly from config company ID — no card system needed.
            var boardItemData = new BoardItemData_Company(SelectedCompany.CompanyId);
            var boardItem = BoardItemFactory.Instance.CreateBoardItem(boardItemData) as BoardItem_Company;
            var wrapper = boardItem.CreateWrapper() as BoardItemWrapper_Company;

            // Signal the input controller to start drag-to-place.
            EventBus<CompanyReadyForPlacementEvent>.Raise(new CompanyReadyForPlacementEvent(wrapper));

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

            // Economy: credit turn revenue to the CardPlayer's Balance attribute via EconomyService.
            // Op-costs are handled separately by ApplyTurnlyCosts() below — do not pass companies here.
            _economyService?.ApplyResolution(Player);

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

        private void ResetAndSubscribeAccumulator()
        {
            if (_revenueAccumulator == null)
                return;

            _revenueAccumulator.Reset();

            // Subscribe to every active company's RevenueGenerator on the board.
            List<BoardItem_Company> companies = CollectCompanyBoardItems();
            for (int i = 0; i < companies.Count; i++)
            {
                BoardItemWrapper_Company wrapper
                    = companies[i]?.Wrapper as BoardItemWrapper_Company;

                if (wrapper?.RevenueGenerator != null)
                    _revenueAccumulator.Subscribe(wrapper.RevenueGenerator);
            }
        }

        private void UnsubscribeAccumulator()
        {
            _revenueAccumulator?.UnsubscribeAll();
        }

        private void OnCompanyPlaced(
            CompanyPlacedEvent e)
        {
            EventBus<CompanyPlacedEvent>
                .Deregister(
                    _companyPlacedEventBinding);

            string companyId = e.Company.Company.CompanyId.CompanyId;
            Debug.Log("Company placed: " + companyId);

            // Mark the placed company in the pool so it won't be offered again (T004).
            if (_companyPool != null)
                _companyPool.MarkPlaced(companyId);

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
