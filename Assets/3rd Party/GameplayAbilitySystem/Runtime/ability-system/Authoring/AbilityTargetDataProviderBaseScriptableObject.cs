using System;
using UnityEngine;

namespace AbilitySystem.Authoring
{
    public abstract class AbilityTargetDataProviderBaseScriptableObject : ScriptableObject
    {
        [field: SerializeField] public float ValidDuration { get; private set; }
        
        public abstract AbilityTargetDataProviderBaseSpec CreateSpec(
            AbilitySystemCharacter owner,
            AbstractAbilitySpec abilitySpec);
    }

    public abstract class AbilityTargetDataProviderBaseSpec
    {
        public AbilityTargetDataProviderBaseScriptableObject AbilityTargetDataProvider { get; }
        protected AbilitySystemCharacter Owner { get; }
        protected AbstractAbilitySpec AbilitySpec { get; }
        
        private AbilityTargetData _lastTargetData; 
        private DateTime _lastTargetDataTime;

        protected AbilityTargetDataProviderBaseSpec(
            AbilityTargetDataProviderBaseScriptableObject abilityTargetDataProvider,
            AbilitySystemCharacter owner,
            AbstractAbilitySpec abilitySpec)
        {
            AbilityTargetDataProvider = abilityTargetDataProvider;
            Owner = owner;
            AbilitySpec = abilitySpec;
        }
        
        public AbilityTargetData GetTargetData()
        {
            float timeSinceLastTargetData = (float)(DateTime.Now - _lastTargetDataTime).TotalSeconds;
            
            if (timeSinceLastTargetData <= AbilityTargetDataProvider.ValidDuration)
                return _lastTargetData;
            
            _lastTargetData = GetTargetDataCore();
            
            if(AbilitySpec.Ability.TargetFilters != null)
                foreach (var targetFilter in AbilitySpec.Ability.TargetFilters)
                    targetFilter.FilterTargets(Owner, ref _lastTargetData);
            
            _lastTargetDataTime = DateTime.Now;
            
            return _lastTargetData;
        }
        
        protected abstract AbilityTargetData GetTargetDataCore();
        
    }
}
