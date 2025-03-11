using System.Collections.Generic;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardWrapper : MonoBehaviour
    {
        [SerializeField] private GameObject _cellPrefab = null;
        [SerializeField] private Transform _cellParent = null;

        [SerializeField] private Vector2 _cellSize = Vector2.one;
        [SerializeField] private Vector2 _cellVisualOffset = Vector2.zero;
        
        public Board Board { get; private set; }
        
        public Dictionary<Cell, CellWrapper> CellWrappers { get; private set; }
            = new Dictionary<Cell, CellWrapper>();
        
        public void WrapBoard(
            Board board)
        {
            Board = board;

            CreateCells();
        }

        private void CreateCells()
        {
            foreach (var kvp in Board.Cells)
            {
                Vector2Int coordinates = kvp.Key;
                Cell cell = kvp.Value;
                
                GameObject cellGO 
                    = Instantiate(
                        _cellPrefab,
                        _cellParent);
                
                cellGO.name = $"Cell {coordinates.x} {coordinates.y}";
                
                CellWrapper cellWrapper = cellGO.GetComponent<CellWrapper>();

                Vector3 cellPosition = GetCellPosition(coordinates);
                
                cellWrapper.VisualParent.position = cellPosition;
                    
                cellWrapper.Init(cell);
                
                CellWrappers.Add(cell, cellWrapper);
            }
        }
        
        public bool TryGetCellWrapper(
            Vector2Int coordinates,
            out CellWrapper cellWrapper)
        {
            cellWrapper = default;
            
            if (!Board.TryGetCellAt(coordinates, out Cell cell))
            {
                return false;
            }

            return CellWrappers.TryGetValue(cell, out cellWrapper);
        }
        
        public bool TryGetCellWrapper(
            Cell cell,
            out CellWrapper cellWrapper)
        {
            cellWrapper = default;
            
            return CellWrappers.TryGetValue(cell, out cellWrapper);
        }
        
        private Vector3 GetCellPosition(
            Vector2Int coordinates)
        {
            return new Vector3(
                coordinates.x * _cellSize.x + _cellVisualOffset.x,
                coordinates.y * _cellSize.y + _cellVisualOffset.y,
                0);
        }
    }
}
