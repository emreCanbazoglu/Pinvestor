using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// Loophole Ledger — once per turn, nullify the first negative market/news modifier
    /// targeting this company.
    ///
    /// Implementation: applies a GE that sets a tag on the owning ASC; market/news systems
    /// should check for this tag before applying negative modifiers, and consume it on first use.
    /// This is a passive shield effect that is reset each turn.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/LoopholeLedger Ability",
        fileName = "Ability.Company.LoopholeLedger.asset")]
    public class LoopholeLedgerAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField]
        public GameplayEffectScriptableObject NegativeModifierShieldEffect { get; private set; } = null;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new LoopholeLedgerAbilitySpec(this, owner);
        }
    }

    public class LoopholeLedgerAbilitySpec : AbstractAbilitySpec
    {
        private LoopholeLedgerAbilityScriptableObject LoopholeLedgerAbility
            => (LoopholeLedgerAbilityScriptableObject)Ability;

        private GameplayEffectContainer _shieldContainer;
        private bool _shieldApplied;

        private EventBinding<TurnResolutionStartedEvent> _turnResetBinding;

        public LoopholeLedgerAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            _turnResetBinding = new EventBinding<TurnResolutionStartedEvent>(OnTurnReset);
            EventBus<TurnResolutionStartedEvent>.Register(_turnResetBinding);

            ApplyShield();

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            EventBus<TurnResolutionStartedEvent>.Deregister(_turnResetBinding);
            RemoveShield();
            base.CancelAbility();
        }

        private void ApplyShield()
        {
            if (_shieldApplied || LoopholeLedgerAbility.NegativeModifierShieldEffect == null)
                return;

            var spec = Owner.MakeOutgoingSpec(this, LoopholeLedgerAbility.NegativeModifierShieldEffect);
            _shieldContainer = Owner.ApplyGameplayEffectSpecToSelf(spec);
            _shieldApplied = true;
        }

        private void RemoveShield()
        {
            if (!_shieldApplied)
                return;

            Owner.RemoveGameplayEffectSpecFromSelf(_shieldContainer);
            _shieldApplied = false;
        }

        private void OnTurnReset(TurnResolutionStartedEvent _)
        {
            // Remove and re-apply to refresh the once-per-turn shield each turn.
            RemoveShield();
            ApplyShield();
        }
    }
}
