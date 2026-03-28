namespace Pinvestor.Game
{
    /// <summary>
    /// Raised on EventBus at the end of the final round to signal win or loss.
    /// Subscribers should react to outcome and stop the run.
    /// </summary>
    public sealed class RunOutcomeEvent : IEvent
    {
        /// <summary>True if the player's net worth reached the final round target.</summary>
        public bool IsWin { get; }

        /// <summary>Player's net worth at the moment of outcome evaluation.</summary>
        public float FinalNetWorth { get; }

        /// <summary>The target net worth required to win the final round.</summary>
        public float TargetNetWorth { get; }

        public RunOutcomeEvent(
            bool isWin,
            float finalNetWorth,
            float targetNetWorth)
        {
            IsWin = isWin;
            FinalNetWorth = finalNetWorth;
            TargetNetWorth = targetNetWorth;
        }
    }
}
