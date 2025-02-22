using System;
using System.Collections.Generic;
using AbilitySystem.Authoring;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Cysharp.Threading.Tasks;
using GameplayTag.Authoring;
using MEC;
using ResetableSystem;
using UnityEngine;

namespace AbilitySystem
{
    public class AbilitySystemCharacter : MonoBehaviour, IResetable
    {
        [SerializeField]
        protected AttributeSystemComponent _attributeSystem;

        [SerializeField] private AbstractAbilityScriptableObject[] _castableAbilities
            = Array.Empty<AbstractAbilityScriptableObject>();
        
        [SerializeField] private AbstractAbilityScriptableObject[] _initializationAbilities
            = Array.Empty<AbstractAbilityScriptableObject>();
        
        public AttributeSystemComponent AttributeSystem { get { return _attributeSystem; } set { _attributeSystem = value; } }
        public List<GameplayEffectContainer> AppliedGameplayEffects = new List<GameplayEffectContainer>();
        public List<AbstractAbilitySpec> GrantedAbilities = new List<AbstractAbilitySpec>();

        public List<uint> ActiveAbilityIndexes { get; private set; }
            = new List<uint>();
        
        [SerializeField] private GameplayTagScriptableObject[] _initialTags 
            = Array.Empty<GameplayTagScriptableObject>();

        public List<GameplayTagScriptableObject> GrantedTags { get; private set; }
            = new List<GameplayTagScriptableObject>();
        
        private bool _initialisationAbilitiesActivated = false;
        private bool _castableAbilitiesGranted = false;

        private bool _isInitialised = false;
        
        private uint _gameplayEffectGUID = 0;
        private uint _maxGameplayEffectGUID = 9999;

        public Action<AbstractAbilitySpec> OnGrantedAbility { get; set; }
        public Action<AbstractAbilitySpec> OnRemovedAbility { get; set; }
        public Action<GameplayEffectContainer> OnGameplayEffectApplied { get; set; }
        public Action<GameplayEffectContainer> OnGameplayEffectRemoved { get; set; }
        
        public Action<GameplayEffectSpec, AttributeValue, AttributeValue> OnGameplayModifierAppliedToSelf { get; set; } 
        public Action<GameplayEffectSpec, AttributeValue, AttributeValue> OnGameplayModifierAppliedToOther { get; set; } 
        
        public void GrantAbility(AbstractAbilitySpec spec)
        {
            this.GrantedAbilities.Add(spec);
            OnGrantedAbility?.Invoke(spec);
        }

        public void RemoveAbility(AbstractAbilitySpec spec)
        {
            GrantedAbilities.Remove(spec);
            OnRemovedAbility?.Invoke(spec);
        }

        public void RemoveAbilitiesWithTag(GameplayTagScriptableObject tag)
        {
            for (var i = GrantedAbilities.Count - 1; i >= 0; i--)
            {
                if (GrantedAbilities[i].Ability.AbilityTags.AssetTag == tag)
                {
                    var abilitySpec = GrantedAbilities[i];
                    GrantedAbilities.RemoveAt(i);
                    OnRemovedAbility?.Invoke(abilitySpec);
                }
            }
        }


        /// <summary>
        /// Applies the gameplay effect spec to self
        /// </summary>
        /// <param name="geSpec">GameplayEffectSpec to apply</param>
        public bool ApplyGameplayEffectSpecToSelf(
            GameplayEffectSpec geSpec)
        {
            if (geSpec == null) return true;
            bool tagRequirementsOK = CheckTagRequirementsMet(geSpec);

            if (tagRequirementsOK == false) return false;

            switch (geSpec.GameplayEffect.gameplayEffect.DurationPolicy)
            {
                case EDurationPolicy.HasDuration:
                case EDurationPolicy.Infinite:
                    ApplyDurationalGameplayEffect(geSpec);
                    break;
                case EDurationPolicy.Instant:
                    ApplyInstantGameplayEffect(geSpec);
                    return true;
            }

            return true;
        }
        
        public GameplayEffectSpec MakeOutgoingSpec(
            AbstractAbilitySpec abilitySpec,
            GameplayEffectScriptableObject gameplayEffect,
            float? level = 1f)
        {
            return GameplayEffectSpec.CreateNew(
                gameplayEffect: gameplayEffect,
                source: this,
                abilitySpec: abilitySpec,
                level: level.GetValueOrDefault(1));
        }

