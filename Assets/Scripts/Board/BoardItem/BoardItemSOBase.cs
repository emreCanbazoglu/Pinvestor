using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemSOBase : ScriptableObject
    {
        [SerializeField] protected BoardItemInfoSO[] _boardItemInfoSOColl = default;
        public BoardItemInfoSO[] BoardItemInfoSOColl => _boardItemInfoSOColl;
        
        public abstract bool TryGetBoardItemInfoSO(Enum boardItemEnum, out BoardItemInfoSO boardItemInfo);

        public abstract void InitSO();

        public abstract Type GetEnumType();
    }
    
    public abstract class BoardItemSOBase<T> : BoardItemSOBase
        where T : Enum
    {
        private readonly Dictionary<T, BoardItemInfoSO> _boardItemInfoMapping = new();

        public sealed override Type GetEnumType()
        {
            return typeof(T);
        }

        public sealed override void InitSO()
        {
            foreach (BoardItemInfoSO boardItemInfoSO in _boardItemInfoSOColl)
            {
                _boardItemInfoMapping[(T)boardItemInfoSO.BoardItemTypeSO.GetID()] = boardItemInfoSO;
            }
        }
        
        public sealed override bool TryGetBoardItemInfoSO(Enum boardItemEnum,
            out BoardItemInfoSO boardItemInfoSO)
        {
            boardItemInfoSO = default;
            
            if (boardItemEnum.GetType() != typeof(T))
            {
                return false;
            }

            return _boardItemInfoMapping.TryGetValue((T)boardItemEnum, out boardItemInfoSO);
        }
    }
}