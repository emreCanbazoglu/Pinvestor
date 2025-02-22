using UnityEngine;
using ENeighbor = MildMania.PuzzleLevelEditor.BoardExtensions.ENeighbor;

namespace Pinvestor.BoardSystem
{
    public class CellVisual : MonoBehaviour
    {
        [field: SerializeField] public Transform VisualParent { get; private set; } = null;
        
        [SerializeField] private TilePlacer_Cell _cellPlacer = null;

        public Cell Cell { get; private set; }

        public void Init(Cell cell)
        {
            Cell = cell;
            
            //_cellPlacer.UpdateTile(cell.Position, HasTile);
        }

        private bool HasTile(Vector2 cellPosition, ENeighbor neighbor)
        {
            if (!BoardManager.Instance.Board.TryGetCellAt(cellPosition, out Cell cell))
            {
                return false;
            }

            bool hasCell = cell.TryGetLinkedCell(neighbor, out Cell neighborCell);

            if (neighbor == ENeighbor.Down && cellPosition.y == 0)
            {
                return true;
            }

            return hasCell;
        }
    }
}