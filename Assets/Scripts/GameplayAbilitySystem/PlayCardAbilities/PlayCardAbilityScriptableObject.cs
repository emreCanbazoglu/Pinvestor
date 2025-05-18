using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using MEC;
using Pinvestor.CardSystem;

namespace Pinvestor.AbilitySystem.Abilities
{
    
    public abstract class PlayCardAbilityScriptableObject : AbstractAbilityScriptableObject
    {
    }

    public abstract class PlayCardAbilitySpec : AbstractAbilitySpec
    {
        public CardPlayer TargetCardPlayer { get; set; }
        
        public PlayCardAbilitySpec(
            AbstractAbilityScriptableObject abilitySO, 
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
        }

        protected sealed override IEnumerator<float> ActivateAbility()
        {
            if (Ability.Cost)
            {
                var costSpec = Owner.MakeOutgoingSpec(this, Ability.Cost);
                Owner.ApplyGameplayEffectSpecToSelf(costSpec);
            }
            
            if (Ability.Cooldown)
            {
                var cdSpec = Owner.MakeOutgoingSpec(this, Ability.Cooldown);
                Owner.ApplyGameplayEffectSpecToSelf(cdSpec);
            }

            yield return ActivateAbilityCore()
                .WaitUntilDone();
        }

        protected virtual IEnumerator<float> ActivateAbilityCore()
        {
            yield break;
        }
    }
}