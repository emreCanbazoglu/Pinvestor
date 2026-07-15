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
    /// TrendNecro Agency — when an adjacent company collapses, gain 1 "Recycled Hype" stack;
    /// next cashout from this company is doubled (stack cap 1, consumed on cashout).
    ///
    /// Wired into the cashout pipeline: the spec implements
    /// <see cref="Pinvestor.Game.Health.ICashoutValueModifier"/>; CashoutService
    /// consumes the stack and doubles the payout when this company cashes out.
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

    public class TrendNecroAbilitySpec : AbstractAbilitySpec, Pinvestor.Game.Health.ICashoutValueModifier
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
            GameEventLog.Add("ABILITY", $"[TrendNecro] Adjacent collapse → Recycled Hype ×{RecycledHypeStacks} (next cashout doubled)", new UnityEngine.Color(0.9f, 0.6f, 1f));
        }

        /// <summary>
        /// Consumes the stack and returns whether the cashout should be doubled.
        /// </summary>
        public bool TryConsumeCashoutDouble()
        {
            if (RecycledHypeStacks <= 0)
                return false;

            RecycledHypeStacks--;
            return true;
        }

        /// <summary>
        /// Cashout pipeline hook: doubles the payout if a Recycled Hype stack is available.
        /// </summary>
        public float ModifyCashoutValue(float currentValue)
        {
            if (!TryConsumeCashoutDouble())
                return currentValue;

            GameEventLog.Add(
                "ABILITY",
                $"[TrendNecro] Recycled Hype consumed — cashout doubled ({currentValue} → {currentValue * 2f})",
                new UnityEngine.Color(0.9f, 0.6f, 1f));

            return currentValue * 2f;
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
