using UnityEngine;

namespace AbilitySystem.ModifierMagnitude
{
    public abstract class ModifierMagnitudeScriptableObject : ScriptableObject
    {
        /// <summary>
        /// Function called when the spec is first initialised (e.g. by the Instigator/Source Ability System)
        /// </summary>
        /// <param name="spec">Gameplay Effect Spec</param>
        public virtual void Initialise(GameplayEffectSpec spec)
        {
        }

        /// <summary>
        /// Function called when the magnitude is calculated, usually after the target has been assigned
        /// </summary>
        /// <param name="spec">Gameplay Effect Spec</param>
        /// <returns></returns>
        public abstract float? CalculateMagnitude(GameplayEffectSpec spec);
        
        public virtual float GetPreviewValue()
        {
            return 1f;
        }
    }
}
