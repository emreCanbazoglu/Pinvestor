using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    public class Cell
    {
        public int Col { get; private set; }
        public int Row { get; private set; }
        public Vector2Int Position { get; private set; }
        public List<CellLayer> Layers { get; private set; }
        
        public CellLayer MainLayer { get; private set; }

        public CellVisual Visual { get; private set; }
        
        public Cell(
            int col,
            int row,
            List<CellLayer> layers)
        {
            Col = col;
            Row = row;
            Position = new Vector2Int(col, row);
            Layers = layers;

            MainLayer = Layers.FirstOrDefault(i => i.IsMainLayer);
        }

        public void InitVisual(CellVisual visual)
        {
            Visual = visual;
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
        
        public bool IsDropBlocked()
        {
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                CellLayer cellLayer = Layers[i];
                            
                bool hasItem = cellLayer.IsFull();

                if (!hasItem)
                {
                    continue;
                }

                if (cellLayer.BlocksDrop)
                {
                    return true;
                }
            }

            return false;
        }
        
        public bool IsMatchBlocked()
        {
            for (int i = Layers.Count - 1; i >= 0; i--)
            {
                CellLayer cellLayer = Layers[i];
                            
                bool hasItem = cellLayer.IsFull();

                if (!hasItem)
                {
                    continue;
                }

                if (cellLayer.BlocksMatch)
                {
                    return true;
                }
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

        public bool TryAddBoardItemPiece(BoardItemPieceBase boardItemPiece)
        {
            //TODO: Optimize
            if (!CanAddBoardItem(boardItemPiece.ParentItem.GetBoardItemType()))
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

        public bool TryGetLinkedCell(PuzzleLevelEditor.BoardExtensions.ENeighbor neighbor, out Cell cell)
        {
            InitLinkedCells();

            cell = null;

            switch (neighbor)
            {
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Down:
                    cell = _linkedCellDown;
                    break;
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Up:
                    cell = _linkedCellUp;
                    break;
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Down_Left:
                    cell = _linkedCellBottomLeft;
                    break;
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Down_Right:
                    cell = _linkedCellBottomRight;
                    break;
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Up_Left:
                    cell = _linkedCellUpperLeft;
                    break;
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Up_Right:
                    cell = _linkedCellUpperRight;
                    break;
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Left:
                    cell = _linkedCellLeft;
                    break;
                case PuzzleLevelEditor.BoardExtensions.ENeighbor.Right:
                    cell = _linkedCellRight;
                    break;
            }

            return cell is not null;
        }

        private void InitLinkedCells()
        {
            if(_linkedCellsInited)
                return;

            Board board = BoardManager.Instance.Board;
            
            board.TryGetCellAt(Position + Vector2Int.up, out _linkedCellUp);
            board.TryGetCellAt(Position + Vector2Int.down, out _linkedCellDown);
            board.TryGetCellAt(Position + Vector2.left, out _linkedCellLeft);
            board.TryGetCellAt(Position + Vector2.right, out _linkedCellRight);
            board.TryGetCellAt(Position + Vector2Int.up + Vector2.right, out _linkedCellUpperRight);
            board.TryGetCellAt(Position + Vector2Int.up + Vector2.left, out _linkedCellUpperLeft);
            board.TryGetCellAt(Position + Vector2.left + Vector2.down, out _linkedCellBottomLeft);
            board.TryGetCellAt(Position + Vector2.right + Vector2.down, out _linkedCellBottomRight);

            _linkedCellsInited = true;
        }
        
        #endregion
    }
}