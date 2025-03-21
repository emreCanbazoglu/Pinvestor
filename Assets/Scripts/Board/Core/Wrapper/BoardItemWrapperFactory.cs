using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemWrapperFactory : Singleton<BoardItemWrapperFactory>
    {
        [System.Serializable]
        public class Wrapper
        {
            [field: SerializeField] public BoardItemTypeSO BoardItemTypeSO { get; private set; } = null;
            [field: SerializeField] public BoardItemWrapperBase BoardItemWrapperBase { get; private set; } = null;
        }

        [SerializeField] private Wrapper[] _wrappers 
            = Array.Empty<Wrapper>();
        
        public bool TryGetWrapper(
            BoardItemTypeSO boardItemTypeSO, 
            out BoardItemWrapperBase boardItemWrapperBase)
        {
            foreach (Wrapper wrapper in _wrappers)
            {
                if (wrapper.BoardItemTypeSO == boardItemTypeSO)
                {
                    boardItemWrapperBase = wrapper.BoardItemWrapperBase;
                    return true;
                }
            }

            boardItemWrapperBase = null;
            return false;
        }
    }
}