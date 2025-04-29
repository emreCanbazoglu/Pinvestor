using System.Collections.Generic;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardHighlighter : MonoBehaviour
    {
        [SerializeField] private BoardWrapper _boardWrapper = null;
        
        [SerializeField] private Color _highlightColor = Color.yellow;

        public void HighlightCell(
            Vector2Int coordinates)
        {
            if (!_boardWrapper.TryGetCellWrapper(
                    coordinates, 
                    out CellWrapper cellWrapper))
                return;

            cellWrapper.Highlight(_highlightColor);
        }
        
        public void HighlightCell(
            Cell cell)
        {
            if (!_boardWrapper.TryGetCellWrapper(cell, out CellWrapper cellWrapper))
                return;

            cellWrapper.Highlight(_highlightColor);
        }
        
        public void HighlightCells(
            Vector2Int[] coords)
        {
            foreach (Vector2Int coordinates in coords)
                HighlightCell(coordinates);
        }
        
        public void HighlightCells(
            List<Cell> cells)
        {
            foreach (Cell cell in cells)
            {
                if (!_boardWrapper.CellWrappers.TryGetValue(
                        cell, out CellWrapper cellWrapper))
                    continue;

                cellWrapper.Highlight(_highlightColor);
            }
        }
        
        public void ClearHighlights()
        {
            foreach (var kvp in _boardWrapper.CellWrappers)
                kvp.Value.ClearHighlight();
        }
    }
}