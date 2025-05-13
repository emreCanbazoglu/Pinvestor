using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public abstract class BoardItemActionScriptableObjectBase : ScriptableObject
    {
        public abstract BoardItemActionSpecBase CreateSpec(BoardItemBase owner);
    }
    
    public abstract class BoardItemActionSpecBase
    {
        public BoardItemActionScriptableObjectBase ScriptableObject { get; private set; }
        
        public BoardItemBase BoardItem { get; private set; }
        
        public BoardItemActionSpecBase(
            BoardItemActionScriptableObjectBase scriptableObject,
            BoardItemBase owner)
        {
            ScriptableObject = scriptableObject;
            BoardItem = owner;

            owner.OnDisposed += OnDisposed;
        }

        private void OnDisposed(BoardItemBase boardItem)
        {
            boardItem.OnDisposed -= OnDisposed;

            Dispose();
        }

        protected abstract void ExecuteCore(Action onCompleted);

        protected virtual void DisposeCore()
        {
            
        }
        
        public void Execute(
            Action onExecuted = null)
        {
            ExecuteCore(onCompleted);

            void onCompleted()
            {
                onExecuted?.Invoke();
            }
        }

        private void Dispose()
        {
            DisposeCore();
        }
    }
}