        private bool CheckTagRequirementsMet(GameplayEffectSpec geSpec)
        {
            // Build temporary list of all gametags currently applied
            var appliedTags = new List<GameplayTagScriptableObject>();
            for (var i = 0; i < AppliedGameplayEffects.Count; i++)
            {
                appliedTags.AddRange(AppliedGameplayEffects[i].Spec.GameplayEffect.gameplayEffectTags.GrantedTags);
            }

            // Every tag in the ApplicationTagRequirements.RequireTags needs to be in the character tags list
            // In other words, if any tag in ApplicationTagRequirements.RequireTags is not present, requirement is not met
            for (var i = 0; i < geSpec.GameplayEffect.gameplayEffectTags.ApplicationTagRequirements.RequireTags.Length; i++)
            {
                if (!appliedTags.Contains(geSpec.GameplayEffect.gameplayEffectTags.ApplicationTagRequirements.RequireTags[i]))
                {
                    return false;
                }
            }

            // No tag in the ApplicationTagRequirements.IgnoreTags must in the character tags list
            // In other words, if any tag in ApplicationTagRequirements.IgnoreTags is present, requirement is not met
            for (var i = 0; i < geSpec.GameplayEffect.gameplayEffectTags.ApplicationTagRequirements.IgnoreTags.Length; i++)
            {
                if (appliedTags.Contains(geSpec.GameplayEffect.gameplayEffectTags.ApplicationTagRequirements.IgnoreTags[i]))
                {
                    return false;
                }
            }

            return true;
        }
        
        private void ApplyInstantGameplayEffect(GameplayEffectSpec spec)
        {
            List<AttributeValue> cachedAttributeValues
                = new List<AttributeValue>();

            for (var i = 0; i < spec.GameplayEffect.gameplayEffect.Modifiers.Length; i++)
            {
                var modifier = spec.GameplayEffect.gameplayEffect.Modifiers[i];
                var attribute = modifier.Attribute;
                
                AttributeSystem.TryGetAttributeValue(attribute, out AttributeValue attributeValue);
                
                cachedAttributeValues.Add(attributeValue);
            }

            for (var i = 0; i < spec.GameplayEffect.gameplayEffect.Modifiers.Length; i++)
            {
                var modifier = spec.GameplayEffect.gameplayEffect.Modifiers[i];
                var magnitude = (modifier.ModifierMagnitude.CalculateMagnitude(spec) * modifier.Multiplier).GetValueOrDefault();
                var attribute = modifier.Attribute;
                AttributeSystem.TryGetAttributeValue(attribute, out AttributeValue attributeValue);

                switch (modifier.ModifierOperator)
                {
                    case EAttributeModifier.Add:
                        attributeValue.BaseValue += magnitude;
                        break;
                    case EAttributeModifier.Multiply:
                        attributeValue.BaseValue *= magnitude;
                        break;
                    case EAttributeModifier.Override:
                        attributeValue.BaseValue = magnitude;
                        break;
                }
                AttributeSystem.SetAttributeBaseValue(attribute, attributeValue.BaseValue);
            }

            for (var i = 0; i < spec.GameplayEffect.gameplayEffect.Modifiers.Length; i++)
            {
                var modifier = spec.GameplayEffect.gameplayEffect.Modifiers[i];
                var attribute = modifier.Attribute;

                AttributeSystem.TryGetAttributeValue(attribute, out AttributeValue attributeValue);

                if (cachedAttributeValues[i].CurrentValue != attributeValue.CurrentValue)
                {
                    foreach (var handler in spec.GameplayEffect.ModifierAppliedHandlers)
                    {
                        var handlerSpec 
                            = handler.CreateSpec();
                        
                        handlerSpec.HandleAppliedModifier(
                            spec,
                            cachedAttributeValues[i],
                            attributeValue);
                    }
                    
                    OnGameplayModifierAppliedToSelf?.Invoke(spec, cachedAttributeValues[i], attributeValue);
                    spec.Source.OnGameplayModifierAppliedToOther?.Invoke(spec, cachedAttributeValues[i], attributeValue);
                }
            }

        }
        private void ApplyDurationalGameplayEffect(GameplayEffectSpec spec)
        {
            var modifiersToApply = new List<GameplayEffectContainer.ModifierContainer>();
            for (var i = 0; i < spec.GameplayEffect.gameplayEffect.Modifiers.Length; i++)
            {
                var modifier = spec.GameplayEffect.gameplayEffect.Modifiers[i];
                var magnitude = (modifier.ModifierMagnitude.CalculateMagnitude(spec) * modifier.Multiplier).GetValueOrDefault();
                var attributeModifier = new AttributeModifier();
                switch (modifier.ModifierOperator)
                {
                    case EAttributeModifier.Add:
                        attributeModifier.Add = magnitude;
                        break;
                    case EAttributeModifier.Multiply:
                        attributeModifier.Multiply = magnitude;
                        break;
                    case EAttributeModifier.Override:
                        attributeModifier.Override = magnitude;
                        break;
                }
                modifiersToApply.Add(new GameplayEffectContainer.ModifierContainer() { Attribute = modifier.Attribute, Modifier = attributeModifier });
            }

            uint specGuid = GetGameplayEffectGUID();

            GameplayEffectContainer geContainer
                = new GameplayEffectContainer()
                {
                    Guid = specGuid, 
                    Spec = spec, 
                    Modifiers = modifiersToApply.ToArray(),
                };

            spec.SetDuration(spec.DurationRemaining);
            
            AppliedGameplayEffects.Add(geContainer);
            GrantedTags.AddRange(spec.GameplayEffect.gameplayEffectTags.GrantedTags);
            OnGameplayEffectApplied?.Invoke(geContainer);
        }

