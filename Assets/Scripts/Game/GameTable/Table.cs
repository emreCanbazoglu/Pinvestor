using System;
using Cysharp.Threading.Tasks;
using Pinvestor.BoardSystem;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.Game
{
    public class Table : IDisposable
    {
        public Board Board { get; private set; }
        public GamePlayer.GamePlayer GamePlayer { get; private set; }
        
        public Table(
            BoardData boardData,
            GamePlayer.GamePlayer gamePlayer,
            IDeckDataProvider deckDataProvider,
            CellLayerInfoSO[] cellLayerInfoColl)
        {
            GamePlayer = gamePlayer;
            
            CreateBoard(boardData, cellLayerInfoColl);
            
            GamePlayer.Initialize(deckDataProvider).Forget();
        }

        private void CreateBoard(
            BoardData boardData,
            CellLayerInfoSO[] cellLayerInfoColl)
        {
            Board = new Board(
                BoardItemSOContainer.Instance,
                BoardItemWrapperPoolManager.Instance,
                new IBoardItemCreator[]
                {
                    new BoardItemCreator_Default()
                },
                cellLayerInfoColl);
            
            Board.Init(boardData, createCells: true);
        }

        public async UniTask WaitUntilInitialized()
        {
            await GamePlayer.WaitUntilInitialized();
        }

        public void Dispose()
        {
            Board?.Dispose();
        }
    }
}
