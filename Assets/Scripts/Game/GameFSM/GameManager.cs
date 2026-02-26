using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.CardSystem.Authoring;
using Pinvestor.GameConfigSystem;
using Pinvestor.Game.BallSystem;
using UnityEngine;
using UnityEngine.Serialization;

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
            
            Turn turn = new Turn(
                Table.GamePlayer.CardPlayer,
                BallShooter);

            while (true)
            {
                await turn.StartAsync();
            }
        }
    }
}
