using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
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

        public override GameplayEffectScriptableObject GetMainGameplayEffect()
        {
            return CollisionGameplayEffect;
        }
    }
    
    public class BallCollideAbilitySpec : AbstractAbilitySpec
    {
        private readonly Ball _ball;
        
        private BallTarget _collideTarget;
        private AbilitySystemCharacter _target;
        
        private BallCollideAbilityScriptableObject BallCollideAbility 
            => (BallCollideAbilityScriptableObject)Ability;
        
        public BallCollideAbilitySpec(
            AbstractAbilityScriptableObject abilitySO, 
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _ball = owner.GetComponent<Ball>();
        }
        
        public void SetCollideTarget(
            BallTarget collideTarget)
        {
            _collideTarget = collideTarget;
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            if(_collideTarget == null)
                yield break;
            
            _target 
                = _collideTarget
                    .GetComponent<AbilitySystemCharacter>();
            
            // Check if the target is valid
            if (_target == null)
            {
                Debug.LogError("Target is not a valid AbilitySystemCharacter");
                yield break;
            }
            
            Cost();
            Cooldown();
            
            _collideTarget.CollidedBy(_ball);
            
            // Apply the collision gameplay effect to the target
            if (BallCollideAbility.CollisionGameplayEffect)
            {
                var collisionSpec 
                    = Owner.MakeOutgoingSpec(
                        this, 
                        BallCollideAbility.CollisionGameplayEffect);
                
                _target.ApplyGameplayEffectSpecToSelf(collisionSpec);
            }

            _collideTarget = null;
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
