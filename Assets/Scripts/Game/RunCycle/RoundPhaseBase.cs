using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinvestor.Game
{
    public interface IRoundPhase
    {
        string PhaseName { get; }
        UniTask ExecuteAsync(RoundContext context, RoundRuntimeState runtimeState);
    }

    public abstract class RoundPhaseBase : IRoundPhase
    {
        public abstract string PhaseName { get; }

        public async UniTask ExecuteAsync(
            RoundContext context,
            RoundRuntimeState runtimeState)
        {
            Debug.Log($"Round Phase Start: {PhaseName} (Round {runtimeState.RoundIndex + 1}, Turn {runtimeState.TurnIndex + 1})");
            await ExecuteCoreAsync(context, runtimeState);
            Debug.Log($"Round Phase End: {PhaseName} (Round {runtimeState.RoundIndex + 1}, Turn {runtimeState.TurnIndex + 1})");
        }

        protected abstract UniTask ExecuteCoreAsync(
            RoundContext context,
            RoundRuntimeState runtimeState);
    }
}

