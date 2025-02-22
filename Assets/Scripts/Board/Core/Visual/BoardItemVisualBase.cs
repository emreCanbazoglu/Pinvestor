using System;
using UnityEngine;

namespace Pinvestor.BoardSystem
{

    public abstract class BoardItemVisualBase : MonoBehaviour
    {
        [field: SerializeField] public Transform VisualContainer { get; private set; } = null;
        
        [SerializeField] private BoardItemTypeSO _boardItemTypeSO = null;
        
        public BoardItemBase BoardItem { get; private set; }
        
        public Action OnInited { get; set; }

        public bool IsInited { get; private set; } = false;

        protected virtual void InitCore()
        {
            
        }
        
        protected virtual void DisposeCore()
        {
            
        }
        
        public BoardItemTypeSO GetBoardItemTypeSO()
        {
            return _boardItemTypeSO;
        }

        public void Init(BoardItemBase boardItem)
        {
            BoardItem = boardItem;

            InitCore();

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