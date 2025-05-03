using AbilitySystem;
using DG.Tweening;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CompanySystem;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardItemWrapper_Company : BoardItemWrapperBase<BoardItem_Company>
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;

        [SerializeField] private float _releaseSpeed = 1f;
        [SerializeField] private Ease _releaseEase = Ease.OutBack;
        
        public Company Company { get; private set; }
        
        public Transform SlotTransform { get; private set; }
        
        protected override void WrapCore()
        {
            InitializeAttributeSystem();
            
            CreateCompany();
            
            gameObject.name 
                = "BoardItemWrapper_" + BoardItem.CompanyCardDataSo.CompanyId.CompanyId;
            
            BoardItem.TryGetPropertySpec(
                out BoardItemPropertySpec_PlacableCompany placableCompanySpec);

            placableCompanySpec.OnPlaced += OnCompanyPlaced;
            
            base.WrapCore();
        }
        
        protected override void DisposeCore()
        {
            BoardItem.TryGetPropertySpec(
                out BoardItemPropertySpec_PlacableCompany placableCompanySpec);

            placableCompanySpec.OnPlaced -= OnCompanyPlaced;
            
            base.DisposeCore();
        }

        private void OnCompanyPlaced(Cell cell)
        {
            if (cell == null)
                return;

            var parentCellWrapper 
                = GameManager.Instance.BoardWrapper
                    .CellWrappers[cell];
            
            transform.SetParent(parentCellWrapper.transform);
        }


        private void InitializeAttributeSystem()
        {
            AbilitySystemCharacter.AttributeSystem
                .Initialize(
                    BoardItem.CompanyCardDataSo.AttributeSet);
        }

        private void CreateCompany()
        {
            CompanyFactory.Instance.TryCreateCompany(
                BoardItem.CompanyCardDataSo.CompanyId,
                out Company company);
            
            Company = company;
            Company.SetBoardItemWrapper(this);
            
            Company.transform.SetParent(VisualContainer);
            Company.transform.localPosition = Vector3.zero;
        }
        
        public void SetSlotTransform(Transform slotTransform)
        {
            SlotTransform = slotTransform;
            
            transform.SetParent(slotTransform);
            transform.localPosition = Vector3.zero;
        }
        
        public void SetSelected(bool isSelected)
        {
            Debug.Log("Company: " + Company.CompanyId.CompanyId + " SetSelected: " + isSelected);
            
            gameObject.SetActive(true);
        }

        public void ReleaseToSlot()
        {
            if (SlotTransform == null)
                return;
            
            gameObject.SetActive(false);

            /*transform
                .DOMove(SlotTransform.position, _releaseSpeed)
                .SetSpeedBased()
                .SetEase(_releaseEase)
                .OnComplete(() =>
                {
                    transform.SetParent(SlotTransform);
                    transform.localPosition = Vector3.zero;
                });*/
        }
    }
}