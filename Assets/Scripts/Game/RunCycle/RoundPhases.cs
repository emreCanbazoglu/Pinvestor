using Cysharp.Threading.Tasks;
using Pinvestor.GameConfigSystem;

namespace Pinvestor.Game
{
    public sealed class TurnExecutionRoundPhase : RoundPhaseBase
    {
        public override string PhaseName => "TurnExecution";

        protected override UniTask ExecuteCoreAsync(
            RoundContext context,
            RoundRuntimeState runtimeState)
        {
            // Resolve CompanyConfigService for the offer drawer (may be null if GameConfig not loaded).
            CompanyConfigService companyConfigService = null;
            GameConfigManager gameConfigManager = GameConfigManager.Instance;
            if (gameConfigManager != null && gameConfigManager.IsInitialized)
                gameConfigManager.TryGetService(out companyConfigService);

            Turn turn = new Turn(
                context.CardPlayer,
                context.BallShooter,
                context.Board,
                context.CompanyPool,
                companyConfigService);

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
