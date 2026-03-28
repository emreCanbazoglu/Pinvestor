using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// DeferredAlpha Capital — on hit, may defer 1 damage to round end (deferred cap 3);
    /// deferred damage amount increases cashout value by +15%.
    ///
    /// TODO(spec-006): deferred damage cashout value — wire DeferredDamageCount into
    /// cashout value modifier (+15% per deferred damage point) via spec-006 cashout handler.
    ///
    /// Current implementation: defers up to MaxDeferrals hits per round by intercepting
    /// HP loss. The deferred damage accumulator is exposed for spec-006 to consume.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/DeferredAlpha Ability",
        fileName = "Ability.Company.DeferredAlpha.asset")]
    public class DeferredAlphaAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject DeferHpEffect { get; private set; } = null;
        [field: SerializeField] public int MaxDeferralsPerRound { get; private set; } = 3;
        [field: SerializeField] public float CashoutBonusPerDeferral { get; private set; } = 0.15f;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new DeferredAlphaAbilitySpec(this, owner);
        }
    }

    public class DeferredAlphaAbilitySpec : AbstractAbilitySpec
    {
        private DeferredAlphaAbilityScriptableObject DeferredAlphaAbility
            => (DeferredAlphaAbilityScriptableObject)Ability;

        private BallTarget _ballTarget;
        private int _deferralsThisRound;

        /// <summary>
        /// Accumulated deferred damage count for this round.
        /// TODO(spec-006): consume this value in cashout modifier to apply +15% per point.
        /// </summary>
        public int DeferredDamageCount { get; private set; }

        private EventBinding<TurnResolutionStartedEvent> _turnBinding;
        private EventBinding<RoundStartedEvent> _roundBinding;

        public DeferredAlphaAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _ballTarget = owner.GetComponentInChildren<BallTarget>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            if (_ballTarget != null)
                _ballTarget.OnBallCollided += OnBallCollided;

            _roundBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            EventBus<RoundStartedEvent>.Register(_roundBinding);

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            if (_ballTarget != null)
                _ballTarget.OnBallCollided -= OnBallCollided;

            EventBus<RoundStartedEvent>.Deregister(_roundBinding);
            base.CancelAbility();
        }

        private void OnBallCollided(Ball ball)
        {
            if (_deferralsThisRound >= DeferredAlphaAbility.MaxDeferralsPerRound)
                return;

            // Defer 1 damage: negate the HP loss from this hit and count it.
            // The actual HP is reduced by the GAS collision system; here we apply
            // a compensating heal (DeferHpEffect) to offset the damage.
            if (DeferredAlphaAbility.DeferHpEffect != null)
            {
                var spec = Owner.MakeOutgoingSpec(this, DeferredAlphaAbility.DeferHpEffect);
                Owner.ApplyGameplayEffectSpecToSelf(spec);
            }

            _deferralsThisRound++;
            DeferredDamageCount++;

            Debug.Log($"[DeferredAlpha] Deferred 1 damage. Total deferred={DeferredDamageCount}. " +
                      $"TODO(spec-006): cashout bonus +{DeferredAlphaAbility.CashoutBonusPerDeferral * 100}% per point.");
        }

        private void OnRoundStarted(RoundStartedEvent _)
        {
            _deferralsThisRound = 0;
            DeferredDamageCount = 0;
        }
    }
}
