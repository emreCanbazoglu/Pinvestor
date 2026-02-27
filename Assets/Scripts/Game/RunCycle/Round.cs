using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinvestor.Game
{
    public sealed class Round
    {
        public int RoundIndex { get; }
        public RoundCycleSettings Settings { get; }

        private readonly IReadOnlyList<IRoundPhase> _phases;

        public Round(
            int roundIndex,
            RoundCycleSettings settings,
            IReadOnlyList<IRoundPhase> phases)
        {
            RoundIndex = roundIndex;
            Settings = settings;
            _phases = phases;
        }

        public async UniTask<RoundExecutionResult> ExecuteAsync(RoundContext context)
        {
            if (Settings == null)
                return RoundExecutionResult.Skipped(RoundIndex, "Round settings is null.");

            int turnCount = Mathf.Max(0, Settings.TurnCount);
            Debug.Log(
                $"Round Start: {Settings.RoundId} (index={RoundIndex + 1}, turns={turnCount}, requiredWorth={Settings.RequiredWorth})");
            EventBus<RoundStartedEvent>.Raise(
                new RoundStartedEvent(
                    RoundIndex,
                    Settings.RoundId,
                    turnCount,
                    Settings.RequiredWorth));

            for (int turnIndex = 0; turnIndex < turnCount; turnIndex++)
            {
                EventBus<TurnStartedEvent>.Raise(new TurnStartedEvent(RoundIndex, turnIndex));
                RoundRuntimeState runtimeState = new RoundRuntimeState(RoundIndex, turnIndex, Settings);
                for (int phaseIndex = 0; phaseIndex < _phases.Count; phaseIndex++)
                {
                    IRoundPhase phase = _phases[phaseIndex];
                    await phase.ExecuteAsync(context, runtimeState);
                }

                EventBus<TurnCompletedEvent>.Raise(new TurnCompletedEvent(RoundIndex, turnIndex));
            }

            RoundExecutionResult result = EvaluateRequirement(context);
            Debug.Log(
                $"Round End: {Settings.RoundId} (index={RoundIndex + 1}, requirementEvaluated={result.WasRequirementEvaluated}, passed={result.PassedRequirement})");
            EventBus<RoundCompletedEvent>.Raise(new RoundCompletedEvent(result));
            return result;
        }

        private RoundExecutionResult EvaluateRequirement(RoundContext context)
        {
            if (context == null)
                return RoundExecutionResult.NotEvaluated(
                    RoundIndex,
                    "Round context is null.");

            if (!context.TryGetCurrentNetWorth(out float currentWorth))
                return RoundExecutionResult.NotEvaluated(
                    RoundIndex,
                    "Could not resolve current net worth from CardPlayer.");

            bool passed = currentWorth >= Settings.RequiredWorth;
            return RoundExecutionResult.Evaluated(
                RoundIndex,
                currentWorth,
                Settings.RequiredWorth,
                passed);
        }
    }

    public readonly struct RoundExecutionResult
    {
        public int RoundIndex { get; }
        public bool WasRequirementEvaluated { get; }
        public bool PassedRequirement { get; }
        public float CurrentWorth { get; }
        public float RequiredWorth { get; }
        public string Message { get; }

        private RoundExecutionResult(
            int roundIndex,
            bool wasRequirementEvaluated,
            bool passedRequirement,
            float currentWorth,
            float requiredWorth,
            string message)
        {
            RoundIndex = roundIndex;
            WasRequirementEvaluated = wasRequirementEvaluated;
            PassedRequirement = passedRequirement;
            CurrentWorth = currentWorth;
            RequiredWorth = requiredWorth;
            Message = message;
        }

        public static RoundExecutionResult Evaluated(
            int roundIndex,
            float currentWorth,
            float requiredWorth,
            bool passed)
        {
            return new RoundExecutionResult(
                roundIndex,
                wasRequirementEvaluated: true,
                passedRequirement: passed,
                currentWorth: currentWorth,
                requiredWorth: requiredWorth,
                message: $"Round requirement check: current={currentWorth}, required={requiredWorth}, passed={passed}");
        }

        public static RoundExecutionResult NotEvaluated(
            int roundIndex,
            string message)
        {
            return new RoundExecutionResult(
                roundIndex,
                wasRequirementEvaluated: false,
                passedRequirement: false,
                currentWorth: 0f,
                requiredWorth: 0f,
                message: message);
        }

        public static RoundExecutionResult Skipped(
            int roundIndex,
            string message)
        {
            return NotEvaluated(roundIndex, message);
        }
    }
}
