using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    [System.Serializable]
    public struct AbilityActionDescription
    {
        [TextArea]
        public string Template;       // e.g. "Increases {attribute} by {value} every hit."
        public int ModifierIndex;     // which modifier this refers to (default = 0)
        public bool ShowDuration;
    }

    
    public abstract class AbstractAbilityScriptableObject : ScriptableObject
    {
        [ScriptableObjectId] public string AbilityId;        
        
        
        /// <summary>
        /// Tags for this ability
        /// </summary>
        [SerializeField] public AbilityTags AbilityTags;

        /// <summary>
        /// The GameplayEffect that defines the cost associated with activating the ability
        /// </summary>
        /// <param name="owner">Usually the character activating this ability</param>
        /// <returns></returns>
        [SerializeField] public GameplayEffectScriptableObject Cost;

        /// <summary>
        /// The GameplayEffect that defines the cooldown associated with this ability
        /// </summary>
        /// <param name="owner">Usually the character activating this ability</param>
        /// <returns></returns>
        [SerializeField] public GameplayEffectScriptableObject Cooldown;
        
        [SerializeField]
        public List<AbilityActionDescription> ActionDescriptions = new();

        [SerializeField]
        [TextArea]
        public string CustomDescription;
        
        /// <summary>
        /// Creates the Ability Spec (the instantiation of the ability)
        /// </summary>
        /// <param name="owner">Usually the character casting thsi ability</param>
        /// <returns>Ability Spec</returns>
        public abstract AbstractAbilitySpec CreateSpec(AbilitySystemCharacter owner, float? level = default);

        public override bool Equals(object obj)
        {
            // Check for null and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            AbstractAbilityScriptableObject other = (AbstractAbilityScriptableObject)obj;
            return AbilityId == other.AbilityId;
        }

        public override int GetHashCode()
        {
            return (AbilityId != null ? AbilityId.GetHashCode() : 0);
        }

        public static bool operator ==(AbstractAbilityScriptableObject a, AbstractAbilityScriptableObject b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(AbstractAbilityScriptableObject a, AbstractAbilityScriptableObject b)
        {
            return !(a == b);
        }
        
        public virtual string GetDescription(float level = 1)
        {
            if (!string.IsNullOrWhiteSpace(CustomDescription))
                return CustomDescription;

            if (ActionDescriptions != null && ActionDescriptions.Count > 0)
            {
                float duration = TryGetGlobalDuration();
                return AbilityDescriptionUtility.GenerateActionDescriptions(
                    ActionDescriptions,
                    GetMainGameplayEffect(),
                    duration);
            }
            
            return AbilityDescriptionUtility.GenerateFullAbilityDescription(
                this,
                GetDescriptiveGameplayEffects(),
                GetDescriptiveTargetFilters(),
                level);
        }
        
        public virtual float TryGetGlobalDuration()
        {
            // Try to read duration from first effect
            foreach (var effect in GetDescriptiveGameplayEffects())
            {
                if (effect == null) continue;

                if (effect.gameplayEffect.DurationPolicy == EDurationPolicy.HasDuration)
                {
                    return effect.gameplayEffect.DurationModifier?.GetPreviewValue()
                           ?? effect.gameplayEffect.DurationMultiplier;
                }
            }

            return 0;
        }

        public virtual GameplayEffectScriptableObject GetMainGameplayEffect()
        {
            return null;
        }

        protected virtual IEnumerable<GameplayEffectScriptableObject> GetDescriptiveGameplayEffects()
        {
            return new[] { Cost, Cooldown }; // Subclasses can override this
        }
        
        protected virtual IEnumerable<IAbilityTargetFilter> GetDescriptiveTargetFilters()
        {
            return Array.Empty<IAbilityTargetFilter>(); // Subclasses can override this
        }
    }
}