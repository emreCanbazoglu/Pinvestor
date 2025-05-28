using Pinvestor.BoardSystem.Authoring;
using Pinvestor.Game;
using Pinvestor.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Pinvestor.InputSystem.PlayerInput;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItemInfoVisualizer : MonoBehaviour,
        PlayerInput.IBoardInteractionActions
    {
        private PlayerInput _playerInput;
        
        private BoardItemBase _currentBoardItem;

        private void Awake()
        {
            InitializeInput();
        }
        
        private void OnDestroy()
        {
            if (_playerInput != null)
            {
                _playerInput.BoardInteraction.Disable();
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

                if (!GameManager.Instance.BoardWrapper.TryGetCellAt(
                        worldPosition,
                        out var cell))
                {
                    onProcessBoardItemSelection(null);
                    return;
                }

                if (cell.MainLayer.RegisteredBoardItemPiece == null)
                {
                    onProcessBoardItemSelection(null);
                    return;
                }
                
                if (!cell.MainLayer.RegisteredBoardItemPiece.ParentItem
                    .TryGetPropertySpec(
                        out BoardItemPropertySpec_CardOwner
                            infoVisualizableSpec))
                {
                    onProcessBoardItemSelection(null);
                    return;
                }
                
                void onProcessBoardItemSelection(
                    BoardItemBase boardItem)
                {
                    if (_currentBoardItem != null && 
                        _currentBoardItem != boardItem)
                        EventBus<HideBoardItemInfoRequestEvent>
                            .Raise(new HideBoardItemInfoRequestEvent(
                                _currentBoardItem));

                    if (boardItem == null)
                        return;
                    
                    _currentBoardItem = boardItem;
                    EventBus<ShowBoardItemInfoRequestEvent>
                        .Raise(new ShowBoardItemInfoRequestEvent(
                            _currentBoardItem));}
            }
        }
    }
}
