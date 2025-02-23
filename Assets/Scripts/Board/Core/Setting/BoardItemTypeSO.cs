using System;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    [CreateAssetMenu(menuName = "Pinvestor/Game/Board Item/Logic/Type/Board Item Type SO")]
    public class BoardItemTypeSO : BoardItemTypeSOBase
    {
        [SerializeField] private EBoardItem _boardItemType = default;
        
        private Enum _boardItemTypeEnum = null;
        private Enum _BoardItemTypeEnum
        {
            get
            {
                if (_boardItemTypeEnum == null)
                {
                    _boardItemTypeEnum = _boardItemType;
                }

                return _boardItemTypeEnum;
            }
        }
        
        public override Enum GetID()
        {
            return _BoardItemTypeEnum;
        }

        private void OnValidate()
        {
            _boardItemTypeEnum = _boardItemType;
        }
    }
}