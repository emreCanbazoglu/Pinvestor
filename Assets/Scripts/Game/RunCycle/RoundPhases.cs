using Cysharp.Threading.Tasks;

namespace Pinvestor.Game
{
    public sealed class TurnExecutionRoundPhase : RoundPhaseBase
    {
        public override string PhaseName => "TurnExecution";

        protected override UniTask ExecuteCoreAsync(
            RoundContext context,
            RoundRuntimeState runtimeState)
        {
            Turn turn = new Turn(context.CardPlayer, context.BallShooter);
            return turn.ExecuteCoreTurnAsync(runtimeState.RoundIndex, runtimeState.TurnIndex);
        }
    }

    public sealed class ShopPlaceholderRoundPhase : RoundPhaseBase
    {
        public override string PhaseName => "ShopPlaceholder";

        protected override UniTask ExecuteCoreAsync(
            RoundContext context,
            RoundRuntimeState runtimeState)
        {
            // Placeholder for future shop implementation.
            return UniTask.CompletedTask;
        }
    }
}

