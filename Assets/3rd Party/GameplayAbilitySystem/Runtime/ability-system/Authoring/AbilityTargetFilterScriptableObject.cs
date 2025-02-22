using UnityEngine;

namespace AbilitySystem.Authoring
{
    public abstract class AbilityTargetFilterScriptableObject : ScriptableObject
    {
        public abstract void FilterTargets(
            AbilitySystemCharacter owner,
            ref AbilityTargetData data);
    }
}