using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Diagnostics;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// AuditFog Exchange — first collapse each round is hidden until round end;
    /// hidden company still generates revenue during its turn.
    ///
    /// TODO(spec-006): collapse handler — requires spec-006 collapse system to intercept
    /// the collapse event and defer it. This stub logs the intent and tracks state
    /// but does not modify collapse behavior.
    /// TODO(spec-006): hidden collapse — wire into collapse handler.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/AuditFog Ability",
        fileName = "Ability.Company.AuditFog.asset")]
    public class AuditFogAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new AuditFogAbilitySpec(this, owner);
        }
    }

    public class AuditFogAbilitySpec : AbstractAbilitySpec
    {
        private bool _hiddenCollapseUsedThisRound;

        private EventBinding<RoundStartedEvent> _roundBinding;

        public AuditFogAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            _hiddenCollapseUsedThisRound = false;

            _roundBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            EventBus<RoundStartedEvent>.Register(_roundBinding);

            // TODO(spec-006): collapse handler — subscribe to collapse events from spec-006
            // and intercept the first collapse per round, hiding it until round end.
            GameEventLog.Add("ABILITY+", "[AuditFog] Active — awaiting spec-006 collapse handler", new UnityEngine.Color(0.6f, 0.9f, 0.6f));

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            EventBus<RoundStartedEvent>.Deregister(_roundBinding);
            base.CancelAbility();
        }

        private void OnRoundStarted(RoundStartedEvent _)
        {
            _hiddenCollapseUsedThisRound = false;
        }

        /// <summary>
        /// Called by spec-006 collapse handler when a company collapses.
        /// Returns true if the collapse should be hidden until round end.
        /// TODO(spec-006): wire this into the collapse handler.
        /// </summary>
        public bool TryHideCollapse(BoardItemBase collapsingItem)
        {
            if (_hiddenCollapseUsedThisRound)
                return false;

            _hiddenCollapseUsedThisRound = true;
            GameEventLog.Add("ABILITY", $"[AuditFog] Hidden collapse for {collapsingItem} (TODO spec-006: defer until round end)", new UnityEngine.Color(0.6f, 0.6f, 1f));
            return true;
        }
    }
}
