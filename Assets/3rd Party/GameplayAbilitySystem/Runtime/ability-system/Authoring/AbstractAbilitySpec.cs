using System;
using System.Collections.Generic;
using AttributeSystem.Components;
using BlackboardSystem;
using GameplayTag.Authoring;

namespace AbilitySystem.Authoring
{
    public struct AbilityCooldownTime
    {
        public float TimeRemaining;
        public float TotalDuration;
    }

    public abstract class AbstractAbilitySpec
    {
        /// <summary>
        /// The ability this AbilitySpec is linked to
        /// </summary>
        public AbstractAbilityScriptableObject Ability;

        /// <summary>
        /// The owner of the AbilitySpec - usually the source
        /// </summary>
        protected AbilitySystemCharacter Owner;
        
        public AbilityTargetDataProviderBaseSpec TargetDataProviderSpec { get; set; }

        public Blackboard Blackboard { get; set; }
            = new Blackboard();

        /// <summary>
        /// Ability level
        /// </summary>
        public float Level;

        /// <summary>
        /// Is this AbilitySpec currently active?
        /// </summary>
        public bool isActive;

        /// <summary>
        /// Default constructor.  Initialises the AbilitySpec from the AbstractAbilityScriptableObject
        /// </summary>
        /// <param name="ability">Ability</param>
        /// <param name="owner">Owner - usually the character activating the ability</param>
        public AbstractAbilitySpec(AbstractAbilityScriptableObject ability, AbilitySystemCharacter owner)
        {
            Ability = ability;
            Owner = owner;
            
            if(Ability.TargetDataProvider != null)
                TargetDataProviderSpec = Ability.TargetDataProvider.CreateSpec(
                    owner, this);
        }

        /// <summary>
        /// Try activating the ability.  Remember to use StartCoroutine() since this 
        /// is a couroutine, to allow abilities to span more than one frame.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<float> TryActivateAbility(
            AbilityTargetData targetData = default,
            Action<bool> onValidatedCallback = null,
            Action onActivatedCallback = null,
            Action onEndedCallback = null)
        {
            if (!CanActivateAbility())
            {
                onValidatedCallback?.Invoke(false);
                yield break;
            }

            onValidatedCallback?.Invoke(true);
            isActive = true;
            onActivatedCallback?.Invoke();
            
            IEnumerator<float> preActivate = PreActivate();
            while(preActivate.MoveNext())
                yield return preActivate.Current;
            IEnumerator<float> activate = ActivateAbility(targetData);
            while(activate.MoveNext())
                yield return activate.Current;

            EndAbility();
            onEndedCallback?.Invoke();
        }

        /// <summary>
        /// Checks if this ability can be activated
        /// </summary>
        /// <returns></returns>
        public bool CanActivateAbility(
            bool considerCooldown = true)
        {
            PreCanActivateAbility();

            bool canActivateAbility = true;
            
            if (considerCooldown)
                canActivateAbility =
                      CheckCooldown().TimeRemaining <= 0;

            if (!canActivateAbility)
                return false;
            
            canActivateAbility
                = !isActive
                  && CheckGameplayTags()
                  && CheckCost()
                  && CanActivateAbilityCore();

            return canActivateAbility;
        }
        
        protected virtual void PreCanActivateAbility()
        {
        }

        protected virtual bool CanActivateAbilityCore()
        {
            return true;
        }

        /// <summary>
        /// Cancels the ability, if it is active
        /// </summary>
        public virtual void CancelAbility()
        {
        }

        /// <summary>
        /// Checks if Gameplay Tag requirements allow activating this ability
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckGameplayTags()
        {
            return AscHasAllTags(Owner, Ability.AbilityTags.OwnerTags.RequireTags)
                   && AscHasNoneTags(Owner, Ability.AbilityTags.OwnerTags.IgnoreTags);
        }

        /// <summary>
        /// Check if this ability is on cooldown
        /// </summary>
        /// <param name="maxDuration">The maximum duration associated with the Gameplay Effect 
        /// causing ths ability to be on cooldown</param>
        /// <returns></returns>
        public virtual AbilityCooldownTime CheckCooldown()
        {
            float maxDuration = 0;
            if (Ability.Cooldown == null) return new AbilityCooldownTime();
            var cooldownTags = Ability.Cooldown.gameplayEffectTags.GrantedTags;

            float longestCooldown = 0f;

            // Check if the cooldown tag is granted to the player, and if so, capture the remaining duration for that tag
            for (var i = 0; i < Owner.AppliedGameplayEffects.Count; i++)
            {
                var grantedTags = Owner.AppliedGameplayEffects[i].Spec.GameplayEffect.gameplayEffectTags.GrantedTags;
                for (var iTag = 0; iTag < grantedTags.Length; iTag++)
                {
                    for (var iCooldownTag = 0; iCooldownTag < cooldownTags.Length; iCooldownTag++)
                    {
                        if (grantedTags[iTag] == cooldownTags[iCooldownTag])
                        {
                            // If this is an infinite GE, then return null to signify this is on CD
                            if (Owner.AppliedGameplayEffects[i].Spec.GameplayEffect.gameplayEffect.DurationPolicy == EDurationPolicy.Infinite) return new AbilityCooldownTime()
                            {
                                TimeRemaining = float.MaxValue,
                                TotalDuration = 0
                            };

                            var durationRemaining = Owner.AppliedGameplayEffects[i].Spec.DurationRemaining;

                            if (durationRemaining > longestCooldown)
                            {
                                longestCooldown = durationRemaining;
                                maxDuration = Owner.AppliedGameplayEffects[i].Spec.TotalDuration;
                            }
                        }

                    }
                }
            }

            return new AbilityCooldownTime()
            {
                TimeRemaining = longestCooldown,
                TotalDuration = maxDuration
            };
        }

