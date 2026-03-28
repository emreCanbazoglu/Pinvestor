using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.GameConfigSystem;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Offer;
using UnityEngine;
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

            PlayAsync().Forget();
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
            RunCompanyPool companyPool = BuildRunCompanyPool();

            RoundContext context = new RoundContext(
                Table.GamePlayer.CardPlayer,
                BallShooter,
                Table.Board,
                companyPool);

            IReadOnlyList<IRoundPhase> phases = BuildRoundPhases();
            bool allEvaluatedRoundsPassed = true;
            int completedRoundCount = 0;
            int totalRoundCount = rounds.Length;

            for (int roundIndex = 0; roundIndex < totalRoundCount; roundIndex++)
            {
                RoundCycleSettings round = rounds[roundIndex];
                Round roundRunner = new Round(roundIndex, round, phases);
                RoundExecutionResult result = await roundRunner.ExecuteAsync(context);
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
