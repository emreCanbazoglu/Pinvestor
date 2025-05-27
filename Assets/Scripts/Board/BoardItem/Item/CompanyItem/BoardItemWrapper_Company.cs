using AbilitySystem;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem.Authoring;
using Pinvestor.CompanySystem;
using Pinvestor.DamagableSystem;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using Pinvestor.GameplayAbilitySystem.Abilities;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardItemWrapper_Company : BoardItemWrapperBase<BoardItem_Company>
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;
        [field: SerializeField] public AttributeSystemComponent AttributeSystemComponent { get; private set; } = null;
        [SerializeField] private GenerateRevenueAbilityScriptableObject _generateRevenueAbility = null;

        [SerializeField] private Damagable _damagable = null;
        
        [SerializeField] private BallTarget _ballTarget = null;

        public Company Company { get; private set; }
        
        public CompanyCardWrapper CompanyCardWrapper { get; private set; }

        private Transform _slotTransform;
        
        private void OnEnable()
        {
            _ballTarget.OnBallCollided += OnBallCollided;
            
            _damagable.OnDied += OnDied;
        }

        private void OnDisable()
        {
            _ballTarget.OnBallCollided -= OnBallCollided;
            
            _damagable.OnDied -= OnDied;
        }
        
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
            _slotTransform = slotTransform;
            
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
            if (_slotTransform == null)
                return;
            
            gameObject.SetActive(false);
        }
        
        public void SetCardWrapper(
            CompanyCardWrapper companyCardWrapper)
        {
            CompanyCardWrapper = companyCardWrapper;
        }
        
        public void ShowCardWrapper()
        {
            //Show the card wrapper
        }
        
        public void HideCardWrapper()
        {
            //Hide the card wrapper
        }
        
        private void OnBallCollided(Ball ball)
        {
            if (AbilitySystemCharacter.TryActivateAbility(
                    _generateRevenueAbility,
                    out _))
            {
                Debug.Log($"Company {gameObject.name} activated ability: {_generateRevenueAbility.name}");

                //Shake the company
                Company.Shake();
            }
        }

        private void OnDied(
            AbilitySystemCharacter other, 
            DamageInfo damageInfo)
        {
            AbilitySystemCharacter.CancelAllAbilities();
            
            BoardItem.TryGetPropertySpec(
                out BoardItemPropertySpec_Destroyable destroyableSpec);
            
            destroyableSpec.Destroy(null);
        }
    }
}