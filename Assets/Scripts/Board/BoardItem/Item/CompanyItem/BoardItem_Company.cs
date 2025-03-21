using System;
using System.Collections.Generic;
using Pinvestor.CardSystem;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItem_Company : BoardItemBase
    {
        public BoardItemData_Company CompanyData => (BoardItemData_Company) BoardItemData;

        public CompanyCardDataScriptableObject CompanyCardDataSo { get; private set; }
        
        protected override void InitCore(
            BoardItemDataBase data)
        {
            CardFactory.Instance.CardContainer
                .TryGetCardData(
                    CompanyData.RefCardId,
                    out var cardDataSo);
            
            CompanyCardDataSo = cardDataSo as CompanyCardDataScriptableObject;
            
            base.InitCore(data);
        }

        protected override List<BoardItemPieceBase> CreatePieces(
            bool isPlaceholder = false)
        {
            List<BoardItemPieceBase> pieces = new List<BoardItemPieceBase>();
            
            BoardItemPiece_Company piece = new BoardItemPiece_Company(this);
            pieces.Add(piece);

            return pieces;
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