using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MEC;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game.InputSystem;
using Pinvestor.UI;
using UnityEngine;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanySelectionInputController : MonoBehaviour,
        IInputReceiver
    {
        [SerializeField] private CompanySelectionPileWrapper _pileWrapper = null;
        
        public List<InputTransmitter> AttachedInputTransmitterList { get; set; }
        public Dictionary<Type, InputTransmitter.EventDelegate> Delegates { get; set; }
        public Dictionary<Delegate, InputTransmitter.EventDelegate> DelegateLookUp { get; set; }
        
        private CoroutineHandle _checkHoveredCompanyCoroutineHandle;
        
        private bool _canCheckHoveredCompany = false;
        
        private BoardItemWrapper_Company _hoveredCompany = null;
        private BoardItemWrapper_Company _selectedCompany = null;
        
        private BoardItemWrapper_Company _placedCompany = null;
        
        public void Activate()
        {
            RegisterToInput();

            _checkHoveredCompanyCoroutineHandle
                = CheckHoveredCompany()
                    .CancelWith(gameObject)
                    .RunCoroutine();
        }
        
        public void Deactivate()
        {
            UnregisterFromInput();
        }

        private IEnumerator<float> CheckHoveredCompany()
        {
            _canCheckHoveredCompany = true;
            
            while (true)
            {
                while (!_canCheckHoveredCompany
                       || GameInputManager.Instance.IsInputBlocked)
                    yield return Timing.WaitForOneFrame;

                TrySelectCompany(
                    Input.mousePosition,
                    out BoardItemWrapper_Company company);
                
                SetHoveredCompany(company);
                
                yield return Timing.WaitForOneFrame;
            }
        }

        public void RegisterToInput()
        {
            this.AddInputListener<Input_WI_OnFingerDown>(OnFingerDown);
            this.AddInputListener<Input_WI_OnFingerUp>(OnFingerUp);
            this.AddInputListener<Input_WI_OnPress>(OnPress);
        }
        
        public void UnregisterFromInput()
        {
            this.RemoveInputListener<Input_WI_OnFingerDown>(OnFingerDown);
            this.RemoveInputListener<Input_WI_OnFingerUp>(OnFingerUp);
            this.RemoveInputListener<Input_WI_OnPress>(OnPress);
        }
        
        private void OnFingerDown(Input_WI_OnFingerDown input)
        {
            if(GameInputManager.Instance.IsInputBlocked)
                return;
            
            if(_hoveredCompany == null)
                return;
            
            _canCheckHoveredCompany = false;
            
            _selectedCompany = _hoveredCompany;
            _hoveredCompany = null;
            
            EventBus<HideCompanySelectionUIEvent>.Raise(
                new HideCompanySelectionUIEvent());
        }
        
        private void OnFingerUp(Input_WI_OnFingerUp input)
        {
            if(GameInputManager.Instance.IsInputBlocked)
                return;
            
            if(_selectedCompany == null)
                return;

            _selectedCompany = null;
            
            EventBus<ShowCompanySelectionUIEvent>.Raise(
                new ShowCompanySelectionUIEvent());
            
            _canCheckHoveredCompany = true;
        }
        
        private void OnPress(Input_WI_OnPress input)
        {
            if(GameInputManager.Instance.IsInputBlocked)
                return;
        }
        
        private void SetHoveredCompany(
            BoardItemWrapper_Company company)
        {
            if(_hoveredCompany == company)
                return;
            
            if(_hoveredCompany != null)
                _hoveredCompany.SetHovered(false);
            
            _hoveredCompany = company;
            
            if(_hoveredCompany != null)
                _hoveredCompany.SetHovered(true);
        }

        private bool TrySelectCompany(
            Vector2 touchPosition,
            out BoardItemWrapper_Company company)
        {
            company = null;

            var ray
                = Camera.main.ScreenPointToRay(
                    touchPosition);
            
            if (!Physics.Raycast(ray, out var hit))
                return false;
            
            if (!hit.collider.TryGetComponent(
                    out IComponentProvider<BoardItemWrapper_Company> companyWrapperProvider))
                return false;
            
            company = companyWrapperProvider.GetComponent();
            
            return true;
        }

        public async UniTask<BoardItemWrapper_Company> WaitUntilCompanyPlacementAsync()
        {
            await UniTask.WaitUntil(() => _placedCompany != null);
            
            return _placedCompany;
        }
    }
}