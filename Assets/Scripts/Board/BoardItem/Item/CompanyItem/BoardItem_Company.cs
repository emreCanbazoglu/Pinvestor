using System;
using System.Collections.Generic;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItem_Company : BoardItemBase
    {
        public BoardItemData_Company CompanyData => (BoardItemData_Company) BoardItemData;
        
        protected override List<BoardItemPieceBase> CreatePieces(
            bool isPlaceholder = false)
        {
            List<BoardItemPieceBase> pieces = new List<BoardItemPieceBase>();
            
            BoardItemPiece_Company piece = new BoardItemPiece_Company(this);
            pieces.Add(piece);

            return pieces;
        }

        protected override BoardItemVisualBase CreateVisual()
        {
            return null;
        }
    }
    
    public class BoardItemPiece_Company : BoardItemPieceBase
    {
        public BoardItemPiece_Company(BoardItemBase boardItem) 
            : base(boardItem)
        {
        }
    }
}