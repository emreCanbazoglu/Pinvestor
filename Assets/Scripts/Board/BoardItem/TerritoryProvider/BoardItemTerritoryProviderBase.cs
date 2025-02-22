using System.Collections.Generic;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public abstract class BoardItemTerritoryProviderBase : MonoBehaviour
    {
        private BoardItemBase _boardItem;

        public BoardItemBase BoardItem
        {
            get
            {
                if (_boardItem == null)
                {
                    _boardItem = GetComponent<BoardItemBase>();
                }

                return _boardItem;
            }
        }
        
        public abstract List<Vector2Int> GetTerritory(BoardItemDataBase boardItemData);

        public List<Cell> GetInsideTerritoryCells(BoardItemDataBase boardItemData)
        {
            List<Cell> cells = new List<Cell>();
        
            List<Vector2Int> territory = GetTerritory(boardItemData);
            
            foreach (Vector2Int cellPosition in territory)
            {
                BoardEditor.Instance.Board.TryGetCell(cellPosition, out Cell cell, boardItemData.Layer);
                
                cells.Add(cell);
            }

            return cells;
        }

        public List<Cell> GetInsideTerritoryCells()
        {
            return GetInsideTerritoryCells(BoardItem.GetGenericData());
        }

        public bool IsInsideTerritory(Cell cell)
        {
            List<Cell> cells = GetInsideTerritoryCells();
            
            return cells.Contains(cell);
        }

        public Cell GetLeftUpCorner(BoardItemDataBase boardItemData)
        {
            List<Cell> territory = GetInsideTerritoryCells(boardItemData);

            Cell cornerCell = territory[0];

            foreach (Cell cell in territory)
            {
                if (cell.Col <= cornerCell.Col && 
                    cell.Row >= cornerCell.Row)
                {
                    cornerCell = cell;
                }
            }
            
            return cornerCell;
        }
        
        public Cell GetRightDownCorner(BoardItemDataBase boardItemData)
        {
            List<Cell> territory = GetInsideTerritoryCells(boardItemData);

            Cell cornerCell = territory[0];

            foreach (Cell cell in territory)
            {
                if (cell.Col >= cornerCell.Col && 
                    cell.Row <= cornerCell.Row)
                {
                    cornerCell = cell;
                }
            }
            
            return cornerCell;
        }
        
        public Cell GetRightUpCorner(BoardItemDataBase boardItemData)
        {
            List<Cell> territory = GetInsideTerritoryCells(boardItemData);

            Cell cornerCell = territory[0];

            foreach (Cell cell in territory)
            {
                if (cell.Col >= cornerCell.Col && 
                    cell.Row >= cornerCell.Row)
                {
                    cornerCell = cell;
                }
            }
            
            return cornerCell;
        }
        
        public Cell GetLeftDownCorner(BoardItemDataBase boardItemData)
        {
            List<Cell> territory = GetInsideTerritoryCells(boardItemData);

            Cell cornerCell = territory[0];

            foreach (Cell cell in territory)
            {
                if (cell.Col <= cornerCell.Col && 
                    cell.Row <= cornerCell.Row)
                {
                    cornerCell = cell;
                }
            }
            
            return cornerCell;
        }
    }
}