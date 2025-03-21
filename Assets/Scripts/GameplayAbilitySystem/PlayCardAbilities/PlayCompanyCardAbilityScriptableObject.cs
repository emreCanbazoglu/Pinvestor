using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.AbilitySystem.Abilities
{
    public class PlayCompanyCardAbilityScriptableObject : PlayCardAbilityScriptableObject
    {
        [field: SerializeField]
        private CompanyCardDataScriptableObject CompanyCardDataScriptableObject { get; set; } = null;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner, 
            float? level = default)
        {
            return new PlayCompanyCardAbilitySpec(
                this, 
                owner, 
                CompanyCardDataScriptableObject);
        }
    }
    
    public class PlayCompanyCardAbilitySpec : PlayCardAbilitySpec
    {
        private CompanyCardDataScriptableObject _companyCardDataScriptableObject;
        
        public PlayCompanyCardAbilitySpec(
            AbstractAbilityScriptableObject abilitySO, 
            AbilitySystemCharacter owner,
            CompanyCardDataScriptableObject companyCardDataScriptableObject) : base(abilitySO, owner)
        {
            _companyCardDataScriptableObject 
                = companyCardDataScriptableObject;
        }

        protected override IEnumerator<float> ActivateAbilityCore(
            AbilityTargetData targetData = default)
        {
            yield break;
            
        }
    }
}