        private void UpdateAttributeSystem()
        {
            // Set Current Value to Base Value (default position if there are no GE affecting that atribute)
            for (var i = 0; i < this.AppliedGameplayEffects.Count; i++)
            {
                if(this.AppliedGameplayEffects[i].Spec.GameplayEffect.Period.IsPeriodic)
                    continue;
                
                var modifiers = this.AppliedGameplayEffects[i].Modifiers;
                for (var m = 0; m < modifiers.Length; m++)
                {
                    var modifier = modifiers[m];
                    AttributeSystem.UpdateAttributeModifiers(modifier.Attribute, modifier.Modifier, out _);
                }
            }
        }

        private void TickGameplayEffects()
        {
            for (var i = 0; i < this.AppliedGameplayEffects.Count; i++)
            {
                var ge = this.AppliedGameplayEffects[i].Spec;

                // Can't tick instant GE
                if (ge.GameplayEffect.gameplayEffect.DurationPolicy == EDurationPolicy.Instant) continue;

                // Update time remaining.  Stritly, it's only really valid for durational GE, but calculating for infinite GE isn't harmful
                ge.UpdateRemainingDuration(Time.deltaTime);

                // Tick the periodic component
                ge.TickPeriodic(Time.deltaTime, out var executePeriodicTick);
                if (executePeriodicTick)
                {
                    ApplyInstantGameplayEffect(ge);
                }
            }
        }

        private void CleanGameplayEffects()
        {
            for (int iGE = AppliedGameplayEffects.Count - 1; iGE >= 0; iGE--)
            {
                var geContainer = AppliedGameplayEffects[iGE];

                if (geContainer.Spec.GameplayEffect.gameplayEffect.DurationPolicy == EDurationPolicy.HasDuration
                    && geContainer.Spec.DurationRemaining <= 0)
                {
                    AppliedGameplayEffects.RemoveAt(iGE);
                    foreach (var gameplayTag in geContainer.Spec.GameplayEffect.gameplayEffectTags.GrantedTags)
                        GrantedTags.Remove(gameplayTag);

                    OnGameplayEffectRemoved?.Invoke(geContainer);
                }
            }
        }
        
        private void ActivateInitialisationAbilities()
        {
            for (var i = 0; i < _initializationAbilities.Length; i++)
            {
                var spec = _initializationAbilities[i]
                    .CreateSpec(this, null);
                
                spec.TryActivateAbility()
                    .RunCoroutine();
            }

            _initialisationAbilitiesActivated = true;
        }

        private void GrantCastableAbilities()
        {
            for (var i = 0; i < _castableAbilities.Length; i++)
            {
                var spec = _castableAbilities[i].CreateSpec(this, null);
                GrantAbility(spec);
            }

            _castableAbilitiesGranted = true;
        }

        private void GrantInitialGameplayTags()
        {
            for (var i = 0; i < _initialTags.Length; i++)
                GrantedTags.Add(_initialTags[i]);
            
        }

