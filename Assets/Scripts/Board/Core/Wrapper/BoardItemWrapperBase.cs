using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemWrapperBase<T> : BoardItemWrapperBase where T : BoardItemBase
    {
        public new T BoardItem => base.BoardItem as T;
    }
    
    public abstract class BoardItemWrapperBase : MonoBehaviour
    {
        [field: SerializeField] public Transform VisualContainer { get; private set; } = null;
        
        [SerializeField] private BoardItemTypeSO _boardItemTypeSO = null;
        
        public BoardItemBase BoardItem { get; private set; }
        
        public Action OnInited { get; set; }

        public bool IsInited { get; private set; } = false;

        protected virtual void WrapCore()
        {
            
        }
        
        protected virtual void DisposeCore()
        {
            
        }
        
        public BoardItemTypeSO GetBoardItemTypeSO()
        {
            return _boardItemTypeSO;
        }

        public void Wrap(BoardItemBase boardItem)
        {
            BoardItem = boardItem;

            WrapCore();

            IsInited = true;
            
            OnInited?.Invoke();
        }

        private void Dispose()
        {
            IsInited = false;
            
            DisposeCore();
        }

        private void OnDisable()
        {
            //Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}