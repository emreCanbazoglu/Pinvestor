using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.CardSystem.Authoring;
using Pinvestor.GameConfigSystem;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Economy;
using UnityEngine;
using System.Collections.Generic;

namespace Pinvestor.Game
{
    public class GameManager : Singleton<GameManager>
    {
        [field: SerializeField] public GameFSM GameFsm { get; private set; } = null;
        [field: SerializeField] public BoardWrapper BoardWrapper { get; private set; } = null;
        
        [field: SerializeField] public BallShooter BallShooter { get; private set; } = null;
        [field: SerializeField] public CompanySelectionPileWrapper CompanySelectionPileWrapper { get; private set; } = null;
        
        [field: SerializeField] public GamePlayer.GamePlayer GamePlayer { get; private set; }= null;
        [SerializeField] private GameConfigManager _gameConfigManager = null;

        [SerializeField] private SerializedDeckDataProvider _serializedDeckDataProvider = null;
        [SerializeField] private Vector2Int _boardSize = new Vector2Int(5, 5);
        [SerializeField] private CellLayerInfoSO[] _cellLayerInfoColl
            = Array.Empty<CellLayerInfoSO>();

        public Table Table { get; private set; }

        private TurnRevenueAccumulator _revenueAccumulator;
        private EconomyService _economyService;
        private EventBinding<RunOutcomeEvent> _runOutcomeEventBinding;

        private const string InitialCapitalConfigKey = "initialCapital";
        private const float DefaultInitialCapital = 500f;

        private void Awake()
        {
            InitializeAsync().Forget();
        }
        
        private async UniTask InitializeAsync()
        {
            await TryInitializeGameConfigAsync();

            Table = new Table(
                GetBoardData(),
                GamePlayer,
                _serializedDeckDataProvider,
                _cellLayerInfoColl);

            await Table.WaitUntilInitialized();

            BoardWrapper.WrapBoard(Table.Board);
            CompanySelectionPileWrapper.WrapPile(
                Table.GamePlayer.CardPlayer.Deck
                    .TryGetDeckPile(EDeckPile.CompanySelection, out var pile)
                    ? pile as CompanySelectionPile
                    : null);

            Debug.Log("Table initialized");

            InitializeEconomy();

            PlayAsync().Forget();
        }

        private void InitializeEconomy()
        {
            // Read initialCapital from the balance section of GameConfig.
            float initialCapital = DefaultInitialCapital;
            GameConfigManager configManager = _gameConfigManager != null
                ? _gameConfigManager
                : GameConfigManager.Instance;

            if (configManager != null && configManager.IsInitialized
                && configManager.TryGetService(out BalanceConfigService balanceService))
            {
                if (!balanceService.TryGetValue(InitialCapitalConfigKey, out initialCapital))
                {
                    Debug.LogWarning(
                        $"[GameManager] '{InitialCapitalConfigKey}' key not found in balance config. " +
                        $"Using default: {DefaultInitialCapital}");
                    initialCapital = DefaultInitialCapital;
                }
            }
            else
            {
                Debug.LogWarning(
                    "[GameManager] GameConfig not available for economy initialization. " +
                    $"Using default initialCapital={DefaultInitialCapital}");
            }

            // Initialize PlayerEconomyState (must be in the scene as a singleton MonoBehaviour).
            // Scene setup: add a PlayerEconomyState component to any persistent GameObject in the
            // game scene before GameManager.Awake() runs. Without it, economy will not function:
            // EconomyService.ApplyResolution() will log a warning and skip every turn, and
            // TryGetCurrentNetWorth() will fall back to the CardPlayer Balance attribute
            // (which does NOT include revenue calculated by EconomyService).
            if (PlayerEconomyState.Instance != null)
            {
                PlayerEconomyState.Instance.Initialize(initialCapital);
            }
            else
            {
                Debug.LogError(
                    "[GameManager] PlayerEconomyState singleton not found in the scene. " +
                    "Economy will not function: net worth will not update, win/loss evaluation " +
                    "will read the CardPlayer Balance attribute instead of EconomyService state. " +
                    "Fix: add a PlayerEconomyState MonoBehaviour to a GameObject in the game scene.");
            }

            // Build run-level economy services.
            _revenueAccumulator = new TurnRevenueAccumulator();
            _economyService = new EconomyService(
                _revenueAccumulator,
                configManager);

            // Subscribe to run outcome event.
            _runOutcomeEventBinding = new EventBinding<RunOutcomeEvent>(OnRunOutcome);
            EventBus<RunOutcomeEvent>.Register(_runOutcomeEventBinding);

            Debug.Log($"[GameManager] Economy initialized: initialCapital={initialCapital}");
        }

