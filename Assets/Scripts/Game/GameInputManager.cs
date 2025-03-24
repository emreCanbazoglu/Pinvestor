using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinvestor.Game.InputSystem
{
    public class GameInputManager : Singleton<GameInputManager>
    {
        private bool _isInputBlocked;

        public bool IsInputBlocked => _isInputBlocked ||
                                      Time.frameCount == UnblockedFrame;

        public int UnblockedFrame { get; private set; }

        private List<InputBlockRequest> _blockRequests = new();

        public class InputBlockRequest
        {

        }

        #region Events

        public Action OnInputBlocked { get; set; }
        public Action OnInputUnblocked { get; set; }

        #endregion

        public InputBlockRequest RequestInputBlock()
        {
            InputBlockRequest request = new();

            _blockRequests.Add(request);

            CheckBlockInput();

            return request;
        }

        public void RequestInputUnblock(InputBlockRequest request)
        {
            _blockRequests.Remove(request);

            CheckUnblockInput();
        }

        private void CheckBlockInput()
        {
            if (_isInputBlocked)
            {
                return;
            }

            _isInputBlocked = true;

            OnInputBlocked?.Invoke();
        }

        private void CheckUnblockInput()
        {
            if (_blockRequests.Count > 0)
            {
                return;
            }

            UnblockedFrame = Time.frameCount;

            _isInputBlocked = false;

            OnInputUnblocked?.Invoke();
        }
    }
}
