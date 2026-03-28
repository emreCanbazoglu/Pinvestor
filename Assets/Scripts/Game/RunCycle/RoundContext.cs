using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Economy;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;

namespace Pinvestor.Game
{
    public sealed class RoundContext
    {
        private const string BalanceAttributeName = "Balance";

        public CardPlayer CardPlayer { get; }
        public BallShooter BallShooter { get; }
        public Board Board { get; }
        public TurnRevenueAccumulator RevenueAccumulator { get; }
        public EconomyService EconomyService { get; }

        /// <summary>
        /// Tracks available/placed/discarded companies for this run.
        /// Initialized at run start; persists across all turns in the run.
        /// </summary>
        public RunCompanyPool CompanyPool { get; }

        /// <summary>
        /// Constructor without economy wiring (backward-compatible).
        /// </summary>
        public RoundContext(
            CardPlayer cardPlayer,
            BallShooter ballShooter,
            Board board,
            RunCompanyPool companyPool = null)
            : this(cardPlayer, ballShooter, board, companyPool, null, null)
        {
        }

        /// <summary>
        /// Constructor with economy wiring.
        /// </summary>
        public RoundContext(
            CardPlayer cardPlayer,
            BallShooter ballShooter,
            Board board,
            RunCompanyPool companyPool,
            TurnRevenueAccumulator revenueAccumulator,
            EconomyService economyService)
        {
            CardPlayer = cardPlayer;
            BallShooter = ballShooter;
            Board = board;
            CompanyPool = companyPool ?? new RunCompanyPool();
            RevenueAccumulator = revenueAccumulator;
            EconomyService = economyService;
        }

        /// <summary>
        /// Returns the player's current net worth by reading the GAS Balance attribute directly.
        /// This is the single source of truth — revenue is credited via EconomyService (GAS
        /// ModifyBaseValue), and op-costs are deducted via Turn.ApplyTurnlyCosts() (also GAS).
        /// </summary>
        public bool TryGetCurrentNetWorth(out float netWorth)
        {
            netWorth = 0f;

            if (CardPlayer == null || CardPlayer.AbilitySystemCharacter == null)
                return false;

            AttributeSystemComponent attributeSystem
                = CardPlayer.AbilitySystemCharacter.AttributeSystem;
            if (attributeSystem == null)
                return false;

            AttributeSetScriptableObject attributeSet = attributeSystem.AttributeSet;
            if (attributeSet == null
                || attributeSet.AttributeDefinitions == null
                || attributeSet.AttributeDefinitions.Length == 0)
                return false;

            if (!attributeSet.TryGetAttributeByName(
                    BalanceAttributeName,
                    out AttributeScriptableObject balanceAttribute))
                return false;

            if (!attributeSystem.TryGetAttributeValue(balanceAttribute, out AttributeValue netWorthValue))
                return false;

            netWorth = netWorthValue.CurrentValue;
            return true;
        }
    }

    public sealed class RoundRuntimeState
    {
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public RoundCycleSettings RoundSettings { get; }

        public RoundRuntimeState(
            int roundIndex,
            int turnIndex,
            RoundCycleSettings roundSettings)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            RoundSettings = roundSettings;
        }
    }
}
