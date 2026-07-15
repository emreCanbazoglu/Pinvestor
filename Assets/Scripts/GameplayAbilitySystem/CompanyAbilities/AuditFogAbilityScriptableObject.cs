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
    /// Wired into the shared collapse handler: the spec implements
    /// <see cref="Pinvestor.Game.Health.ICollapseInterceptor"/>; CollapseResolver
    /// consults it during the Resolution Phase and defers the intercepted collapse
    /// until round end.
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

    public class AuditFogAbilitySpec : AbstractAbilitySpec, Pinvestor.Game.Health.ICollapseInterceptor
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

            GameEventLog.Add("ABILITY+", "[AuditFog] Active — first collapse each round is hidden until round end", new UnityEngine.Color(0.6f, 0.9f, 0.6f));

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
        /// Collapse handler hook: hides the first collapse each round.
        /// The collapsing company stays on the board (still generating revenue)
        /// until CollapseResolver flushes deferred collapses at round end.
        /// </summary>
        public bool TryInterceptCollapse(BoardItemWrapper_Company collapsingCompany)
        {
            if (_hiddenCollapseUsedThisRound)
                return false;

            _hiddenCollapseUsedThisRound = true;
            GameEventLog.Add(
                "ABILITY",
                $"[AuditFog] Collapse of {collapsingCompany.Company?.CompanyId?.CompanyId} hidden until round end",
                new UnityEngine.Color(0.6f, 0.6f, 1f));
            return true;
        }
    }
}
