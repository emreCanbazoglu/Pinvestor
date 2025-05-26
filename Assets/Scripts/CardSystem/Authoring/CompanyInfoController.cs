using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Pinvestor.InputSystem.PlayerInput;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanyInfoController : MonoBehaviour,
        PlayerInput.IBoardInteractionActions
    {
        private PlayerInput _playerInput;

        private void OnDestroy()
        {
            if (_playerInput != null)
            {
                _playerInput.CompanySelection.Disable();
                _playerInput.BoardInteraction.SetCallbacks(null);
                _playerInput.Dispose();
                _playerInput = null;
            }
        }

        public void Activate()
        {
            _playerInput = new PlayerInput();
            _playerInput.BoardInteraction.SetCallbacks(this);
            
            _playerInput.CompanySelection.Enable();
        }
        
        public void Deactivate()
        {
            _playerInput.CompanySelection.Disable();
        }
        
        public void OnClick(
            InputAction.CallbackContext context)
        {
            
        }
    }
}
