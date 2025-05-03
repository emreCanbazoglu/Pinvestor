using System;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem
{

    public abstract class BoardItemProperty_PlacableBase : BoardItemPropertySOBase
    {

    }

    public abstract class BoardItemPropertySpec_PlacableBase : BoardItemPropertySpecBase, IBoardStabilityChecker
    {
        private bool _isPlacing;

        public bool IsPlacing
        {
            get => _isPlacing;
            private set
            {
                _isPlacing = value;

                OnStabilityUpdated?.Invoke();
            }
        }

        public Action OnStabilityUpdated { get; set; }
        
        public Action<Cell> OnPlaced { get; set; }

        protected BoardItemPropertySpec_PlacableBase(
            BoardItemPropertySOBase propertySO,
            BoardItemBase owner) : base(propertySO, owner)
        {
        }

        public bool IsStable()
        {
            return !IsPlacing;
        }
        
        public bool CanPlace()
        {
            return !IsPlacing;
        }

        public bool TryPlace(
            Cell cell,
            bool force = false,
            Action onPlaced = null)
        {
            if(!force && !CanPlace())
                return false;
            
            IsPlacing = true;
            
            if (cell == null)
                return false;
            
            BoardItem.BoardItemData.Col = cell.Position.x;
            BoardItem.BoardItemData.Row = cell.Position.y;

            foreach (var piece in BoardItem.Pieces)
            {
                Vector2Int targetCellIndex 
                    = cell.Position + piece.LocalCoords;

                cell.Board.TryGetCellAt(targetCellIndex, out Cell pieceCell);
                
                pieceCell.TryAddBoardItemPiece(piece, force: true);
            }
            
            PlaceCore(cell, onPlacedCore);
            return true;

            void onPlacedCore()
            {
                IsPlacing = false;
                
                Debug.Log($"Placed {BoardItem.GetBoardItemType().GetID()} at {cell.Position}");
                
                onPlaced?.Invoke();
                
                OnPlaced?.Invoke(cell);
            }
        }
        
        public bool TryRemove(
            Action onRemoved = null)
        {
            //TODO: Implement remove logic

            return false;
        }
        

        protected virtual void PlaceCore(Cell cell, Action onPlaced = null) { }
        protected virtual void RemoveCore(Action onRemoved = null) { }
    }
}
