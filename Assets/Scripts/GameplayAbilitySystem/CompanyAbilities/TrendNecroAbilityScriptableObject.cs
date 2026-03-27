using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// TrendNecro Agency — when an adjacent company collapses, gain 1 "Recycled Hype" stack;
    /// next cashout from this company is doubled (stack cap 1, consumed on cashout).
    ///
    /// Note: cashout doubling depends on spec 006 cashout system.
    /// Stack tracking is implemented; cashout multiplier is stubbed.
    /// TODO(spec-006): cashout doubling — wire RecycledHypeStack into cashout value modifier.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Pinvestor/Ability System/Company Abilities/TrendNecro Ability",
        fileName = "Ability.Company.TrendNecro.asset")]
    public class TrendNecroAbilityScriptableObject : AbstractAbilityScriptableObject
    {
        [field: SerializeField] public int MaxStacks { get; private set; } = 1;

        public override AbstractAbilitySpec CreateSpec(
            AbilitySystemCharacter owner,
            float? level = default)
        {
            return new TrendNecroAbilitySpec(this, owner);
        }
    }

    public class TrendNecroAbilitySpec : AbstractAbilitySpec
    {
        private TrendNecroAbilityScriptableObject TrendNecroAbility
            => (TrendNecroAbilityScriptableObject)Ability;

        private BoardItemWrapper_Company _selfWrapper;

        /// <summary>Recycled Hype stack count. Max 1 per design.</summary>
        public int RecycledHypeStacks { get; private set; }

        public TrendNecroAbilitySpec(
            AbstractAbilityScriptableObject abilitySO,
            AbilitySystemCharacter owner) : base(abilitySO, owner)
        {
            _selfWrapper = owner.GetComponent<BoardItemWrapper_Company>();
        }

        protected override IEnumerator<float> ActivateAbility()
        {
            GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved += OnBoardItemRemoved;

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.BoardWrapper.Board.OnBoardItemRemoved -= OnBoardItemRemoved;

            base.CancelAbility();
        }

        private void OnBoardItemRemoved(BoardItemBase boardItem)
        {
            if (!(boardItem is BoardItem_Company companyItem))
                return;

            if (!IsAdjacentTo(companyItem))
                return;

            if (RecycledHypeStacks >= TrendNecroAbility.MaxStacks)
                return;

            RecycledHypeStacks++;
            Debug.Log($"[TrendNecro] Adjacent company collapsed. RecycledHypeStacks={RecycledHypeStacks}. " +
                      $"TODO(spec-006): apply cashout doubling modifier on next cashout.");
        }

        /// <summary>
        /// Called by spec-006 cashout system when this company cashs out.
        /// Consumes the stack and returns whether the cashout should be doubled.
        /// TODO(spec-006): wire this into cashout value modifier.
        /// </summary>
        public bool TryConsumeCashoutDouble()
        {
            if (RecycledHypeStacks <= 0)
                return false;

            RecycledHypeStacks--;
            return true;
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
