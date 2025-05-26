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

        private PlayerInput _playerInput;
        
        private BoardItemWrapper_Company _selectedCompany = null;
        private BoardItemWrapper_Company _placedCompany = null;

        private CanPlaceBoardItemResult _currentPlacementResult
            = CanPlaceBoardItemResult.Failure();
        
        private EventBinding<CompanyCardSelectedEvent> _companySelectedBinding;
        
        private CoroutineHandle _moveSelectedCompanyCoroutineHandle;
        
        public void Activate()
        {
            _companySelectedBinding
                = new EventBinding<CompanyCardSelectedEvent>(
                    OnCompanyCardSelected);
            
            EventBus<CompanyCardSelectedEvent>.Register(_companySelectedBinding);

            _playerInput = new PlayerInput();
            _playerInput.CompanySelection.SetCallbacks(this);
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
                OnCancelPlacement(ctx);
            }
            
            void onCompanyBoardItemPlaced()
            {
                _placedCompany = _selectedCompany;
                
                _selectedCompany = null;
                
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
            
            _selectedCompany.ReleaseToSlot();
            _selectedCompany = null;
            
            EventBus<ShowCompanySelectionUIEvent>
                .Raise(new ShowCompanySelectionUIEvent());
        }

        private IEnumerator<float> MoveSelectedCompanyRoutine()
        {
            while (_selectedCompany != null)
            {
                Vector2 inputPosition = Input.mousePosition;

                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(
                    new Vector3(
                        inputPosition.x,
                        inputPosition.y));

                worldPosition.z = _selectedCompany.transform.position.z;

                _selectedCompany.transform.position = worldPosition;

                CheckPlacement(worldPosition);
                
                yield return Timing.WaitForOneFrame;
            }
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