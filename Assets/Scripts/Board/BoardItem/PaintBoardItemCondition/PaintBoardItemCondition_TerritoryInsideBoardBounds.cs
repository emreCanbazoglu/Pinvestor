using System.Collections.Generic;
using System.Linq;

namespace MildMania.PuzzleLevelEditor
{
    public class PaintBoardItemCondition_TerritoryInsideBoardBounds : PaintBoardItemConditionBase
    {
        protected override bool IsSatisfied(BoardItemBase boardItem, BoardItemDataBase data)
        {
            List<Cell> territoryCells = boardItem.TerritoryProvider.GetInsideTerritoryCells(data);

            return territoryCells.All(i => i != null);
        }
    }
}