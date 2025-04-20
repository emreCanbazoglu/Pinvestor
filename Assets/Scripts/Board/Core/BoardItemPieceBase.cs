using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemPieceBase : IDisposable
    {
        public BoardItemBase ParentItem { get; protected set; }
        public Vector2Int LocalCoords { get; private set; }

        public Cell Cell { get; private set; }
        
        public Action<BoardItemPieceBase> OnCellUpdated { get; set; }

        protected virtual void DisposeCore()
        {
            
        }
        
        public BoardItemPieceBase(BoardItemBase parentItem)
        {
            ParentItem = parentItem;
        }

        public void SetCell(Cell cell)
        {
            Cell = cell;

            OnCellUpdated?.Invoke(this);
        }

        public void Dispose()
        {
            DisposeCore();

            if (Cell != null)
            {
                Cell.TryRemoveBoardItemPiece(this);
            }
        }
    }
}