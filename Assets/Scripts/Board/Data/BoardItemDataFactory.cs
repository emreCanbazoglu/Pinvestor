using System;
using System.Collections.Generic;
using UnityEngine;

namespace MildMania.PuzzleLevelEditor
{
    public static class BoardItemDataFactory
    {
        public static BoardItemDataBase CreateBoardItemData(Enum boardItemType, params object[] args)
        {
            bool result = BoardItemSOContainer.Instance.TryGetBoardItemType(
                boardItemType.ToString(),
                out BoardItemTypeSOBase boardItemTypeSO);

            return CreateBoardItemData(boardItemTypeSO, args);
        }
        
        public static BoardItemDataBase CreateBoardItemData(BoardItemTypeSOBase itemTypeSO, params object[] args)
        {
            bool result = BoardItemSOContainer.Instance.TryGetBoardItemInfoSO(
                itemTypeSO.GetID(), out BoardItemInfoSO info);
            
            if (!result)
            {
                Debug.LogError("BoardItemDataFactory: Couldn't create board item data: " + itemTypeSO.GetID());
                
                return null;
            }
            
            BoardItemDataBase itemData 
                = (BoardItemDataBase) Activator.CreateInstance(info.BoardItemDataTypeRef.Type, args);
            
            if(itemData is IBaseBoardItemData baseBoardItemData)
                baseBoardItemData.SetID(itemTypeSO.GetID());
        
            return itemData;
        }

        public static List<BoardItemInfoSO> GetBoardItemDataMap()
        {
            return BoardItemSOContainer.Instance.GetBoardItemInfoCollection();
        }

        public static BoardItemDataBase CreateBoardItemData(BoardItemDataBase itemData)
        {
            BoardItemDataBase newItemData 
                = (BoardItemDataBase) Activator.CreateInstance(itemData.GetType(), itemData);
            
            if(newItemData is IBaseBoardItemData baseBoardItemData)
                baseBoardItemData.SetID(itemData.GetItemID());

            return newItemData;
        }
    }
}
