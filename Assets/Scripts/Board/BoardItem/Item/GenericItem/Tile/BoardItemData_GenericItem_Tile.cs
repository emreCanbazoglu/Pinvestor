using System;
using Boomlagoon.JSON;

namespace MildMania.PuzzleLevelEditor
{
    public class BoardItemData_GenericItem_Tile : BoardItemDataBase
    {
        public BoardItemData_GenericItem_Tile(JSONObject jsonObj) : base(jsonObj)
        {
        }

        public BoardItemData_GenericItem_Tile()
        {
        }
        
        public BoardItemData_GenericItem_Tile(int col, int row, int layerID = -1) : base(col, row, layerID)
        {
        }

        public BoardItemData_GenericItem_Tile(BoardItemData_GenericItem_Tile data) : base(data)
        {
        }
        
        public override int GetObjectiveCount()
        {
            return 0;
        }

        public override Enum GetItemID()
        {
            return EGenericBoardItemType.Tile;
        }
    }
}