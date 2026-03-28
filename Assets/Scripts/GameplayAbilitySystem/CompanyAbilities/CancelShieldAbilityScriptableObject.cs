using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// CancelShield PR — if this company took 4+ hits this turn, resolve at turn end:
    /// 50% chance bonus revenue, 50% chance fine payment.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/CancelShield Ability",
        fileName = "Ability.Company.CancelShield.asset")]
    public class CancelShieldAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject BonusRevenueEffect { get; private set; } = null;
        [field: SerializeField] public GameplayEffectScriptableObject FineEffect { get; private set; } = null;
        [field: SerializeField] public int HitThreshold { get; private set; } = 4;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new CancelShieldAbilitySpec(this, owner);
        }
    }

    public class CancelShieldAbilitySpec : AbstractAbilitySpec
    {
        private CancelShieldAbilityScriptableObject CancelShieldAbility
            => (CancelShieldAbilityScriptableObject)Ability;

        private BallTarget _ballTarget;
        private int _hitsThisTurn;
        private EventBinding<TurnResolutionStartedEvent> _turnResBinding;

        public CancelShieldAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _ballTarget = owner.GetComponentInChildren<BallTarget>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            _hitsThisTurn = 0;

            if (_ballTarget != null)
                _ballTarget.OnBallCollided += OnBallCollided;

            _turnResBinding = new EventBinding<TurnResolutionStartedEvent>(OnTurnResolution);
            EventBus<TurnResolutionStartedEvent>.Register(_turnResBinding);

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            if (_ballTarget != null)
                _ballTarget.OnBallCollided -= OnBallCollided;

            EventBus<TurnResolutionStartedEvent>.Deregister(_turnResBinding);
            base.CancelAbility();
        }

        private void OnBallCollided(Ball ball)
        {
            _hitsThisTurn++;
        }

        private void OnTurnResolution(TurnResolutionStartedEvent _)
        {
            if (_hitsThisTurn >= CancelShieldAbility.HitThreshold)
            {
                bool bonusRevenue = Random.value >= 0.5f;
                if (bonusRevenue && CancelShieldAbility.BonusRevenueEffect != null)
                {
                    var spec = Owner.MakeOutgoingSpec(this, CancelShieldAbility.BonusRevenueEffect);
                    Owner.ApplyGameplayEffectSpecToSelf(spec);
                    Debug.Log("[CancelShield] PR survived — bonus revenue granted.");
                }
                else if (!bonusRevenue && CancelShieldAbility.FineEffect != null)
                {
                    var spec = Owner.MakeOutgoingSpec(this, CancelShieldAbility.FineEffect);
                    Owner.ApplyGameplayEffectSpecToSelf(spec);
                    Debug.Log("[CancelShield] PR overexposed — fine applied.");
                }
            }

            _hitsThisTurn = 0;
        }
    }
}
