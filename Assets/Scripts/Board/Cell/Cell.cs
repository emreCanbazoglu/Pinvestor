using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class Cell
    {
        public Board Board { get; private set; }
        public Vector2Int Position { get; private set; }
        public List<CellLayer> Layers { get; private set; }
        
        public CellLayer MainLayer { get; private set; }
        
        public Cell(
            Board board,
            int col,
            int row,
            List<CellLayer> layers)
        {
            Board = board;
            Position = new Vector2Int(col, row);
            Layers = layers;

            MainLayer = Layers.FirstOrDefault(i => i.IsMainLayer);
        }

        public bool TryGetTopmostFullLayer(out CellLayer cellLayer)
        {
            cellLayer = default;
            
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                cellLayer = Layers[i];
                            
                bool hasItem = cellLayer.IsFull();

                if (!hasItem)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public bool TryRemoveBoardItemPiece(BoardItemPieceBase boardItemPiece)
        {
            foreach (CellLayer layer in Layers)
            {
                if (layer.TryUnregisterBoardItemPiece(boardItemPiece))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAddBoardItemPiece(
            BoardItemPieceBase boardItemPiece, 
            bool force = false)
        {
            if (!force
                 && !CanAddBoardItem(boardItemPiece.ParentItem.GetBoardItemType()))
            {
                return false;
            }
            
            foreach (CellLayer layer in Layers)
            {
                if (layer.TryRegisterBoardItemPiece(boardItemPiece))
                {
                    boardItemPiece.SetCell(this);
                    
                    return true;
                }
            }

            return false;
        }

        public bool CanAddBoardItem(BoardItemTypeSO boardItemTypeSO)
        {
            foreach (CellLayer layer in Layers)
            {
                if (layer.CanRegisterBoardItem(boardItemTypeSO))
                {
                    return true;
                }
            }

            return false;
        }
        
        #region LinkedCells

        
        private bool _linkedCellsInited;
        private Cell _linkedCellUp;
        private Cell _linkedCellDown;
        private Cell _linkedCellUpperLeft;
        private Cell _linkedCellUpperRight;
        private Cell _linkedCellBottomLeft;
        private Cell _linkedCellBottomRight;
        private Cell _linkedCellLeft;
        private Cell _linkedCellRight;

        public bool IsLinkedCell(Cell cell)
        {
            InitLinkedCells();

            bool isLinkedCell = cell.Equals(_linkedCellUp) ||
                                cell.Equals(_linkedCellDown) ||
                                cell.Equals(_linkedCellUpperLeft) ||
                                cell.Equals(_linkedCellUpperRight) ||
                                cell.Equals(_linkedCellBottomLeft) ||
                                cell.Equals(_linkedCellBottomRight) ||
                                cell.Equals(_linkedCellLeft) ||
                                cell.Equals(_linkedCellRight);

            return isLinkedCell;
        }

        public bool TryGetLinkedCell(
            ENeighbor neighbor, out Cell cell)
        {
            InitLinkedCells();

            cell = null;

            switch (neighbor)
            {
                case ENeighbor.Down:
                    cell = _linkedCellDown;
                    break;
                case ENeighbor.Up:
                    cell = _linkedCellUp;
                    break;
                case ENeighbor.Down_Left:
                    cell = _linkedCellBottomLeft;
                    break;
                case ENeighbor.Down_Right:
                    cell = _linkedCellBottomRight;
                    break;
                case ENeighbor.Up_Left:
                    cell = _linkedCellUpperLeft;
                    break;
                case ENeighbor.Up_Right:
                    cell = _linkedCellUpperRight;
                    break;
                case ENeighbor.Left:
                    cell = _linkedCellLeft;
                    break;
                case ENeighbor.Right:
                    cell = _linkedCellRight;
                    break;
            }

            return cell is not null;
        }

        private void InitLinkedCells()
        {
            if(_linkedCellsInited)
                return;

            Board.TryGetCellAt(Position + Vector2Int.up, out _linkedCellUp);
            Board.TryGetCellAt(Position + Vector2Int.down, out _linkedCellDown);
            Board.TryGetCellAt(Position + Vector2Int.left, out _linkedCellLeft);
            Board.TryGetCellAt(Position + Vector2Int.right, out _linkedCellRight);
            Board.TryGetCellAt(Position + Vector2Int.up + Vector2Int.right, out _linkedCellUpperRight);
            Board.TryGetCellAt(Position + Vector2Int.up + Vector2Int.left, out _linkedCellUpperLeft);
            Board.TryGetCellAt(Position + Vector2Int.left + Vector2Int.down, out _linkedCellBottomLeft);
            Board.TryGetCellAt(Position + Vector2Int.right + Vector2Int.down, out _linkedCellBottomRight);

            _linkedCellsInited = true;
        }
        
        #endregion
    }
}