using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Diagnostics;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// LastMile Orchestrator — when an adjacent company collapses, move this company into
    /// the collapsed tile and trigger one free hit payout (once per round).
    ///
    /// The move uses Board.TryMoveBoardItem once the collapsed cell is vacant. AuditFog
    /// may keep the physical item there until round end, so the target remains pending
    /// without a frame-count timeout. The free payout fires when collapse is recognized.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/LastMile Ability",
        fileName = "Ability.Company.LastMile.asset")]
    public class LastMileAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public GameplayEffectScriptableObject FreeHitPayoutEffect { get; private set; } = null;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new LastMileAbilitySpec(this, owner);
        }

        protected override IEnumerable<GameplayEffectScriptableObject> GetDescriptiveGameplayEffects()
        {
            if (FreeHitPayoutEffect != null) yield return FreeHitPayoutEffect;
        }
    }

    public class LastMileAbilitySpec : AbstractAbilitySpec
    {
        private LastMileAbilityScriptableObject LastMileAbility
            => (LastMileAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;
        private bool _procUsedThisRound;

        // Pending relocation into a logically collapsed tile. AuditFog can defer
        // physical removal until round end, so this is intentionally not frame-limited.
        private Vector2Int? _pendingMoveTarget;

        private EventBinding<RoundStartedEvent> _roundBinding;
        private EventBinding<CompanyCollapsedEvent> _collapseBinding;

        public LastMileAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            _procUsedThisRound = false;

            _roundBinding = new EventBinding<RoundStartedEvent>(OnRoundStarted);
            _collapseBinding = new EventBinding<CompanyCollapsedEvent>(OnCompanyCollapsed);
            EventBus<RoundStartedEvent>.Register(_roundBinding);
            EventBus<CompanyCollapsedEvent>.Register(_collapseBinding);

            while (true)
            {
                TryExecutePendingMove();
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        private void TryExecutePendingMove()
        {
            if (_pendingMoveTarget == null)
                return;

            Vector2Int target = _pendingMoveTarget.Value;
            var board = GameManager.Instance != null
                ? GameManager.Instance.BoardWrapper?.Board
                : null;

            if (board != null
                && _selfWrapper?.BoardItem != null
                && board.TryMoveBoardItem(_selfWrapper.BoardItem, target))
            {
                _pendingMoveTarget = null;
                GameEventLog.Add(
                    "ABILITY",
                    $"[LastMile] Moved into collapsed tile {target}",
                    new UnityEngine.Color(0.4f, 1f, 0.7f));
                return;
            }

        }

        public override void CancelAbility()
        {
            EventBus<RoundStartedEvent>.Deregister(_roundBinding);
            EventBus<CompanyCollapsedEvent>.Deregister(_collapseBinding);

            base.CancelAbility();
        }

        private void OnCompanyCollapsed(CompanyCollapsedEvent e)
        {
            if (_procUsedThisRound)
                return;

            if (!IsAdjacentTo(e.BoardPosition))
                return;

            _procUsedThisRound = true;

            _pendingMoveTarget = e.BoardPosition;

            GameEventLog.Add("ABILITY", $"[LastMile] Adjacent collapse at {e.BoardPosition} — free payout triggered, relocating", new UnityEngine.Color(0.4f, 1f, 0.7f));

            if (LastMileAbility.FreeHitPayoutEffect != null)
            {
                var spec = Owner.MakeOutgoingSpec(this, LastMileAbility.FreeHitPayoutEffect);
                Owner.ApplyGameplayEffectSpecToSelf(spec);
            }
        }

        private void OnRoundStarted(RoundStartedEvent _)
        {
            // AuditFog flushes its deferred physical removal on RoundCompleted.
            // The next RoundStarted can fire before the ability loop advances a frame,
            // so make one immediate move attempt before treating the target as stale.
            TryExecutePendingMove();

            if (_pendingMoveTarget != null)
            {
                Debug.LogWarning(
                    $"[LastMile] Pending move to {_pendingMoveTarget.Value} " +
                    "did not resolve before the next round.");
                _pendingMoveTarget = null;
            }

            _procUsedThisRound = false;
        }

        private bool IsAdjacentTo(Vector2Int boardPosition)
        {
            if (_selfWrapper?.BoardItem?.MainPiece?.Cell == null)
                return false;

            var board = _selfWrapper.BoardItem.MainPiece.Cell.Board;
            if (!board.TryGetCellAt(boardPosition, out Cell collapsedCell))
                return false;

            return _selfWrapper.BoardItem.MainPiece.Cell.IsLinkedCell(collapsedCell);
        }
    }
}
