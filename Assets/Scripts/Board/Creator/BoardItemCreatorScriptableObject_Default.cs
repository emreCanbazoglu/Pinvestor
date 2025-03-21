using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Game/Board Item/Logic/Creators/Default",
        fileName = "BoardItemCreator.Default.asset")]
    public class BoardItemCreatorScriptableObject_Default : BoardItemCreatorScriptableObjectBase
    {
        public override BoardItemBase CreateItem(
            BoardItemInfoSO infoSo,
            BoardItemDataBase boardItemData)
        {
            var boardItem 
                = (BoardItemBase) Activator.CreateInstance(
                    infoSo.BoardItemTypeRef);

            return boardItem;
        }
    }
}