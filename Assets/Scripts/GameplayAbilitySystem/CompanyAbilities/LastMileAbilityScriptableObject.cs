using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using Pinvestor.Game.BallSystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// LastMile Orchestrator — when an adjacent company collapses, move this company into
    /// the collapsed tile and trigger one free hit payout (once per round).
    ///
    /// TODO(spec-006): collapse handler — full implementation requires spec-006 collapse
    /// handler to intercept collapse order and allow position swapping.
    /// Current implementation: detects adjacent collapse and logs movement intent.
    /// The free hit payout is applied via the FreeHitEffect.
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
    }

    public class LastMileAbilitySpec : AbstractAbilitySpec
    {
        private LastMileAbilityScriptableObject LastMileAbility
            => (LastMileAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;
        private bool _procUsedThisRound;

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
                yield return MEC.Timing.WaitForOneFrame;
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

            // TODO(spec-006): collapse handler — move this company into the collapsed tile.
            // For now, log intent and apply free payout.
            Debug.Log($"[LastMile] Adjacent company collapsed. " +
                      $"TODO(spec-006): move into collapsed tile at {collapsedCompany.MainPiece?.Cell?.Position}.");

            if (LastMileAbility.FreeHitPayoutEffect != null)
            {
                var spec = Owner.MakeOutgoingSpec(this, LastMileAbility.FreeHitPayoutEffect);
                Owner.ApplyGameplayEffectSpecToSelf(spec);
                Debug.Log("[LastMile] Free hit payout applied.");
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
