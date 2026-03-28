using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// SleepDebt SaaS — exactly 2 hits this turn → +1 permanent RPH (cap +6);
    /// more than 2 hits → lose extra HP equal to (hits - 2).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/SleepDebt Ability",
        fileName = "Ability.Company.SleepDebt.asset")]
    public class SleepDebtAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject PermanentRphBonusEffect { get; private set; } = null;
        [field: SerializeField] public GameplayEffectScriptableObject ExtraHpLossEffect { get; private set; } = null;
        [field: SerializeField] public int TargetHitCount { get; private set; } = 2;
        [field: SerializeField] public int MaxPermanentBonus { get; private set; } = 6;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new SleepDebtAbilitySpec(this, owner);
        }
    }

    public class SleepDebtAbilitySpec : AbstractAbilitySpec
    {
        private SleepDebtAbilityScriptableObject SleepDebtAbility
            => (SleepDebtAbilityScriptableObject)Ability;

        private BallTarget _ballTarget;
        private BoardItemWrapper_Company _selfWrapper;
        private int _hitsThisTurn;
        private int _permanentBonusCount;

        private EventBinding<TurnResolutionStartedEvent> _turnResBinding;

        public SleepDebtAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _ballTarget = owner.GetComponentInChildren<BallTarget>();
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
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
            if (_hitsThisTurn == SleepDebtAbility.TargetHitCount)
            {
                // Exactly 2 hits: grant permanent RPH bonus if under cap
                if (_permanentBonusCount < SleepDebtAbility.MaxPermanentBonus
                    && SleepDebtAbility.PermanentRphBonusEffect != null)
                {
                    var spec = Owner.MakeOutgoingSpec(this, SleepDebtAbility.PermanentRphBonusEffect);
                    Owner.ApplyGameplayEffectSpecToSelf(spec);
                    _permanentBonusCount++;
                    Debug.Log($"[SleepDebt] Perfect 2 hits — permanent RPH bonus #{_permanentBonusCount}.");
                }
            }
            else if (_hitsThisTurn > SleepDebtAbility.TargetHitCount)
            {
                // Over 2 hits: lose extra HP for each hit beyond target
                int extraHits = _hitsThisTurn - SleepDebtAbility.TargetHitCount;
                for (int i = 0; i < extraHits; i++)
                {
                    if (SleepDebtAbility.ExtraHpLossEffect == null)
                        break;

                    var spec = Owner.MakeOutgoingSpec(this, SleepDebtAbility.ExtraHpLossEffect);
                    Owner.ApplyGameplayEffectSpecToSelf(spec);
                }

                Debug.Log($"[SleepDebt] Over-hit by {extraHits} — extra HP loss applied.");
            }

            _hitsThisTurn = 0;
        }
    }
}
