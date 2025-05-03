using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Pinvestor.InputSystem.PlayerInput;

namespace Pinvestor.Game.BallShooter
{
    public class BallShooterInputController : MonoBehaviour,
        PlayerInput.IBallShooterActions
    {
        [SerializeField] private BallShooter _ballShooter = null;
        
        private PlayerInput _playerInput;
        
        public Action<Vector2> OnAimInput { get; set; }
        public Action<Vector2> OnShootInput { get; set; }
        
        public void Activate()
        {
            _playerInput = new PlayerInput();
            _playerInput.BallShooter.SetCallbacks(this);
            _playerInput.Enable();
        }

        public void Deactivate()
        {
            UnregisterFromInput();
        }

        private void UnregisterFromInput()
        {
            if (_playerInput != null)
            {
                _playerInput.Disable();
                _playerInput.Dispose();
                _playerInput = null;
            }
        }

        public void OnAim(
            InputAction.CallbackContext context)
        {
            
        }

        public void OnShoot(
            InputAction.CallbackContext context)
        {
            
        }
    }
}