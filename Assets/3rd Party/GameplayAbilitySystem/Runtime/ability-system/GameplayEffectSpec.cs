using System;
using System.Collections.Generic;
using AbilitySystem.Authoring;
using AttributeSystem.Components;
using GameplayTag.Authoring;

namespace AbilitySystem
{
    [Serializable]
    public class GameplayEffectSpec
    {
        /// <summary>
        /// Original gameplay effect that is the base for this spec
        /// </summary>
        public GameplayEffectScriptableObject GameplayEffect { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public float DurationRemaining { get; private set; }

        public float TotalDuration { get; private set; }
        public float TimeUntilPeriodTick { get; private set; }
        public float Level { get; private set; }
        public AbilitySystemCharacter Source { get; private set; }
        public AbilitySystemCharacter Target { get; private set; }
        
        public AbstractAbilitySpec AbilitySpec { get; private set; }
        
        public AttributeValue? SourceCapturedAttribute = null;

        public List<GameplayTagScriptableObject> RuntimeGameplayTags
            = new List<GameplayTagScriptableObject>();

        public static GameplayEffectSpec CreateNew(
            GameplayEffectScriptableObject gameplayEffect, 
            AbilitySystemCharacter source,
            AbstractAbilitySpec abilitySpec,
            float level = 1,
            float passedDuration = 0)
        {
            return new GameplayEffectSpec(gameplayEffect, source, abilitySpec, level, passedDuration);
        }

        private GameplayEffectSpec(
            GameplayEffectScriptableObject gameplayEffect, 
            AbilitySystemCharacter source, 
            AbstractAbilitySpec abilitySpec,
            float level = 1,
            float passedDuration = 0)
        {
            GameplayEffect = gameplayEffect;
            Source = source;
            AbilitySpec = abilitySpec;
            for (var i = 0; i < GameplayEffect.gameplayEffect.Modifiers.Length; i++)
            {
                GameplayEffect.gameplayEffect.Modifiers[i].ModifierMagnitude.Initialise(this);
            }
            Level = level;
            
            if (gameplayEffect.gameplayEffect.DurationPolicy == EDurationPolicy.HasDuration
                && GameplayEffect.gameplayEffect.DurationModifier)
            {
                DurationRemaining = gameplayEffect.gameplayEffect.DurationModifier.CalculateMagnitude(this)
                                             .GetValueOrDefault()
                                         * gameplayEffect.gameplayEffect.DurationMultiplier;
                TotalDuration = DurationRemaining;

                DurationRemaining -= passedDuration;
            }

            TimeUntilPeriodTick = GameplayEffect.Period.Period;
            // By setting the time to 0, we make sure it gets executed at first opportunity
            if (GameplayEffect.Period.ExecuteOnApplication)
            {
                TimeUntilPeriodTick = 0;
            }
        }

        public GameplayEffectSpec SetTarget(AbilitySystemCharacter target)
        {
            Target = target;
            return this;
        }

        public void SetTotalDuration(float totalDuration)
        {
            TotalDuration = totalDuration;
        }

        public GameplayEffectSpec SetDuration(float duration)
        {
            DurationRemaining = duration;
            return this;
        }

        public GameplayEffectSpec UpdateRemainingDuration(float deltaTime)
        {
            DurationRemaining -= deltaTime;
            return this;
        }

        public GameplayEffectSpec TickPeriodic(float deltaTime, out bool executePeriodicTick)
        {
            if (!GameplayEffect.Period.IsPeriodic)
            {
                executePeriodicTick = false;
                return this;
            }
            
            TimeUntilPeriodTick -= deltaTime;
            executePeriodicTick = false;
            if (TimeUntilPeriodTick <= 0)
            {
                TimeUntilPeriodTick = GameplayEffect.Period.Period;

                // Check to make sure period is valid, otherwise we'd just end up executing every frame
                if (GameplayEffect.Period.Period > 0)
                {
                    executePeriodicTick = true;
                }
            }

            return this;
        }

        public GameplayEffectSpec SetLevel(float level)
        {
            Level = level;
            return this;
        }

    }

}
