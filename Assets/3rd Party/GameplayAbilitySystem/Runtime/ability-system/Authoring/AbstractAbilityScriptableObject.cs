using System;
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
        
        /// <summary>
        /// 
        /// </summary>
        [field: SerializeField] public AbilityTargetDataProviderBaseScriptableObject TargetDataProvider { get; private set; }

        [field: SerializeField]
        public AbilityTargetFilterScriptableObject[] TargetFilters { get; private set; }
            = Array.Empty<AbilityTargetFilterScriptableObject>();
        
        /// <summary>
        /// Creates the Ability Spec (the instantiation of the ability)
        /// </summary>
        /// <param name="owner">Usually the character casting thsi ability</param>
        /// <returns>Ability Spec</returns>
        public abstract AbstractAbilitySpec CreateSpec(AbilitySystemCharacter owner, float? level);

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
    }
}