using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public class Test_Main : MonoBehaviour
    {
        [SerializeField] private GamePlayer.GamePlayer _gamePlayer = null;
        [SerializeField] private SerializedDeckDataProvider _serializedDeckDataProvider = null;

        [SerializeField] private Vector2Int _boardSize = new Vector2Int(5, 5);

        [SerializeField] private BoardWrapper _boardWrapper = null;

        [SerializeField] private CellLayerInfoSO[] _cellLayerInfoColl
            = Array.Empty<CellLayerInfoSO>();

        private Table _table;

        private void Awake()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            _table = new Table(
                GetBoardData(),
                _gamePlayer,
                _serializedDeckDataProvider,
                _cellLayerInfoColl);

            await _table.WaitUntilInitialized();

            _boardWrapper.WrapBoard(_table.Board);

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
                _table.GamePlayer.CardPlayer,
                null,
                _table.Board);

            await turn.StartAsync();
        }
    }
}
