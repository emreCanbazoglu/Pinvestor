namespace Pinvestor.Game
{
    public sealed class RoundStartedEvent : IEvent
    {
        public int RoundIndex { get; }
        public string RoundId { get; }
        public int TurnCount { get; }
        public float RequiredWorth { get; }

        public RoundStartedEvent(
            int roundIndex,
            string roundId,
            int turnCount,
            float requiredWorth)
        {
            RoundIndex = roundIndex;
            RoundId = roundId;
            TurnCount = turnCount;
            RequiredWorth = requiredWorth;
        }
    }

    public sealed class TurnStartedEvent : IEvent
    {
        public int RoundIndex { get; }
        public int TurnIndex { get; }

        public TurnStartedEvent(
            int roundIndex,
            int turnIndex)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
        }
    }

    public sealed class TurnCompletedEvent : IEvent
    {
        public int RoundIndex { get; }
        public int TurnIndex { get; }

        public TurnCompletedEvent(
            int roundIndex,
            int turnIndex)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
        }
    }

    public sealed class TurnResolutionStartedEvent : IEvent
    {
        public int RoundIndex { get; }
        public int TurnIndex { get; }

        public TurnResolutionStartedEvent(
            int roundIndex,
            int turnIndex)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
        }
    }

    public sealed class TurnResolutionCompletedEvent : IEvent
    {
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public float TotalTurnlyCost { get; }
        public int CollapsedCompanyCount { get; }

        public TurnResolutionCompletedEvent(
            int roundIndex,
            int turnIndex,
            float totalTurnlyCost,
            int collapsedCompanyCount)
        {
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            TotalTurnlyCost = totalTurnlyCost;
            CollapsedCompanyCount = collapsedCompanyCount;
        }
    }

    public sealed class RoundCompletedEvent : IEvent
    {
        public int RoundIndex { get; }
        public bool WasRequirementEvaluated { get; }
        public bool PassedRequirement { get; }
        public float CurrentWorth { get; }
        public float RequiredWorth { get; }
        public string Message { get; }

        public RoundCompletedEvent(RoundExecutionResult result)
        {
            RoundIndex = result.RoundIndex;
            WasRequirementEvaluated = result.WasRequirementEvaluated;
            PassedRequirement = result.PassedRequirement;
            CurrentWorth = result.CurrentWorth;
            RequiredWorth = result.RequiredWorth;
            Message = result.Message;
        }
    }

    public sealed class RunCycleCompletedEvent : IEvent
    {
        public bool AllEvaluatedRoundsPassed { get; }
        public int CompletedRoundCount { get; }
        public int TotalRoundCount { get; }

        public RunCycleCompletedEvent(
            bool allEvaluatedRoundsPassed,
            int completedRoundCount,
            int totalRoundCount)
        {
            AllEvaluatedRoundsPassed = allEvaluatedRoundsPassed;
            CompletedRoundCount = completedRoundCount;
            TotalRoundCount = totalRoundCount;
        }
    }
}
