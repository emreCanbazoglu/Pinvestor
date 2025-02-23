using System;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    [CreateAssetMenu(menuName = "Puzzle Game/Board Item/Logic/Type/Generic Board Item Type SO")]
    public class BoardItemTypeSO_GenericItem : BoardItemTypeSOBase
    {
        [SerializeField] private EGenericBoardItemType _itemType = default;

        public override Enum GetID()
        {
            return _itemType;
        }
    }
}