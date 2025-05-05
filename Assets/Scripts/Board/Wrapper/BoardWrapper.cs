using System.Collections.Generic;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardWrapper : MonoBehaviour
    {
        [field: SerializeField] public BoardHighlighter Highlighter { get; private set; } = null;
        
        [SerializeField] private Vector3 _centerPosition = Vector3.zero;
        
        [SerializeField] private GameObject _cellPrefab = null;
        [SerializeField] private Transform _cellParent = null;

        [SerializeField] private Vector2 _cellSize = Vector2.one;
        [SerializeField] private Vector2 _cellVisualOffset = Vector2.zero;

        [SerializeField] private Transform _boardEnterTransform = null;
        
        public Board Board { get; private set; }
        
        public Bounds Bounds { get; private set; }
        private BoxCollider[] _colliders;

        public BoxCollider BoardEnterCollider => _colliders[3];
        
        public Dictionary<Cell, CellWrapper> CellWrappers { get; private set; }
            = new Dictionary<Cell, CellWrapper>();
        
        public void WrapBoard(
            Board board)
        {
            Board = board;

            CreateCells();
            
            CalculateBounds();
            
            CreateBoardCollider();
        }

        private void CreateCells()
        {
            Debug.Log("Create Cells: " + Board.Cells.Count);
            
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

                Vector3 cellPosition 
                    = GetCellPosition(
                        Board.Dimensions,
                        coordinates);
                
                cellWrapper.VisualParent.position = cellPosition;
                    
                cellWrapper.Init(cell);
                
                CellWrappers.Add(cell, cellWrapper);
            }
        }
        
        private void CalculateBounds()
        {
            Bounds = new Bounds(
                _centerPosition,
                new Vector3(
                    Board.Dimensions.x * _cellSize.x,
                    Board.Dimensions.y * _cellSize.y,
                    0));
        }
        
        private void CreateBoardCollider()
        {
            //Create bounding box colliders for left, top, right and bottom
            _colliders = new BoxCollider[4];
            
            //Create left collider
            _colliders[0] = gameObject.AddComponent<BoxCollider>();
            _colliders[0].size = new Vector3(
                _cellSize.x,
                Bounds.size.y,
                1);
            
            _colliders[0].center = new Vector3(
                Bounds.min.x - _cellSize.x / 2,
                Bounds.center.y,
                Bounds.center.z);
            
            _colliders[0].isTrigger = true;
            
            //Create top collider
            _colliders[1] = gameObject.AddComponent<BoxCollider>();
            _colliders[1].size = new Vector3(
                Bounds.size.x,
                _cellSize.y,
                1);
            
            _colliders[1].center = new Vector3(
                Bounds.center.x,
                Bounds.max.y + _cellSize.y / 2,
                Bounds.center.z);
            
            _colliders[1].isTrigger = true;
            
            //Create right collider
            _colliders[2] = gameObject.AddComponent<BoxCollider>();
            _colliders[2].size = new Vector3(
                _cellSize.x,
                Bounds.size.y,
                1);

            _colliders[2].center = new Vector3(
                Bounds.max.x + _cellSize.x / 2,
                Bounds.center.y,
                Bounds.center.z);
            
            _colliders[2].isTrigger = true;
            
            //Create bottom collider
            _colliders[3] = _boardEnterTransform.gameObject.AddComponent<BoxCollider>();
            _colliders[3].size = new Vector3(
                Bounds.size.x,
                _cellSize.y,
                1);
            
            _colliders[3].center = new Vector3(
                Bounds.center.x,
                Bounds.min.y - _cellSize.y / 2,
                Bounds.center.z);
            
            _colliders[3].isTrigger = true;
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
            Vector2Int dimensions,
            Vector2Int coordinates)
        {
            Vector3 leftBottomPosition
                = _centerPosition
                  - new Vector3(
                      dimensions.x * _cellSize.x / 2,
                      dimensions.y * _cellSize.y / 2,
                      0);

            Vector3 cellPosition
                = leftBottomPosition
                  + new Vector3(
                      coordinates.x * _cellSize.x + _cellSize.x / 2,
                      coordinates.y * _cellSize.y + _cellSize.y / 2,
                      0);
            
            return cellPosition;
        }

        public bool TryGetCellAt(
            Vector3 worldPosition,
            out Cell cell)
        {
            cell = null;
            
            float xDist = worldPosition.x - Bounds.min.x;

            if (xDist < 0)
                return false;

            int xIndex = (int)(xDist / _cellSize.x);

            if (xIndex < 0 || xIndex >= Board.Dimensions.x)
                return false;

            float yDist = worldPosition.y - Bounds.min.y;

            if (yDist < 0)
                return false;

            int yIndex = (int)(yDist / _cellSize.y);

            if (yIndex < 0 || yIndex >= Board.Dimensions.y)
                return false;

            return Board.TryGetCellAt(
                new Vector2Int(xIndex, yIndex),
                out cell);
        }

        private void OnDrawGizmos()
        {
            if(Board == null)
                return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(
                Bounds.center,
                Bounds.size);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                _centerPosition,
                new Vector3(
                    Board.Dimensions.x * _cellSize.x,
                    Board.Dimensions.y * _cellSize.y));
        }
    }
}
