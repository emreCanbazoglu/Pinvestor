using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public class EraseBoardItemCondition_AboveBoardLayersEmpty : EraseBoardItemConditionBase
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

            CellActionController.EAction currentCellAction = CellActionController.Instance.CurrentAction;

            if (currentCellAction == CellActionController.EAction.None)
            {
                return true;
            }
            
            int totalLayerCount = BoardEditor.Instance.Board.Layers.Count;
            
            for (int i = data.Layer + 1; i < totalLayerCount; i++)
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
                
                if (!cellLayer.IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }
    }
}