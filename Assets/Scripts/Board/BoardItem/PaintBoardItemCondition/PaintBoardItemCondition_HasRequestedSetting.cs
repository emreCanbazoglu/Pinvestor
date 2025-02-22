namespace MildMania.PuzzleLevelEditor
{
    public class PaintBoardItemCondition_HasRequestedSetting : PaintBoardItemConditionBase
    {
        protected override bool IsSatisfied(BoardItemBase boardItem, BoardItemDataBase data)
        {
            if (!boardItem.HasSetting())
            {
                return true;
            }

            return boardItem.TryGetSetting(data, out BoardItemSettingBase setting);
        }
    }
}