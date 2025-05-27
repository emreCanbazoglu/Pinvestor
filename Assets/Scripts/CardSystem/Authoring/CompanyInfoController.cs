using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Pinvestor.InputSystem.PlayerInput;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanyInfoController : MonoBehaviour,
        PlayerInput.IBoardInteractionActions
    {
        private PlayerInput _playerInput;

        private void Awake()
        {
            InitializeInput();
        }
        
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

        private void InitializeInput()
        {
            if(_playerInput != null)
                return;
            
            _playerInput = new PlayerInput();
            _playerInput.BoardInteraction.SetCallbacks(this);
        }
        
        public void Activate()
        {
            InitializeInput();
            
            _playerInput.BoardInteraction.Click.performed += OnClick;
            
            _playerInput.BoardInteraction.Enable();
        }
        
        public void Deactivate()
        {
            _playerInput.BoardInteraction.Click.performed -= OnClick;

            _playerInput.BoardInteraction.Disable();
        }
        
        public void OnClick(
            InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Vector2 screenPosition 
                    = Mouse.current.position.ReadValue();
                
                Vector3 worldPosition
                    = Camera.main.ScreenToWorldPoint(
                        new Vector3(
                            screenPosition.x, 
                            screenPosition.y, 
                            Camera.main.nearClipPlane));
                
                if(!GameManager.Instance.BoardWrapper.TryGetCellAt(
                    worldPosition,
                    out var cell))
                    return;
                
                if (cell.MainLayer.RegisteredBoardItemPiece == null)
                    return;
                
                if (cell.MainLayer.RegisteredBoardItemPiece.ParentItem is not BoardItem_Company company)
                    return;
                
                Debug.Log(
                    $"Company Clicked: {company.CompanyCardDataSo.CompanyId.CompanyId}");
            }
        }
    }
}
