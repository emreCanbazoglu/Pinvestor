using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.CompanySystem;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/GenerateRevenue Ability",
        fileName = "Ability.Company.GenerateRevenue.asset")]
    public class GenerateRevenueAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject RevenueGameplayEffect { get; private set; } = null;
        
        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level)
        {
            return new GenerateRevenueAbilitySpec(this, owner);
        }
    }
    
    public class GenerateRevenueAbilitySpec : AbstractAbilitySpec
    {
        private readonly Company _company;
        private AbilitySystemCharacter _target;
        
        private GenerateRevenueAbilityScriptableObject GenerateRevenueAbility 
            => (GenerateRevenueAbilityScriptableObject)Ability; 
        
        public GenerateRevenueAbilitySpec(
            AbstractAbilityScriptableObject abilitySO, 
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _company = owner
                .GetComponentInChildren<Company>();
        }

        protected override void PreCanActivateAbility()
        {
            _target
                = GameManager.Instance.GamePlayer
                    .GetComponent<AbilitySystemCharacter>();

            // Check if the target is valid
            if (_target == null)
            {
                Debug.LogError("Target is not a valid AbilitySystemCharacter.");
                return;
            }
            
            base.PreCanActivateAbility();
        }

        protected override IEnumerator<float> ActivateAbility(
            AbilityTargetData targetData = default)
        {
            Cost();
            Cooldown();

            if (GenerateRevenueAbility.RevenueGameplayEffect)
            {
                var spec
                    = Owner
                        .MakeOutgoingSpec(
                            this, 
                            GenerateRevenueAbility.RevenueGameplayEffect);

                _target.ApplyGameplayEffectSpecToSelf(spec);
            }
            
            yield break;
        }
        
        public override bool CheckGameplayTags()
        {
            return AscHasAllTags(Owner, Ability.AbilityTags.OwnerTags.RequireTags)
                   && AscHasNoneTags(Owner, Ability.AbilityTags.OwnerTags.IgnoreTags)
                   && AscHasAllTags(Owner, Ability.AbilityTags.SourceTags.RequireTags)
                   && AscHasNoneTags(Owner, Ability.AbilityTags.SourceTags.IgnoreTags)
                   && AscHasAllTags(_target, Ability.AbilityTags.TargetTags.RequireTags)
                   && AscHasNoneTags(_target, Ability.AbilityTags.TargetTags.IgnoreTags);
        }
    }
}
