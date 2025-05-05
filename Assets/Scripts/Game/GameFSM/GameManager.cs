using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using Pinvestor.CardSystem.Authoring;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public class GameManager : Singleton<GameManager>
    {
        [field: SerializeField] public GameFSM GameFsm { get; private set; } = null;
        [field: SerializeField] public BoardWrapper BoardWrapper { get; private set; } = null;
        
        [field: SerializeField] public BallShooter BallShooter { get; private set; } = null;
        [field: SerializeField] public CompanySelectionPileWrapper CompanySelectionPileWrapper { get; private set; } = null;
        
        [SerializeField] private GamePlayer.GamePlayer _gamePlayer = null;
        
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
            Table = new Table(
                GetBoardData(),
                _gamePlayer,
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
