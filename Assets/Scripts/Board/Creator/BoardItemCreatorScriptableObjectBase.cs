using System;
using System.Linq;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemCreatorScriptableObjectBase : ScriptableObject
    {
        [field: SerializeField] protected BoardItemTypeSO[] ValidBoardItemTypes
            = Array.Empty<BoardItemTypeSO>();

        public abstract BoardItemBase CreateItem(
            BoardItemInfoSO infoSo,
            BoardItemDataBase boardItemData);

        public bool IsValid(BoardItemTypeSOBase boardItemType)
        {
            return ValidBoardItemTypes.Contains(boardItemType);
        }
    }
}