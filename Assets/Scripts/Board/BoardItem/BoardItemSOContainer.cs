using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemSOContainer : Singleton<BoardItemSOContainer>
    {
        [SerializeField] private BoardItemSOBase[] _boardItemSOs = null;
        private BoardItemSOBase[] _BoardItemSOs
        {
            get
            {
                if (!_isInited)
                {
                    foreach (BoardItemSOBase boardItemSO in _boardItemSOs)
                    {
                        boardItemSO.InitSO();
                    }
                }

                _isInited = true;

                return _boardItemSOs;
            }
        }
        
        private bool _isInited = false;
        
        private List<BoardItemInfoSO> _boardItemInfoSOColl;
        private List<BoardItemInfoSO> _BoardItemInfoSOColl
        {
            get
            {
                if (_boardItemInfoSOColl == null)
                {
                    _boardItemInfoSOColl 
                        = new List<BoardItemInfoSO>();

                    foreach (BoardItemSOBase boardItemSO in _BoardItemSOs)
                    {
                        _boardItemInfoSOColl.AddRange(boardItemSO.BoardItemInfoSOColl);
                    }
                }

                return _boardItemInfoSOColl;
            }
        }

        private List<Type> _enumTypes;
        private List<Type> _EnumTypes
        {
            get
            {
                if (_enumTypes == null)
                {
                    _enumTypes = new List<Type>();

                    foreach (BoardItemSOBase boardItemSO in _BoardItemSOs)
                    {
                        _enumTypes.Add(boardItemSO.GetEnumType());
                    }
                }
                
                return _enumTypes;
            }
        }
        
        public bool TryGetBoardItemInfoSO<T>(T boardItemEnum, out BoardItemInfoSO infoSO)
            where T : Enum
        {
            infoSO = default;
            
            foreach (BoardItemSOBase boardItemSO in _BoardItemSOs)
            {
                if (boardItemSO.TryGetBoardItemInfoSO(boardItemEnum, out infoSO))
                {
                    return true;
                }
            }

            return false;
        }

        public List<BoardItemInfoSO> GetBoardItemInfoCollection()
        {
            return _BoardItemInfoSOColl;
        }

        public List<Type> GetValidEnumTypes()
        {
            return _EnumTypes;
        }

        public bool TryGetBoardItemType(string id, out BoardItemTypeSOBase boardItemTypeSO)
        {
            List<Type> enumTypes = GetValidEnumTypes();

            boardItemTypeSO = default;
            
            foreach (Type type in enumTypes)
            {
                bool enumParsed = Enum.TryParse(type, id, out object resultEnum);

                if (!enumParsed)
                {
                    continue;
                }
                
                bool result = TryGetBoardItemInfoSO((Enum)resultEnum, out BoardItemInfoSO infoSO);

                if (result)
                {
                    boardItemTypeSO = infoSO.BoardItemTypeSO;

                    return true;
                }
            }

            return false;
        }

        protected override void OnDestroyCore()
        {
            _isInited = false;

            _boardItemInfoSOColl = null;

            _enumTypes = null;
            
            base.OnDestroyCore();
        }
    }
}