using AbilitySystem;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.CompanySystem
{
    public class Company : MonoBehaviour
    {
        [field: SerializeField] public AbilitySystemCharacter AbilitySystemCharacter { get; private set; } = null;

        public CompanyCard CompanyCard { get; private set; }
        
        public void Initialize(
            CompanyCard companyCard)
        {
            CompanyCard = companyCard;
            
            AbilitySystemCharacter.AttributeSystem
                .Initialize(
                    CompanyCard.CastedCardDataSo.AttributeSet);
        }
    }
}
