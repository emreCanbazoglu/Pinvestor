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
    /// The move uses Board.TryMoveBoardItem once the collapsed cell is vacated
    /// (pieces dispose after OnBoardItemRemoved fires, so the move is attempted on
    /// subsequent frames with a short retry budget). The free hit payout is applied
    /// via the FreeHitEffect immediately on detection.
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

        // Pending relocation into an adjacent collapsed tile. The cell is only
        // vacated after the collapsed item's pieces dispose, so the move is retried
        // across frames from the ability loop until it succeeds or the budget runs out.
        private Vector2Int? _pendingMoveTarget;
        private int _pendingMoveFramesLeft;
        private const int MoveRetryFrameBudget = 120;

        private EventBinding<RoundStartedEvent> _roundBinding;

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
            EventBus<RoundStartedEvent>.Register(_roundBinding);

            GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved += OnBoardItemRemoved;

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
                _selfWrapper.transform.localPosition = Vector3.zero;
                GameEventLog.Add(
                    "ABILITY",
                    $"[LastMile] Moved into collapsed tile {target}",
                    new UnityEngine.Color(0.4f, 1f, 0.7f));
                return;
            }

            _pendingMoveFramesLeft--;
            if (_pendingMoveFramesLeft <= 0)
            {
                GameEventLog.Add(
                    "ABILITY",
                    $"[LastMile] Move to {target} abandoned — tile never became available",
                    new UnityEngine.Color(1f, 0.7f, 0.4f));
                _pendingMoveTarget = null;
            }
        }

        public override void CancelAbility()
        {
            EventBus<RoundStartedEvent>.Deregister(_roundBinding);

            if (GameManager.Instance != null)
                GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved -= OnBoardItemRemoved;

            base.CancelAbility();
        }

        private void OnBoardItemRemoved(BoardItemBase boardItem)
        {
            if (_procUsedThisRound)
                return;

            if (!(boardItem is BoardItem_Company collapsedCompany))
                return;

            if (!IsAdjacentTo(collapsedCompany))
                return;

            _procUsedThisRound = true;

            // Capture the vacating tile now — the cell reference is gone once the
            // collapsed item's pieces dispose. The actual move runs from the ability
            // loop on subsequent frames (see TryExecutePendingMove).
            Cell collapsedCell = collapsedCompany.MainPiece?.Cell;
            if (collapsedCell != null)
            {
                _pendingMoveTarget = collapsedCell.Position;
                _pendingMoveFramesLeft = MoveRetryFrameBudget;
            }

            GameEventLog.Add("ABILITY", $"[LastMile] Adjacent collapse at {collapsedCell?.Position} — free payout triggered, relocating", new UnityEngine.Color(0.4f, 1f, 0.7f));

            if (LastMileAbility.FreeHitPayoutEffect != null)
            {
                var spec = Owner.MakeOutgoingSpec(this, LastMileAbility.FreeHitPayoutEffect);
                Owner.ApplyGameplayEffectSpecToSelf(spec);
            }
        }

        private void OnRoundStarted(RoundStartedEvent _)
        {
            _procUsedThisRound = false;
        }

        private bool IsAdjacentTo(BoardItem_Company other)
        {
            if (_selfWrapper?.BoardItem?.MainPiece?.Cell == null)
                return false;

            var otherCell = other.MainPiece?.Cell;
            if (otherCell == null)
                return false;

            return _selfWrapper.BoardItem.MainPiece.Cell.IsLinkedCell(otherCell);
        }
    }
}
