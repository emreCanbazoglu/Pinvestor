using System;
using System.Collections.Generic;
using System.Linq;
using MEC;
using MMFramework;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.CardSystem.Authoring
{
    public class CompanySelectionPileWrapper : MonoBehaviour
    {
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
            CreatePileCards().RunCoroutine();
        }

        private IEnumerator<float> CreatePileCards()
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
                
                var cardWrapper
                    = card.CreateWrapper() as CompanyCardWrapper;
                
                CompanyCardMap.Add(
                    boardItemWrapper, cardWrapper);
                
                boardItemWrapper.transform.SetParent(
                    _slots[slotIndex]);
                
                boardItemWrapper.transform.localPosition = Vector3.zero;
                
                slotIndex++;
            }
            
            yield break;
            
        }
    }
}
