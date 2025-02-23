using System.Collections.Generic;
using MildMania.PuzzleLevelEditor;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemCreator_Default : IBoardItemCreator
    {
        public void CreateItems(
            List<BoardItemDataBase> boardItems,
            out List<BoardItemDataBase> filteredBoardItems)
        {
            foreach (BoardItemDataBase boardItemData in boardItems)
            {
                BoardItemDataBase clonedData = BoardItemDataFactory.CreateBoardItemData(boardItemData);
                
                BoardManager.Instance.Board.TryCreateNewBoardItem(clonedData, out BoardItemBase boardItem);
            }
            
            boardItems.Clear();

            filteredBoardItems = boardItems;
        }
    }
}