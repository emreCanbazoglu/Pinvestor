using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using MMFramework;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using Pinvestor.GameplayAbilitySystem;
using Pinvestor.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanySelectionPileWrapper : MonoBehaviour
    {
        [SerializeField] private CompanySelectionInputController _inputController = null;
        [FormerlySerializedAs("_boardItemInfoController")] [FormerlySerializedAs("_companyInfoController")] [SerializeField] private BoardItemInfoVisualizer _boardItemInfoVisualizer = null;
        
        [SerializeField] private Transform[] _slots
            = Array.Empty<Transform>();
        
        public CompanySelectionPile Pile { get; private set; }

        public Map<BoardItemWrapper_Company, CompanyCardWrapper> CompanyCardMap { get; private set; }
            = new Map<BoardItemWrapper_Company, CompanyCardWrapper>();

        public void WrapPile(
            CompanySelectionPile pile)
        {
            Pile = pile;

            Pile.OnSlotsReset += OnSlotsReset;
            Pile.OnSlotsFilled += OnSlotsFilled;
        }

        private void OnDestroy()
        {
            Pile.OnSlotsReset -= OnSlotsReset;
            Pile.OnSlotsFilled -= OnSlotsFilled;
        }

        private void OnSlotsReset()
        {
        }

        private void OnSlotsFilled()
        {
            SelectCompanyAsync().Forget();
        }

        private async UniTask SelectCompanyAsync()
        {
            await CreatePileCardsAsync();
            
            EventBus<InitializeCompanySelectionUIEvent>
                .Raise(new InitializeCompanySelectionUIEvent(this));
            
            EventBus<ShowCompanySelectionUIEvent>
                .Raise(new ShowCompanySelectionUIEvent());
            
            _inputController.Activate();
            _boardItemInfoVisualizer.Activate();
            
            var placedCompany
                = await _inputController.WaitUntilCompanyPlacementAsync();
            
            _inputController.Deactivate();
            _boardItemInfoVisualizer.Deactivate();

            DestroyWrappers(placedCompany);
            
            EventBus<CompanyPlacedEvent>
                .Raise(new CompanyPlacedEvent(placedCompany));
        }

        private UniTask CreatePileCardsAsync()
        {
            var companyCards
                = Pile.GetCardsInSlots().Cast<CompanyCard>();
            
            int slotIndex = 0;
            
            foreach (var card in companyCards)
            {
                BoardItemData_Company boardItemData
                    = new BoardItemData_Company(
                        card.CardData.ReferenceCardId);
                
                var boardItem 
                    = BoardItemFactory.Instance.CreateBoardItem(
                        boardItemData) as BoardItem_Company;
                
                var boardItemWrapper
                    = boardItem.CreateWrapper() as BoardItemWrapper_Company;

                var abilityController 
                    = boardItemWrapper.GetComponent<AbilityController>();
                
                abilityController.Initialize(
                    ((CompanyCardDataScriptableObject)card.CardDataScriptableObject)
                    .AbilityTriggerDefinitions);
                
                var cardWrapper
                    = card.CreateWrapper() as CompanyCardWrapper;
                
                boardItemWrapper.SetCardWrapper(cardWrapper);
                boardItemWrapper.SetSlotTransform(_slots[slotIndex]);
                boardItemWrapper.ReleaseToSlot();
                
                CompanyCardMap.Add(
                    boardItemWrapper, cardWrapper);
                
                slotIndex++;
            }
            
            return UniTask.CompletedTask;
        }

        private void DestroyWrappers(
            BoardItemWrapper_Company placedCompany)
        {
            foreach (var pair in CompanyCardMap)
            {
                var boardItemWrapper = pair.Key;

                if (boardItemWrapper == placedCompany) 
                    continue;
                
                boardItemWrapper.DestroyWrapper();

                var cardWrapper = pair.Value;
                cardWrapper.DestroyWrapper();
            }

            CompanyCardMap.Clear();
        }
    }
}
