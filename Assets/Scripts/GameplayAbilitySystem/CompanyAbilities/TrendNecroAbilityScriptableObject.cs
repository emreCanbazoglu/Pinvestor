using System.Collections.Generic;
using AbilitySystem;
using AbilitySystem.Authoring;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Diagnostics;
using Pinvestor.Game;
using Pinvestor.CompanySystem;
using UnityEngine;

namespace Pinvestor.GameplayAbilitySystem.Abilities
{
    /// <summary>
    /// TrendNecro Agency — when an adjacent company collapses, gain 1 "Recycled Hype" stack;
    /// next eligible SocialMedia cashout is doubled (stack cap 1, consumed on cashout).
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

        private EventBinding<CompanyCollapsedEvent> _collapseBinding;

        protected override IEnumerator<float> ActivateAbility()
        {
            _collapseBinding = new EventBinding<CompanyCollapsedEvent>(OnCompanyCollapsed);
            EventBus<CompanyCollapsedEvent>.Register(_collapseBinding);

            while (true)
            {
                yield return MEC.Timing.WaitForOneFrame;
            }
        }

        public override void CancelAbility()
        {
            EventBus<CompanyCollapsedEvent>.Deregister(_collapseBinding);

            base.CancelAbility();
        }

        private void OnCompanyCollapsed(CompanyCollapsedEvent e)
        {
            if (!IsAdjacentTo(e.BoardPosition))
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
        public float ModifyCashoutValue(
            BoardItemWrapper_Company cashingOutCompany,
            float currentValue)
        {
            string companyId = cashingOutCompany?.Company?.CompanyId?.CompanyId;
            if (CompanyCategoryResolver.ResolveOrNone(companyId)
                != ECompanyCategory.SocialMedia)
                return currentValue;

            if (!TryConsumeCashoutDouble())
                return currentValue;

            GameEventLog.Add(
                "ABILITY",
                $"[TrendNecro] Recycled Hype consumed — cashout doubled ({currentValue} → {currentValue * 2f})",
                new UnityEngine.Color(0.9f, 0.6f, 1f));

            return currentValue * 2f;
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
