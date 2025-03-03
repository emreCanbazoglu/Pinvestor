using System.Collections.Generic;

namespace Pinvestor.BoardSystem.Base
{
    public interface IBoardItemCreator
    {
        public void CreateItems(
            Board board,
            List<BoardItemDataBase> boardItems,
            out List<BoardItemDataBase> filteredBoardItems);
    }
}