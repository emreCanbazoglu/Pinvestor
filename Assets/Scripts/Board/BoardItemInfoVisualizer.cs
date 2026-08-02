using Pinvestor.Game;
using Pinvestor.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Pinvestor.InputSystem.PlayerInput;

namespace Pinvestor.BoardSystem.Base
{
    public class OnViewBoardModeEnterEvent : IEvent { }
    public class OnViewBoardModeExitEvent : IEvent { }
    
    public class BoardItemInfoVisualizer : MonoBehaviour,
        PlayerInput.IBoardInteractionActions
    {
        [Tooltip("Camera used to project clicks onto the board surface. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera = null;

        private PlayerInput _playerInput;

        private BoardItemBase _currentBoardItem;
        
        private EventBinding<OnViewBoardModeEnterEvent> _viewBoardModeEnterBinding;
        private EventBinding<OnViewBoardModeExitEvent> _viewBoardModeExitBinding;

        private void Awake()
        {
            _viewBoardModeEnterBinding
                = new EventBinding<OnViewBoardModeEnterEvent>(
                    OnViewBoardModeEnter);
            
            _viewBoardModeExitBinding
                = new EventBinding<OnViewBoardModeExitEvent>(
                    OnViewBoardModeExit);
            
            EventBus<OnViewBoardModeEnterEvent>
                .Register(_viewBoardModeEnterBinding);
            EventBus<OnViewBoardModeExitEvent>
                .Register(_viewBoardModeExitBinding);
            
            InitializeInput();
        }

        private void OnViewBoardModeEnter(
            OnViewBoardModeEnterEvent e)
        {
            Activate();
        }
        
        private void OnViewBoardModeExit(
            OnViewBoardModeExitEvent e)
        {
            Deactivate();
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

        private Camera GetCamera()
        {
            if (_camera == null)
                _camera = Camera.main;

            return _camera;
        }

        private void InitializeInput()
        {
            if(_playerInput != null)
                return;
            
            _playerInput = new PlayerInput();
            _playerInput.BoardInteraction.SetCallbacks(this);
        }
        
        private void Activate()
        {
            InitializeInput();
            
            _playerInput.BoardInteraction.Click.performed += OnClick;
            
            _playerInput.BoardInteraction.Enable();
        }
        
        private void Deactivate()
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

                // Perspective camera: the click only resolves to a board position
                // once its ray is intersected with the board surface plane.
                if (!GameManager.Instance.BoardWrapper.TryGetCellAtScreenPoint(
                        GetCamera(),
                        screenPosition,
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
                
                var boardItem = 
                    cell.MainLayer.RegisteredBoardItemPiece
                        .ParentItem;
                
                if (!boardItem
                    .TryGetPropertySpec(
                        out BoardItemPropertySpec_CardOwner
                            infoVisualizableSpec))
                {
                    onProcessBoardItemSelection(null);
                }
                else
                {
                    onProcessBoardItemSelection(
                        boardItem);
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
