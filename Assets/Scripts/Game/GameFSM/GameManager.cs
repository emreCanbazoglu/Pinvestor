using System;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.GameConfigSystem;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Economy;
using Pinvestor.Game.Offer;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Pinvestor.Game
{
    public class GameManager : Singleton<GameManager>
    {
        [field: SerializeField] public GameFSM GameFsm { get; private set; } = null;
        [field: SerializeField] public BoardWrapper BoardWrapper { get; private set; } = null;

        [field: SerializeField] public BallShooter BallShooter { get; private set; } = null;

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

        private const string BalanceAttributeName = "Balance";
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

            // Initialize the CardPlayer's Balance attribute to initialCapital.
            // This is the GAS single source of truth for net worth — no parallel economy state.
            InitializeBalanceAttribute(Table.GamePlayer.CardPlayer, initialCapital);

            // Build run-level economy services.
            _revenueAccumulator = new TurnRevenueAccumulator();
            _economyService = new EconomyService(_revenueAccumulator);

            // Subscribe to run outcome event.
            _runOutcomeEventBinding = new EventBinding<RunOutcomeEvent>(OnRunOutcome);
            EventBus<RunOutcomeEvent>.Register(_runOutcomeEventBinding);

            Debug.Log($"[GameManager] Economy initialized: initialCapital={initialCapital}");
        }

        private static void InitializeBalanceAttribute(CardPlayer cardPlayer, float initialCapital)
        {
            if (cardPlayer == null || cardPlayer.AbilitySystemCharacter == null)
            {
                Debug.LogWarning(
                    "[GameManager] CardPlayer or AbilitySystemCharacter is null. " +
                    "Cannot initialize Balance attribute.");
                return;
            }

            AttributeSystemComponent attributeSystem
                = cardPlayer.AbilitySystemCharacter.AttributeSystem;
            if (attributeSystem == null || attributeSystem.AttributeSet == null)
            {
                Debug.LogWarning(
                    "[GameManager] AttributeSystemComponent or AttributeSet is null. " +
                    "Cannot initialize Balance attribute.");
                return;
            }

            if (!attributeSystem.AttributeSet.TryGetAttributeByName(
                    BalanceAttributeName,
                    out AttributeScriptableObject balanceAttribute))
            {
                Debug.LogWarning(
                    $"[GameManager] Balance attribute '{BalanceAttributeName}' not found. " +
                    "Cannot initialize Balance attribute.");
                return;
            }

            attributeSystem.SetAttributeBaseValue(balanceAttribute, initialCapital);

            Debug.Log(
                $"[GameManager] Balance attribute initialized to {initialCapital}");
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
                Debug.Log("Configured run cycle completed. Restarting...");
            }
            else
            {
                Debug.LogWarning("Run cycle config is missing or empty in GameConfig. Skipping run cycle execution.");
                return;
            }

            // Brief pause so the player can see the final board state before restart.
            await UniTask.Delay(2000);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private async UniTask PlayConfiguredRunAsync(RoundCycleSettings[] rounds)
        {
            RunCompanyPool companyPool = BuildRunCompanyPool();

            RoundContext context = new RoundContext(
                Table.GamePlayer.CardPlayer,
                BallShooter,
                Table.Board,
                companyPool,
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

            // Clear the company pool on run end to prevent stale state in the next run (T022).
            companyPool.Clear();

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

        /// <summary>
        /// Builds and initializes the RunCompanyPool from the GameConfig company list.
        /// Called once at run start (T003, T021).
        /// </summary>
        private RunCompanyPool BuildRunCompanyPool()
        {
            var pool = new RunCompanyPool();

            GameConfigManager gameConfigManager = _gameConfigManager != null
                ? _gameConfigManager
                : GameConfigManager.Instance;

            if (gameConfigManager == null || !gameConfigManager.IsInitialized)
            {
                Debug.LogWarning("[GameManager] GameConfigManager not ready. RunCompanyPool will be empty.");
                return pool;
            }

            if (!gameConfigManager.TryGetService<CompanyConfigService>(out _))
            {
                Debug.LogWarning("[GameManager] CompanyConfigService not available. RunCompanyPool will be empty.");
                return pool;
            }

            // Collect all company IDs from the root model.
            if (gameConfigManager.RootModel?.Companies == null)
            {
                Debug.LogWarning("[GameManager] No companies in GameConfig root model.");
                return pool;
            }

            var companyIds = new System.Collections.Generic.List<string>();
            foreach (var company in gameConfigManager.RootModel.Companies)
            {
                if (!string.IsNullOrWhiteSpace(company.CompanyId))
                    companyIds.Add(company.CompanyId);
            }

            pool.Initialize(companyIds);
            return pool;
        }
    }
}
