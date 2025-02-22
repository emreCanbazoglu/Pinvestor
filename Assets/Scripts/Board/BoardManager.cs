using UnityEngine;

namespace Pinvestor.BoardSystem
{
    public class BoardManager : Singleton<BoardManager>
    {
        [field: SerializeField] public Transform BoardTransform { get; private set; }
        [field: SerializeField] public Transform BoardItemTransform { get; private set; }

        public Board Board { get; private set; }
        
        public void Init(BoardData boardData, bool isReloading, bool createCells)
        {
            CreateBoard(boardData, isReloading, createCells);
        }
        
        private void CreateBoard(BoardData boardData, bool isReloading, bool createCells)
        {
            if (isReloading)
            {
                Board.Dispose();
            }
            else
            {
                Board = new Board();
            }
            
            Board.Init(boardData, isReloading, createCells);
        }

        protected override void OnDestroyCore()
        {
            Board?.Dispose();
            
            base.OnDestroyCore();
        }
    }
}