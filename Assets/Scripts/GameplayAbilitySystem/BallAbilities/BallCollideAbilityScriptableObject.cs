using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using UnityEngine;

namespace Pinvestor.Game.BallSystem.Abilities
{

    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Ball Abilities/Ball Collide Ability",
        fileName = "Ability.Ball.Collide.asset")]
    public class BallCollideAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject CollisionGameplayEffect { get; private set; }
        
        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level)
        {
            return new BallCollideAbilitySpec(this, owner);
        }
    }
    
    public class BallCollideAbilitySpec : AbstractAbilitySpec
    {
        public Ball Ball { get; private set; }
        public BallTarget CollideTarget { get; private set; }
        public AbilitySystemCharacter Target { get; private set; }
        
        protected BallCollideAbilityScriptableObject BallCollideAbility 
            => (BallCollideAbilityScriptableObject)Ability;
        
        public BallCollideAbilitySpec(
            AbstractAbilityScriptableObject abilitySO, 
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            Ball = owner.GetComponent<Ball>();
        }
        
        public void SetCollideTarget(
            BallTarget collideTarget)
        {
            CollideTarget = collideTarget;
        }

        protected override IEnumerator<float> ActivateAbility(
            AbilityTargetData targetData = default)
        {
            if(CollideTarget == null)
                yield break;
            
            Target 
                = CollideTarget
                    .GetComponent<AbilitySystemCharacter>();
            
            // Check if the target is valid
            if (Target == null)
            {
                Debug.LogError("Target is not a valid AbilitySystemCharacter");
                yield break;
            }

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
            
            // Apply the collision gameplay effect to the target
            if (BallCollideAbility.CollisionGameplayEffect)
            {
                var collisionSpec 
                    = Target.MakeOutgoingSpec(
                        this, 
                        BallCollideAbility.CollisionGameplayEffect);
                
                Target.ApplyGameplayEffectSpecToSelf(collisionSpec);
            }

            CollideTarget.CollidedBy(Ball);
            CollideTarget = null;
        }
        
        public override bool CheckGameplayTags()
        {
            return AscHasAllTags(Owner, Ability.AbilityTags.OwnerTags.RequireTags)
                   && AscHasNoneTags(Owner, Ability.AbilityTags.OwnerTags.IgnoreTags)
                   && AscHasAllTags(Owner, Ability.AbilityTags.SourceTags.RequireTags)
                   && AscHasNoneTags(Owner, Ability.AbilityTags.SourceTags.IgnoreTags)
                   && AscHasAllTags(Target, Ability.AbilityTags.TargetTags.RequireTags)
                   && AscHasNoneTags(Target, Ability.AbilityTags.TargetTags.IgnoreTags);
        }
    }
}