        private void OnRunOutcome(RunOutcomeEvent e)
        {
            string outcome = e.IsWin ? "WIN" : "LOSS";
            Debug.Log(
                $"[GameManager] Run outcome: {outcome} | " +
                $"finalNetWorth={e.FinalNetWorth} | targetNetWorth={e.TargetNetWorth}");

            // Deregister to avoid repeated handling if the event fires more than once.
            if (_runOutcomeEventBinding != null)
            {
                EventBus<RunOutcomeEvent>.Deregister(_runOutcomeEventBinding);
                _runOutcomeEventBinding = null;
            }
        }

        private void OnDestroy()
        {
            if (_runOutcomeEventBinding != null)
            {
                EventBus<RunOutcomeEvent>.Deregister(_runOutcomeEventBinding);
                _runOutcomeEventBinding = null;
            }
        }

        private async UniTask TryInitializeGameConfigAsync()
        {
            GameConfigManager gameConfigManager = _gameConfigManager != null
                ? _gameConfigManager
                : GameConfigManager.Instance;

            if (gameConfigManager == null)
            {
                Debug.LogWarning("GameConfigManager is not present in scene. Skipping GameConfig initialization.");
                return;
            }

            await gameConfigManager.InitializeAsync();
        }
        
        private BoardData GetBoardData()
        {
            return new BoardData(_boardSize);
        }

        private async UniTask PlayAsync()
        {
            Debug.Log("Playing...");

            if (TryGetRunCycleFromGameConfig(out RoundCycleSettings[] rounds))
            {
                await PlayConfiguredRunAsync(rounds);
                Debug.Log("Configured run cycle completed.");
            }
            else
            {
                Debug.LogWarning("Run cycle config is missing or empty in GameConfig. Skipping run cycle execution.");
            }
        }

        private async UniTask PlayConfiguredRunAsync(RoundCycleSettings[] rounds)
        {
            RoundContext context = new RoundContext(
                Table.GamePlayer.CardPlayer,
                BallShooter,
                Table.Board,
                _revenueAccumulator,
                _economyService);

            IReadOnlyList<IRoundPhase> phases = BuildRoundPhases();
            bool allEvaluatedRoundsPassed = true;
            int completedRoundCount = 0;
            int totalRoundCount = rounds.Length;

            for (int roundIndex = 0; roundIndex < totalRoundCount; roundIndex++)
            {
                RoundCycleSettings round = rounds[roundIndex];
                bool isFinalRound = (roundIndex == totalRoundCount - 1);
                Round roundRunner = new Round(roundIndex, round, phases);
                RoundExecutionResult result = await roundRunner.ExecuteAsync(context, isFinalRound);
                completedRoundCount++;
                if (result.WasRequirementEvaluated)
                {
                    Debug.Log(
                        $"Round Requirement Check: Round {roundIndex + 1}, CurrentWorth={result.CurrentWorth}, RequiredWorth={result.RequiredWorth}, Passed={result.PassedRequirement}");

                    if (!result.PassedRequirement)
                    {
                        allEvaluatedRoundsPassed = false;
                        Debug.LogWarning(
                            $"Run cycle stopped at round {roundIndex + 1}. Required worth check failed.");
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning($"Round Requirement Check Skipped: Round {roundIndex + 1}. {result.Message}");
                }
            }

            EventBus<RunCycleCompletedEvent>.Raise(
                new RunCycleCompletedEvent(
                    allEvaluatedRoundsPassed,
                    completedRoundCount,
                    totalRoundCount));
        }

        private bool TryGetRunCycleFromGameConfig(out RoundCycleSettings[] rounds)
        {
            rounds = Array.Empty<RoundCycleSettings>();

            GameConfigManager gameConfigManager = _gameConfigManager != null
                ? _gameConfigManager
                : GameConfigManager.Instance;

            if (gameConfigManager == null || !gameConfigManager.IsInitialized)
                return false;

            if (!gameConfigManager.TryGetService(out RunCycleConfigService runCycleService))
                return false;

            RunCycleConfigModel runCycleConfig = runCycleService.Config;
            if (runCycleConfig == null || runCycleConfig.Rounds == null || runCycleConfig.Rounds.Count == 0)
                return false;

            rounds = new RoundCycleSettings[runCycleConfig.Rounds.Count];
            for (int i = 0; i < runCycleConfig.Rounds.Count; i++)
            {
                RoundCycleConfigEntryModel source = runCycleConfig.Rounds[i];
                rounds[i] = new RoundCycleSettings(
                    source.RoundId,
                    source.TurnCount,
                    source.RequiredWorth);
            }

            return true;
        }

        private IReadOnlyList<IRoundPhase> BuildRoundPhases()
        {
            return new IRoundPhase[]
            {
                new TurnExecutionRoundPhase(),
                new ShopPlaceholderRoundPhase(),
            };
        }
    }
}
