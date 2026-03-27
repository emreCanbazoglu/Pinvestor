using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Economy;

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
        /// Constructor without economy wiring (backward-compatible).
        /// </summary>
        public RoundContext(
            CardPlayer cardPlayer,
            BallShooter ballShooter,
            Board board)
            : this(cardPlayer, ballShooter, board, null, null)
        {
        }

        /// <summary>
        /// Constructor with economy wiring.
        /// </summary>
        public RoundContext(
            CardPlayer cardPlayer,
            BallShooter ballShooter,
            Board board,
            TurnRevenueAccumulator revenueAccumulator,
            EconomyService economyService)
        {
            CardPlayer = cardPlayer;
            BallShooter = ballShooter;
            Board = board;
            RevenueAccumulator = revenueAccumulator;
            EconomyService = economyService;
        }

        /// <summary>
        /// Returns the player's current net worth for win/loss evaluation.
        /// Reads from PlayerEconomyState (the single source of truth written by EconomyService).
        /// Falls back to the CardPlayer Balance attribute only when PlayerEconomyState is not
        /// present in the scene — this preserves backward compatibility in non-economy scenes
        /// but will not reflect revenue or op-costs calculated by EconomyService.
        /// </summary>
        public bool TryGetCurrentNetWorth(out float netWorth)
        {
            netWorth = 0f;

            // Primary path: PlayerEconomyState is the single source of truth.
            // EconomyService.ApplyResolution() writes here each turn; reading here ensures
            // EvaluateRequirement() and RunOutcomeEvent see the same economy the service computed.
            if (PlayerEconomyState.Instance != null && PlayerEconomyState.Instance.IsInitialized)
            {
                netWorth = PlayerEconomyState.Instance.NetWorth;
                return true;
            }

            // Fallback: no economy state in scene — read the legacy CardPlayer Balance attribute.
            // This path is only hit in scenes that have not added PlayerEconomyState to the scene.
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
