using System.Collections.Generic;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    [CreateAssetMenu(menuName = "Puzzle Game/Cell/Logic/Paint Layer Condition/Layer Available Condition SO")]
    public class PaintLayerConditionSO_LayerAvailable : PaintLayerConditionSOBase
    {
        protected override bool IsSatisfied(CellLayer layer, BoardItemDataBase boardItemData)
        {
            layer.TryGetBoardItemPrefab(boardItemData.GetBoardItemType(), out BoardItemBase boardItem);
            
            List<Cell> cells = boardItem.TerritoryProvider.GetInsideTerritoryCells(boardItemData);

            foreach (Cell cell in cells)
            {
                if (BoardEditor.Instance.Board.IsCellInsideAnyBoardItemsTerritory(
                    cell,
                    boardItemData.GetBoardItemType(),
                    out _))
                {
                    return false;
                }
            }

            return true;
        }
    }
}