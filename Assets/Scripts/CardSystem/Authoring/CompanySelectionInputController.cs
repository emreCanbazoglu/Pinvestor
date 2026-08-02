using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MEC;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using Pinvestor.Game.InputSystem;
using Pinvestor.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInput = Pinvestor.InputSystem.PlayerInput;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanySelectionInputController : MonoBehaviour,
        PlayerInput.ICompanySelectionActions
    {
        [SerializeField] private CompanySelectionPileWrapper _pileWrapper = null;

        [Header("Drag To Place")]
        [Tooltip("Camera used to project the pointer onto the board surface. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera = null;

        [Tooltip("How far above the board surface the dragged company hovers.")]
        [SerializeField] private float _dragHeight = 0.75f;

        [Tooltip("Snap the dragged company to the hovered cell instead of following the pointer freely.")]
        [SerializeField] private bool _snapToCell = true;

        private PlayerInput _playerInput;

        private BoardItemWrapper_Company _selectedCompany = null;
        private BoardItemWrapper_Company _placedCompany = null;

        private CanPlaceBoardItemResult _currentPlacementResult
            = CanPlaceBoardItemResult.Failure();

        private EventBinding<CompanyCardSelectedEvent> _companySelectedBinding;
        private EventBinding<CompanyReadyForPlacementEvent> _readyBinding;

        private CoroutineHandle _moveSelectedCompanyCoroutineHandle;

        /// <summary>True when placement was triggered by CompanyReadyForPlacementEvent (config-driven flow).</summary>
        private bool _isDirectPlacement;

        private void Awake()
        {
            _playerInput = new PlayerInput();
            _playerInput.CompanySelection.SetCallbacks(this);

            _readyBinding = new EventBinding<CompanyReadyForPlacementEvent>(OnCompanyReadyForPlacement);
            EventBus<CompanyReadyForPlacementEvent>.Register(_readyBinding);
        }

        private void OnDestroy()
        {
            EventBus<CompanyReadyForPlacementEvent>.Deregister(_readyBinding);
        }

        public void Activate()
        {
            _companySelectedBinding
                = new EventBinding<CompanyCardSelectedEvent>(
                    OnCompanyCardSelected);

            EventBus<CompanyCardSelectedEvent>.Register(_companySelectedBinding);
        }

        public void Deactivate()
        {
            UnregisterFromInput();
            CancelHighlightPlacement();
            Timing.KillCoroutines(
                _moveSelectedCompanyCoroutineHandle);

            _selectedCompany?.SetSelected(false);
            _selectedCompany = null;

            _placedCompany = null;
            _isDirectPlacement = false;

            EventBus<CompanyCardSelectedEvent>
                .Deregister(_companySelectedBinding);
        }
        
        private void OnCompanyCardSelected(
            CompanyCardSelectedEvent e)
        {
            if (_selectedCompany != null)
            {
                Debug.LogError(
                    "Company already selected: " + _selectedCompany.Company.CompanyId.CompanyId);
            }

            if (!_pileWrapper.CompanyCardMap.Reverse
                    .TryGetValue(e.CompanyCard, out var companyBoardItem))
            {
                Debug.LogError(
                    "Company card not found in map: " + e.CompanyCard);
                return;
            }

            EventBus<HideCompanySelectionUIEvent>
                .Raise(new HideCompanySelectionUIEvent());

            _selectedCompany = companyBoardItem;
            _selectedCompany.SetSelected(true);

            RegisterToInput();

            _moveSelectedCompanyCoroutineHandle
                = MoveSelectedCompanyRoutine()
                    .CancelWith(gameObject)
                    .RunCoroutine();
        }

        private void OnCompanyReadyForPlacement(CompanyReadyForPlacementEvent e)
        {
            _isDirectPlacement = true;
            _selectedCompany = e.CompanyWrapper;
            _selectedCompany.SetSelected(true);

            RegisterToInput();

            _moveSelectedCompanyCoroutineHandle
                = MoveSelectedCompanyRoutine()
                    .CancelWith(gameObject)
                    .RunCoroutine();
        }

        private void RegisterToInput()
        {
            _playerInput.CompanySelection.ApprovePlacement.performed += OnApprovePlacement;
            _playerInput.CompanySelection.CancelPlacement.performed += OnCancelPlacement;
            
            _playerInput.CompanySelection.Enable();
        }
        
        private void UnregisterFromInput()
        {
            _playerInput.CompanySelection.ApprovePlacement.performed -= OnApprovePlacement;
            _playerInput.CompanySelection.CancelPlacement.performed -= OnCancelPlacement;
            
            _playerInput.CompanySelection.Disable();
        }
        
        public void OnApprovePlacement(InputAction.CallbackContext ctx)
        {
            if(GameInputManager.Instance.IsInputBlocked)
                return;
            
            UnregisterFromInput();

            Timing.KillCoroutines(_moveSelectedCompanyCoroutineHandle);
            
            if(_selectedCompany == null)
                return;

            _selectedCompany.SetSelected(false);
            
            CancelHighlightPlacement();

            if (_currentPlacementResult.CanPlace)
                GameManager.Instance.BoardWrapper.Board
                    .TryPlaceBoardItem(_selectedCompany.BoardItem,
                        _currentPlacementResult.TargetCellIndices[0],
                        force: true,
                        onCompanyBoardItemPlaced);
            else
            {
                _selectedCompany.Company?.PlayDangerFeedback();
                OnCancelPlacement(ctx);
            }
            
            void onCompanyBoardItemPlaced()
            {
                _placedCompany = _selectedCompany;
                _placedCompany.Company?.PlayPlacementFeedback();
                _selectedCompany = null;

                if (_isDirectPlacement)
                {
                    _isDirectPlacement = false;
                    EventBus<CompanyPlacedEvent>.Raise(new CompanyPlacedEvent(_placedCompany));
                }

                EventBus<DeactivateCompanySelectionUIEvent>
                    .Raise(
                        new DeactivateCompanySelectionUIEvent());
            }
        }
        
        public void OnCancelPlacement(InputAction.CallbackContext ctx)
        {
            if(GameInputManager.Instance.IsInputBlocked)
                return;

            UnregisterFromInput();

            Timing.KillCoroutines(_moveSelectedCompanyCoroutineHandle);

            if (_selectedCompany == null)
                return;

            _selectedCompany.SetSelected(false);

            CancelHighlightPlacement();

            if (_isDirectPlacement)
            {
                // In direct placement there's no card selection to go back to —
                // restart drag by re-raising the event with the same wrapper.
                var wrapper = _selectedCompany;
                _selectedCompany = null;
                EventBus<CompanyReadyForPlacementEvent>.Raise(
                    new CompanyReadyForPlacementEvent(wrapper));
                return;
            }

            _selectedCompany.ReleaseToSlot();
            _selectedCompany = null;

            EventBus<ShowCompanySelectionUIEvent>
                .Raise(new ShowCompanySelectionUIEvent());
        }

        private IEnumerator<float> MoveSelectedCompanyRoutine()
        {
            BoardWrapper boardWrapper = GameManager.Instance.BoardWrapper;

            // Park the company over the board so it never flashes at world origin
            // on the frame the drag starts.
            _selectedCompany.transform.SetPositionAndRotation(
                GetHoverPosition(boardWrapper, boardWrapper.SurfaceCenter),
                boardWrapper.transform.rotation);

            while (_selectedCompany != null)
            {
                if (TryGetPointerSurfacePoint(
                        boardWrapper,
                        out Vector3 surfacePoint))
                {
                    // Placement is evaluated first — the snap target depends on it.
                    CheckPlacement(surfacePoint);

                    _selectedCompany.transform.SetPositionAndRotation(
                        GetDragPosition(boardWrapper, surfacePoint),
                        boardWrapper.transform.rotation);
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        /// <summary>
        /// Projects the pointer onto the board surface plane. Under a perspective
        /// camera a screen point has no single world position, so it only becomes
        /// meaningful once intersected with the board plane.
        /// </summary>
        private bool TryGetPointerSurfacePoint(
            BoardWrapper boardWrapper,
            out Vector3 worldPosition)
        {
            worldPosition = default;

            if (Pointer.current == null)
                return false;

            return boardWrapper.TryGetSurfacePoint(
                GetCamera(),
                Pointer.current.position.ReadValue(),
                out worldPosition);
        }

        /// <summary>
        /// Hovered cell center when the current placement is valid, otherwise the
        /// raw pointer position — both lifted off the surface by <see cref="_dragHeight"/>.
        /// </summary>
        private Vector3 GetDragPosition(
            BoardWrapper boardWrapper,
            Vector3 surfacePoint)
        {
            if (!_snapToCell
                || !_currentPlacementResult.CanPlace
                || _currentPlacementResult.TargetCellIndices.Length == 0)
                return GetHoverPosition(boardWrapper, surfacePoint);

            Vector2Int targetCellIndex = _currentPlacementResult.TargetCellIndices[0];

            if (boardWrapper.TryGetCellWrapper(
                    targetCellIndex,
                    out CellWrapper cellWrapper)
                && cellWrapper.PlacementPivot != null)
                return GetHoverPosition(
                    boardWrapper,
                    cellWrapper.PlacementPivot.position);

            return GetHoverPosition(
                boardWrapper,
                boardWrapper.GetCellWorldPosition(targetCellIndex));
        }

        private Vector3 GetHoverPosition(
            BoardWrapper boardWrapper,
            Vector3 surfacePoint)
        {
            return surfacePoint
                   + boardWrapper.SurfaceNormal * _dragHeight;
        }

        private Camera GetCamera()
        {
            if (_camera == null)
                _camera = Camera.main;

            return _camera;
        }

        public async UniTask<BoardItemWrapper_Company> WaitUntilCompanyPlacementAsync()
        {
            await UniTask.WaitUntil(() => _placedCompany != null);
            
            return _placedCompany;
        }

        private void CheckPlacement(
            Vector3 worldPosition)
        {
            CancelHighlightPlacement();

            if (!GameManager.Instance.BoardWrapper
                    .TryGetCellAt(worldPosition, out var cell))
            {
                _currentPlacementResult
                    = CanPlaceBoardItemResult.Failure();
                
                return;
            }
            
            _currentPlacementResult
                = GameManager.Instance.BoardWrapper.Board
                    .CanPlaceBoardItem(_selectedCompany.BoardItem, cell.Position);
            
            TryHighlightPlacement();
            
        }

        private void TryHighlightPlacement()
        {
            if (_currentPlacementResult.CanPlace)
            {
                GameManager.Instance.BoardWrapper.Highlighter
                    .HighlightCells(
                        _currentPlacementResult.TargetCellIndices);
            }
        }
        
        private void CancelHighlightPlacement()
        {
            GameManager.Instance.BoardWrapper.Highlighter
                .ClearHighlights();
        }
    }
}
