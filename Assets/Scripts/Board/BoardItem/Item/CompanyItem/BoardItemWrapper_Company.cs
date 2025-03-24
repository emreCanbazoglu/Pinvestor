using AbilitySystem;
using Pinvestor.BoardSystem.Base;
using Pinvestor.CompanySystem;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardItemWrapper_Company : BoardItemWrapperBase<BoardItem_Company>
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;
        
        public Company Company { get; private set; }
        
        protected override void WrapCore()
        {
            InitializeAttributeSystem();
            
            CreateCompany();
            
            gameObject.name 
                = "BoardItemWrapper_" + BoardItem.CompanyCardDataSo.CompanyId;
            
            base.WrapCore();
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
        
        public void SetHovered(bool isHovered)
        {
            Debug.Log("Company: " + Company.CompanyId.CompanyId + " SetHovered: " + isHovered);
        }
    }
}