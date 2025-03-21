using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemFactory : Singleton<BoardItemFactory>
    {
        [SerializeField] private BoardItemCreatorScriptableObjectBase[] _creators
            = Array.Empty<BoardItemCreatorScriptableObjectBase>();
        
        public BoardItemBase CreateBoardItem(
            BoardItemDataBase boardItemData,
            bool isPlaceHolder = false)
        {
            BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(
                boardItemData.GetBoardItemType().GetID(),
                out BoardItemInfoSO genericInfoSO);
            
            if (genericInfoSO is not BoardItemInfoSO infoSO)
                return null;
            
            if (!TryGetCreator(boardItemData, out var creator))
                return null;

            var boardItem
                = creator.CreateItem(
                    infoSO,
                    boardItemData);
            
            boardItem.Init(
                infoSO, 
                boardItemData,
                isPlaceHolder);

            boardItem.CreateItem();

            return boardItem;
        }

        private bool TryGetCreator(
            BoardItemDataBase boardItemData,
            out BoardItemCreatorScriptableObjectBase creator)
        {
            creator = null;
            
            var boardItemType
                = boardItemData.GetBoardItemType();

            foreach (var c in _creators)
            {
                if (c.IsValid(boardItemType))
                {
                    creator = c;
                    return true;
                }
            }

            return false;
        }
        
    }
}