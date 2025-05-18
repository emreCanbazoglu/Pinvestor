using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    [CreateAssetMenu(menuName = "Gameplay Ability System/Abilities/Stat Initialisation")]
    public class InitialiseStatsAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        public GameplayEffectScriptableObject[] InitialisationGE;

        public override AbstractAbilitySpec CreateSpec(AbilitySystemCharacter owner, float? level)
        {
            var spec = new InitialiseStatsAbility(this, owner);
            spec.Level = level.GetValueOrDefault(1);
            return spec;
        }

        public class InitialiseStatsAbility : AbstractAbilitySpec
        {
            public InitialiseStatsAbility(AbstractAbilityScriptableObject abilitySO, AbilitySystemCharacter owner) : base(abilitySO, owner)
            {
            }

            protected override IEnumerator<float> ActivateAbility()
            {
                // Apply cost and cooldown (if any)
                if (Ability.Cooldown)
                {
                    var cdSpec = Owner.MakeOutgoingSpec(this, Ability.Cooldown);
                    Owner.ApplyGameplayEffectSpecToSelf(cdSpec);
                }

                if (Ability.Cost)
                {
                    var costSpec = Owner.MakeOutgoingSpec(this, Ability.Cost);
                    Owner.ApplyGameplayEffectSpecToSelf(costSpec);
                }

                InitialiseStatsAbilityScriptableObject abilitySO = Ability as InitialiseStatsAbilityScriptableObject;
                Owner.AttributeSystem.UpdateAttributeCurrentValues();

                for (var i = 0; i < abilitySO.InitialisationGE.Length; i++)
                {
                    var effectSpec = Owner.MakeOutgoingSpec(this, abilitySO.InitialisationGE[i]);
                    Owner.ApplyGameplayEffectSpecToSelf(effectSpec);
                    Owner.AttributeSystem.UpdateAttributeCurrentValues();
                }

                yield break;
            }
        }
    }

}