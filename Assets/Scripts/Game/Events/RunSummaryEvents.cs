namespace Pinvestor.Game
{
    /// <summary>
    /// Raised by GameManager after the run cycle completes (win, loss, or early stop)
    /// to open the run summary screen. Carries everything the UI needs to display.
    /// </summary>
    public sealed class ShowRunSummaryEvent : IEvent
    {
        public bool IsWin { get; }
        public float FinalNetWorth { get; }
        public float TargetNetWorth { get; }
        public int CompletedRoundCount { get; }
        public int TotalRoundCount { get; }
        public int CompaniesPlaced { get; }
        public int CompaniesCollapsed { get; }
        public int CompaniesCashedOut { get; }
        public float TotalCashoutPayout { get; }

        public ShowRunSummaryEvent(
            bool isWin,
            float finalNetWorth,
            float targetNetWorth,
            int completedRoundCount,
            int totalRoundCount,
            int companiesPlaced,
            int companiesCollapsed,
            int companiesCashedOut,
            float totalCashoutPayout)
        {
            IsWin = isWin;
            FinalNetWorth = finalNetWorth;
            TargetNetWorth = targetNetWorth;
            CompletedRoundCount = completedRoundCount;
            TotalRoundCount = totalRoundCount;
            CompaniesPlaced = companiesPlaced;
            CompaniesCollapsed = companiesCollapsed;
            CompaniesCashedOut = companiesCashedOut;
            TotalCashoutPayout = totalCashoutPayout;
        }
    }

    /// <summary>
    /// Raised by the run summary UI when the player clicks "Play Again".
    /// GameManager awaits this before reloading the scene.
    /// </summary>
    public sealed class RunSummaryDismissedEvent : IEvent { }
}
