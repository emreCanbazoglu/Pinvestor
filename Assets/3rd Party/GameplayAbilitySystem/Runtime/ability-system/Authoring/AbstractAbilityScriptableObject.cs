using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem.Authoring
{
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
            {
                float duration = TryGetGlobalDuration();
                var modifiers = GetAllGameplayEffectModifiers();
                return AbilityDescriptionUtility.GenerateManualDescription(
                    CustomDescription,
                    modifiers,
                    duration);
            }

            return "<i>No description available.</i>";
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
        
        public virtual GameplayEffectModifier[] GetAllGameplayEffectModifiers()
        {
            List<GameplayEffectModifier> all = new();

            foreach (var effect in GetDescriptiveGameplayEffects())
            {
                if (effect?.gameplayEffect.Modifiers != null)
                    all.AddRange(effect.gameplayEffect.Modifiers);
            }

            return all.ToArray();
        }
        
        protected virtual IEnumerable<GameplayEffectScriptableObject> GetDescriptiveGameplayEffects()
        {
            return new[]
            {
                Cost,
                Cooldown
            };
        }
    }
}