        public  AbilityTargetData GetTargetData()
        {
            return TargetDataProviderSpec?.GetTargetData() ?? new AbilityTargetData();
        }

        /// <summary>
        /// Method to activate before activating this ability.  This method is run after activation checks.
        /// </summary>
        protected virtual IEnumerator<float> PreActivate()
        {
            yield break;
        }

        protected virtual void Cost()
        {
            if (Ability.Cost)
            {
                var costSpec = Owner.MakeOutgoingSpec(this, Ability.Cost);
                Owner.ApplyGameplayEffectSpecToSelf(costSpec);
            }
        }
        
        protected virtual void Cooldown()
        {
            if (Ability.Cooldown)
            {
                var cdSpec = Owner.MakeOutgoingSpec(this, Ability.Cooldown);
                Owner.ApplyGameplayEffectSpecToSelf(cdSpec);
            }
        }
        
        /// <summary>
        /// The logic that dictates what the ability does.  Targetting logic should be placed here.
        /// Gameplay Effects are applied in this method.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerator<float> ActivateAbility(
            AbilityTargetData targetData = default);
        
        /// <summary>
        /// Method to run once the ability ends
        /// </summary>
        public virtual void EndAbility()
        {
            isActive = false;
        }

        /// <summary>
        /// Checks whether the activating character has enough resources to activate this ability
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckCost()
        {
            if (Ability.Cost == null) return true;
            var geSpec = Owner.MakeOutgoingSpec(this, Ability.Cost, Level);
            // If this isn't an instant cost, then assume it passes cooldown check
            if (geSpec.GameplayEffect.gameplayEffect.DurationPolicy != EDurationPolicy.Instant) return true;

            for (var i = 0; i < geSpec.GameplayEffect.gameplayEffect.Modifiers.Length; i++)
            {
                var modifier = geSpec.GameplayEffect.gameplayEffect.Modifiers[i];

                // Only worry about additive.  Anything else passes.
                if (modifier.ModifierOperator != EAttributeModifier.Add) continue;
                var costValue = (modifier.ModifierMagnitude.CalculateMagnitude(geSpec) * modifier.Multiplier).GetValueOrDefault();

                Owner.AttributeSystem.TryGetAttributeValue(modifier.Attribute, out AttributeValue attributeValue);

                // The total attribute after accounting for cost should be >= 0 for the cost check to succeed
                if (attributeValue.CurrentValue + costValue < 0) return false;

            }
            return true;
        }

        /// <summary>
        /// Checks if an Ability System Character has all the listed tags
        /// </summary>
        /// <param name="asc">Ability System Character</param>
        /// <param name="tags">List of tags to check</param>
        /// <returns>True, if the Ability System Character has all tags</returns>
        protected virtual bool AscHasAllTags(AbilitySystemCharacter asc, GameplayTagScriptableObject[] tags)
        {
            // If the input ASC is not valid, assume check passed
            if (!asc) return true;

            for (var iAbilityTag = 0; iAbilityTag < tags.Length; iAbilityTag++)
            {
                var abilityTag = tags[iAbilityTag];

                bool requirementPassed = false;
                List<GameplayTagScriptableObject> ascGrantedTags = asc.GrantedTags;
                for (var iAscTag = 0; iAscTag < ascGrantedTags.Count; iAscTag++)
                {
                    if (ascGrantedTags[iAscTag] == abilityTag)
                    {
                        requirementPassed = true;
                    }
                }
                // If any ability tag wasn't found, requirements failed
                if (!requirementPassed) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if an Ability System Character has none of the listed tags
        /// </summary>
        /// <param name="asc">Ability System Character</param>
        /// <param name="tags">List of tags to check</param>
        /// <returns>True, if the Ability System Character has none of the tags</returns>
        protected virtual bool AscHasNoneTags(AbilitySystemCharacter asc, GameplayTagScriptableObject[] tags)
        {
            // If the input ASC is not valid, assume check passed
            if (!asc) return true;

            for (var iAbilityTag = 0; iAbilityTag < tags.Length; iAbilityTag++)
            {
                var abilityTag = tags[iAbilityTag];

                bool requirementPassed = true;
                List<GameplayTagScriptableObject> ascGrantedTags = asc.GrantedTags;
                for (var iAscTag = 0; iAscTag < ascGrantedTags.Count; iAscTag++)
                {
                    if (ascGrantedTags[iAscTag] == abilityTag)
                    {
                        requirementPassed = false;
                    }
                }
                // If any ability tag wasn't found, requirements failed
                if (!requirementPassed) return false;
            }
            return true;
        }
    }

}