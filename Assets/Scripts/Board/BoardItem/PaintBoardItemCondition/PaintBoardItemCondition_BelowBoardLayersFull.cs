using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public class PaintBoardItemCondition_BelowBoardLayersFull : PaintBoardItemConditionBase
    {
        [SerializeField] private bool _skipIfLoading = true;
        
        protected override bool IsSatisfied(
            BoardItemBase boardItem,
            BoardItemDataBase data)
        {
            if (LevelEditor.Instance.IsLevelLoading && _skipIfLoading)
            {
                return true;
            }
            
            for (int i = 0; i < data.Layer; i++)
            {
                BoardLayer layer = BoardEditor.Instance.Board.Layers[i];

                if (!layer
                    .Rows[data.Row]
                    .Cols[data.Col]
                    .CellController
                    .TryGetLayerToPlaceBoardItem(boardItem.BoardItemTypeSO, out CellLayer cellLayer))
                {
                    return false;
                }
                
                if (cellLayer.IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }
    }
}