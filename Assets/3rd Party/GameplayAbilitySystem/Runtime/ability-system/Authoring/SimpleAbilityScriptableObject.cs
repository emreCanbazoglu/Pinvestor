using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    /// <summary>
    /// Simple Ability that applies a Gameplay Effect to the activating character
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay Ability System/Abilities/Simple Ability")]
    public class SimpleAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        /// <summary>
        /// Gameplay Effect to apply
        /// </summary>
        public GameplayEffectScriptableObject GameplayEffect;

        /// <summary>
        /// Creates the Ability Spec, which is instantiated for each character.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public override AbstractAbilitySpec CreateSpec(AbilitySystemCharacter owner, float? level)
        {
            var spec = new SimpleAbilitySpec(this, owner);
            spec.Level = level.GetValueOrDefault(1);
            return spec;
        }

        /// <summary>
        /// The Ability Spec is the instantiation of the ability.  Since the Ability Spec
        /// is instantiated for each character, we can store stateful data here.
        /// </summary>
        public class SimpleAbilitySpec : AbstractAbilitySpec
        {
            public SimpleAbilitySpec(AbstractAbilityScriptableObject abilitySO, AbilitySystemCharacter owner) : base(abilitySO, owner)
            {
            }

            /// <summary>
            /// What happens when we activate the ability.
            /// 
            /// In this example, we apply the cost and cooldown, and then we apply the main
            /// gameplay effect
            /// </summary>
            /// <returns></returns>
            protected override IEnumerator<float> ActivateAbility()
            {
                //Apply Cooldown
                if (Ability.Cooldown)
                {
                    var cdSpec = Owner.MakeOutgoingSpec(this, Ability.Cooldown);
                    Owner.ApplyGameplayEffectSpecToSelf(cdSpec);
                }
                
                // Apply cost
                if (Ability.Cost)
                {
                    var costSpec = Owner.MakeOutgoingSpec(this, Ability.Cost);
                    Owner.ApplyGameplayEffectSpecToSelf(costSpec);
                }


                // Apply primary effect
                var effectSpec = Owner.MakeOutgoingSpec(this, (Ability as SimpleAbilityScriptableObject).GameplayEffect);
                Owner.ApplyGameplayEffectSpecToSelf(effectSpec);

                yield break;
            }

            /// <summary>
            /// Checks to make sure Gameplay Tags checks are met. 
            /// 
            /// Since the target is also the character activating the ability,
            /// we can just use Owner for all of them.
            /// </summary>
            /// <returns></returns>
            public override bool CheckGameplayTags()
            {
                return AscHasAllTags(Owner, Ability.AbilityTags.OwnerTags.RequireTags)
                        && AscHasNoneTags(Owner, Ability.AbilityTags.OwnerTags.IgnoreTags)
                        && AscHasAllTags(Owner, Ability.AbilityTags.SourceTags.RequireTags)
                        && AscHasNoneTags(Owner, Ability.AbilityTags.SourceTags.IgnoreTags)
                        && AscHasAllTags(Owner, Ability.AbilityTags.TargetTags.RequireTags)
                        && AscHasNoneTags(Owner, Ability.AbilityTags.TargetTags.IgnoreTags);
            }
        }
    }

}