        public async UniTask WaitUntilInitializeAsync()
        {
            await UniTask.WaitUntil(() => _isInitialised);
        }
        
        private void Update()
        {
            // Reset all attributes to 0
            AttributeSystem.ResetAttributeModifiers();
            UpdateAttributeSystem();

            TickGameplayEffects();
            CleanGameplayEffects();
        }

        public bool TryGetAbilitySpec(
            AbstractAbilityScriptableObject ability,
            out AbstractAbilitySpec spec)
        {
            spec = null;
            
            foreach (var abilitySpec in GrantedAbilities)
            {
                if (abilitySpec.Ability == ability)
                {
                    spec = abilitySpec;
                    return true;
                }
            }

            return false;
        }

        public bool TryActivateAbility(
            AbstractAbilityScriptableObject ability,
            out AbstractAbilitySpec abilitySpec)
        {
            if(!TryGetAbilitySpec(ability, out abilitySpec))
            {
                return false;
            }

            return TryActivateAbility(abilitySpec);
        }

        public bool TryActivateAbility(
            AbstractAbilitySpec abilitySpec)
        {
            int abilityIndex = GrantedAbilities.IndexOf(abilitySpec);
            
            if(abilityIndex == -1)
                return false;

            return TryActivateAbility(abilityIndex);
        }
        
        public bool TryActivateAbility(
            int abilityIndex)
        {
            if(!_isInitialised)
                return false;

            if (abilityIndex >= GrantedAbilities.Count)
            {
                Debug.Log("Ability with index: " + abilityIndex + " not found");
                return false;
            }
            
            if(ActiveAbilityIndexes.Contains((uint)abilityIndex)
                   || !GrantedAbilities[abilityIndex].CanActivateAbility())
                return false;

            AbilityTargetData targetData
                = GrantedAbilities[abilityIndex].GetTargetData();

            TryActivateAbilityCore(
                (uint)abilityIndex,
                targetData);

            return true;
        }

        private void TryActivateAbilityCore(
            uint abilityIndex,
            AbilityTargetData targetData = default)
        {
            GrantedAbilities[(int)abilityIndex]
                .TryActivateAbility(
                    targetData, 
                    onValidated,
                    onActivated,
                    onEnded)
                .CancelWith(gameObject)
                .RunCoroutine();

            void onValidated(bool didValidate)
            {
  
            }

            void onActivated()
            {
                ActiveAbilityIndexes.Add(abilityIndex);
            }

            void onEnded()
            {
                ActiveAbilityIndexes.Remove(abilityIndex);
            }
        }

        /*public bool HasGrantedTag(
            GameplayTagScriptableObject tag)
        {
            foreach (var t in GrantedTags)
            {
                if (t.UniqueId.Equals(tag.UniqueId))
                    return true;
            }

            return false;
        }*/
        
        private void Awake()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            await AttributeSystem.WaitUntilInitializeAsync();
            
            ActivateInitialisationAbilities();

            GrantCastableAbilities();

            GrantInitialGameplayTags();
            
            while (!_initialisationAbilitiesActivated
                   || !_castableAbilitiesGranted)
            {
                await UniTask.Yield();
            }

            _isInitialised = true;
        }

        
        private uint GetGameplayEffectGUID()
        {
            uint guid = _gameplayEffectGUID;

            if (guid == _maxGameplayEffectGUID)
                guid = 0;

            _gameplayEffectGUID++;

            return guid;
        }

        public void CancelAllAbilities()
        {
            foreach (var ability in GrantedAbilities)
                ability.CancelAbility();

            GrantedAbilities.Clear();
        }

        public void ClearTagsAndEffects()
        {
            AppliedGameplayEffects.Clear();
            GrantedTags.Clear();
        }
        public void ResetResetable()
        {
            GrantedAbilities.Clear();

            _isInitialised = false;
            _initialisationAbilitiesActivated = false;
            _castableAbilitiesGranted = false;

            ClearTagsAndEffects();
            
            ActiveAbilityIndexes.Clear();
            AttributeSystem.ResetResetable();

            InitializeAsync().Forget();
        }
    }
}


namespace AbilitySystem
{
    public class GameplayEffectContainer
    {
        public uint Guid;
        public GameplayEffectSpec Spec;
        public ModifierContainer[] Modifiers;

        public class ModifierContainer
        {
            public AttributeScriptableObject Attribute;
            public AttributeModifier Modifier;
        }
    }
}