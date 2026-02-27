using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.CardSystem;
using Pinvestor.Game.BallSystem;

namespace Pinvestor.Game
{
    public sealed class RoundContext
    {
        private const string BalanceAttributeName = "Balance";

        public CardPlayer CardPlayer { get; }
        public BallShooter BallShooter { get; }

        public RoundContext(
            CardPlayer cardPlayer,
            BallShooter ballShooter)
        {
            CardPlayer = cardPlayer;
            BallShooter = ballShooter;
        }

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
