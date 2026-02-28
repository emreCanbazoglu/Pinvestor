using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.CardSystem.Authoring;
using Pinvestor.GameConfigSystem;
using Pinvestor.Game.BallSystem;
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
        [SerializeField] private RunCycleSettings _runCycleSettings = null;
        
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
            CompanySelectionPileWrapper.WrapPile(
                Table.GamePlayer.CardPlayer.Deck
                    .TryGetDeckPile(EDeckPile.CompanySelection, out var pile)
                    ? pile as CompanySelectionPile
                    : null);
            
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

            RoundCycleSettings[] configuredRounds = GetConfiguredRunCycleRounds();
            if (configuredRounds != null && configuredRounds.Length > 0)
            {
                await PlayConfiguredRunAsync(configuredRounds);
                Debug.Log("Configured run cycle completed.");
            }
            else
            {
                Debug.LogWarning("No configured rounds found for run cycle. Skipping run cycle execution.");
            }
        }

        private async UniTask PlayConfiguredRunAsync(RoundCycleSettings[] rounds)
        {
            RoundContext context = new RoundContext(
                Table.GamePlayer.CardPlayer,
                BallShooter);

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

            EventBus<RunCycleCompletedEvent>.Raise(
                new RunCycleCompletedEvent(
                    allEvaluatedRoundsPassed,
                    completedRoundCount,
                    totalRoundCount));
        }

        private RoundCycleSettings[] GetConfiguredRunCycleRounds()
        {
            if (TryGetRunCycleFromGameConfig(out RoundCycleSettings[] configRounds))
                return configRounds;

            if (_runCycleSettings == null || _runCycleSettings.Rounds == null)
                return Array.Empty<RoundCycleSettings>();

            return _runCycleSettings.Rounds;
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
