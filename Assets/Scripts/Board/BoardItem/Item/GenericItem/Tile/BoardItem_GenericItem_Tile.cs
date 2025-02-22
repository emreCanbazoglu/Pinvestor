using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MildMania.PuzzleLevelEditor
{
    public class BoardItem_GenericItem_Tile : BoardItemBase<BoardItemData_GenericItem_Tile, BoardItemSetting_Simple>
    {
        [SerializeField] private TilePlacer_LevelEditor _editorTilePlacer = null;

        protected override void PaintCustomActions(BoardItemData_GenericItem_Tile boardItemDataBase)
        {
            UpdateCell();

            UpdateNeighborCells();
        }
        
        protected override void EraseCustomActions()
        {
            UpdateCell();

            UpdateNeighborCells();
        }
        
        private bool DoesNeighborHaveBoardTile(Vector2 cellPosition, BoardExtensions.ENeighbor neighbor)
        {
            int layerID = 0;
            
            if (ParentCell)
            {
                layerID = ParentCell.ParentRow.LayerID;
            }
            
            Dictionary<BoardExtensions.ENeighbor, Cell> neighbors = 
                BoardEditor.Instance.Board.GetNeighborCells(cellPosition, layerID);

            Cell neighborCell = neighbors[neighbor];

            if (!neighborCell)
            {
                return false;
            }

            return DoesCellHaveBoardTile(neighborCell);
        }

        private bool DoesCellHaveBoardTile(Cell cell)
        {
            BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(
                EGenericBoardItemType.Tile, out BoardItemInfoSO info);
            
            return cell && cell.CellController.IsPainted(info.BoardItemTypeSO);
        }

        public void UpdateCell()
        {
            if (!ParentCell)
            {
                _editorTilePlacer.UpdateTile(new Vector2(-1, -1), DoesNeighborHaveBoardTile);

                return;
            }
            
            _editorTilePlacer.UpdateTile(new Vector2(ParentCell.Col, ParentCell.Row), DoesNeighborHaveBoardTile);
        }

        private void UpdateNeighborCells()
        {
            if (!ParentCell)
            {
                return;
            }
            
            Dictionary<BoardExtensions.ENeighbor, Cell> neighbors = 
                BoardEditor.Instance.Board.GetNeighborCells(new Vector2(ParentCell.Col, ParentCell.Row), ParentCell.ParentRow.LayerID);
            
            foreach (KeyValuePair<BoardExtensions.ENeighbor, Cell> kvp in neighbors)
            {
                if (!DoesCellHaveBoardTile(kvp.Value))
                {
                    continue;
                }

                BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(
                    EGenericBoardItemType.Tile, out BoardItemInfoSO info);
                
                kvp.Value.CellController.TryGetBoardItem(
                    info.BoardItemTypeSO,
                    out BoardItemBase boardItem);
                
                ((BoardItem_GenericItem_Tile) boardItem).UpdateCell();
            } 
        }
    }
}