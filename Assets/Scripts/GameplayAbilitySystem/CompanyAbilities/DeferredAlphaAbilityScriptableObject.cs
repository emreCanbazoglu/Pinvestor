using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.Diagnostics;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using Pinvestor.Game.Health;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// DeferredAlpha Capital — on hit, may defer 1 damage to round end (deferred cap 3);
    /// deferred damage amount increases cashout value by +15%.
    ///
    /// Wired into the cashout pipeline: the spec implements
    /// <see cref="Pinvestor.Game.Health.ICashoutValueModifier"/>; CashoutService applies
    /// +CashoutBonusPerDeferral per deferred damage point when this company cashes out.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/DeferredAlpha Ability",
        fileName = "Ability.Company.DeferredAlpha.asset")]
    public class DeferredAlphaAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject DeferHpEffect { get; private set; } = null;
        [field: SerializeField] public GameplayEffectScriptableObject DeferredDamageEffect { get; private set; } = null;
        [field: SerializeField] public int MaxDeferralsPerRound { get; private set; } = 3;
        [field: SerializeField] public float CashoutBonusPerDeferral { get; private set; } = 0.15f;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new DeferredAlphaAbilitySpec(this, owner);
        }
    }

    public class DeferredAlphaAbilitySpec : AbstractAbilitySpec, Pinvestor.Game.Health.ICashoutValueModifier
    {
        private DeferredAlphaAbilityScriptableObject DeferredAlphaAbility
            => (DeferredAlphaAbilityScriptableObject)Ability;

        private BallTarget _ballTarget;
        private DeferredDamageState _deferredDamage;
        private int _turnCountThisRound;

        /// <summary>
        /// Accumulated deferred damage count for this round.
        /// Consumed by ModifyCashoutValue (+CashoutBonusPerDeferral per point).
        /// </summary>
        public int DeferredDamageCount => _deferredDamage?.PendingDamage ?? 0;

        /// <summary>
        /// Cashout pipeline hook: +15% (configurable) payout per deferred damage point.
        /// </summary>
        public float ModifyCashoutValue(
            BoardItemWrapper_Company cashingOutCompany,
            float currentValue)
        {
            if (cashingOutCompany?.AbilitySystemCharacter != Owner)
                return currentValue;

            if (DeferredDamageCount <= 0)
                return currentValue;

            float multiplier = 1f + DeferredAlphaAbility.CashoutBonusPerDeferral * DeferredDamageCount;
            float modified = currentValue * multiplier;

            GameEventLog.Add(
                "ABILITY",
                $"[DeferredAlpha] {DeferredDamageCount} deferred hits → cashout ×{multiplier:F2} ({currentValue} → {modified})",
                new UnityEngine.Color(0.6f, 0.6f, 1f));

            return modified;
        }

        private EventBinding<TurnResolutionStartedEvent> _turnBinding;
        private EventBinding<RoundStartedEvent> _roundBinding;

        public DeferredAlphaAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _ballTarget = owner.GetComponentInChildren<BallTarget>();
            _deferredDamage = new DeferredDamageState(
                DeferredAlphaAbility.MaxDeferralsPerRound);
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            if (_ballTarget != null)
                _ballTarget.OnBallCollided += OnBallCollided;

            _roundBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            _turnBinding = new EventBinding<TurnResolutionStartedEvent>(OnTurnResolution);
            EventBus<RoundStartedEvent>.Register(_roundBinding);
            EventBus<TurnResolutionStartedEvent>.Register(_turnBinding);

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
            EventBus<TurnResolutionStartedEvent>.Deregister(_turnBinding);
            base.CancelAbility();
        }

        private void OnBallCollided(Ball ball)
        {
            if (DeferredAlphaAbility.DeferHpEffect == null)
            {
                Debug.LogError(
                    "[DeferredAlpha] DeferHpEffect is not assigned; " +
                    "the hit cannot be deferred.");
                return;
            }

            if (!_deferredDamage.TryDeferDamage())
                return;

            // Defer 1 damage: negate the HP loss from this hit and count it.
            // The actual HP is reduced by the GAS collision system; here we apply
            // a compensating heal (DeferHpEffect) to offset the damage.
            var spec = Owner.MakeOutgoingSpec(this, DeferredAlphaAbility.DeferHpEffect);
            Owner.ApplyGameplayEffectSpecToSelf(spec);

            GameEventLog.Add(
                "ABILITY",
                $"[DeferredAlpha] Deferred hit #{DeferredDamageCount} " +
                $"(cap {DeferredAlphaAbility.MaxDeferralsPerRound})",
                new UnityEngine.Color(0.6f, 0.6f, 1f));
        }

        private void OnRoundStarted(RoundStartedEvent e)
        {
            _turnCountThisRound = e.TurnCount;
        }

        private void OnTurnResolution(TurnResolutionStartedEvent e)
        {
            if (_turnCountThisRound <= 0
                || e.TurnIndex != _turnCountThisRound - 1)
                return;

            if (DeferredDamageCount <= 0)
                return;

            if (DeferredAlphaAbility.DeferredDamageEffect == null)
            {
                Debug.LogError(
                    "[DeferredAlpha] DeferredDamageEffect is not assigned; " +
                    $"could not resolve {DeferredDamageCount} deferred damage.");
                return;
            }

            int deferredDamage = _deferredDamage.ConsumePendingDamage();

            for (int i = 0; i < deferredDamage; i++)
            {
                var spec = Owner.MakeOutgoingSpec(
                    this,
                    DeferredAlphaAbility.DeferredDamageEffect);
                Owner.ApplyGameplayEffectSpecToSelf(spec);
            }

            GameEventLog.Add(
                "ABILITY",
                $"[DeferredAlpha] Round-end debt resolved: -{deferredDamage} HP",
                new UnityEngine.Color(1f, 0.55f, 0.35f));
        }
    }
}
