using System;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    [Serializable]
    public class BoardItemActionPlayer
    {
        [SerializeField] private BoardItemActionScriptableObjectBase[] _boardItemActions
            = Array.Empty<BoardItemActionScriptableObjectBase>();

        [SerializeField] private bool _isQueued = false;

        public void Execute(BoardItemBase owner, Action onCompleted)
        {
            if (_isQueued)
            {
                QueuedExecution(owner, onCompleted);
            }
            else
            {
                ParallelExecution(owner, onCompleted);
            }
        }

        private void QueuedExecution(BoardItemBase owner, Action onCompleted)
        {
            int index = -1;
            
            onExecuted();
            
            void onExecuted()
            {
                index++;

                if (index == _boardItemActions.Length)
                {
                    onCompleted?.Invoke();

                    return;
                }
                
                _boardItemActions[index].CreateSpec(owner).Execute(onExecuted);
            }
        }

        private void ParallelExecution(BoardItemBase owner, Action onCompleted)
        {
            int count = 0;

            if (_boardItemActions.Length == 0)
            {
                onCompleted?.Invoke();

                return;
            }

            _boardItemActions.ForEach(i => i.CreateSpec(owner).Execute(onExecuted));
            
            void onExecuted()
            {
                count++;

                if (count == _boardItemActions.Length)
                {
                    onCompleted?.Invoke();
                }
            }
        }
    }
}