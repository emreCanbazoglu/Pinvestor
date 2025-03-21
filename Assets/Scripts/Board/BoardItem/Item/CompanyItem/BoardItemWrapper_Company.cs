using AbilitySystem;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    public class BoardItemWrapper_Company : BoardItemWrapperBase<BoardItem_Company>
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;

        protected override void WrapCore()
        {
            AbilitySystemCharacter.AttributeSystem
                .Initialize(
                    BoardItem.CompanyCardDataSo.AttributeSet);
            
            gameObject.name 
                = "BoardItemWrapper_" + BoardItem.CompanyCardDataSo.CompanyName;
            
            base.WrapCore();
        }
    }
}