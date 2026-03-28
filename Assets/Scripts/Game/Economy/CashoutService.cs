using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.BoardSystem;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.Game.Economy
{
    /// <summary>
    /// Executes player-initiated cashout of a placed company during the Offer Phase.
    ///
    /// TryCashout:
    ///   1. Validates the company is alive (not PendingCollapse, not already destroying).
    ///   2. Calculates payout: CompanyValuationModel.CashoutValue.
    ///   3. Credits payout to the player's Balance GAS attribute (same pattern as economy resolution).
    ///   4. Removes the company from the board via BoardItemPropertySpec_Destroyable.
    ///   5. Emits CompanyCashedOutEvent.
    ///
    /// Note: preventing re-offer of the cashed-out company requires CompanySelectionPile to support
    /// a discard/exclude list. A stub comment marks that integration point.
    /// TODO(spec-005): When spec 005 merges, wire CashedOutCompanyIds into RunCompanyPool exclusion list.
    /// </summary>
    public sealed class CashoutService
    {
        private const string BalanceAttributeName = "Balance";

        private readonly Turn _turn;

        public CashoutService(Turn turn)
        {
            _turn = turn;
        }

        /// <summary>
        /// Attempt to cash out the given company wrapper.
        /// Returns true and credits balance if successful; false with a log if not.
        /// </summary>
        public bool TryCashout(BoardItemWrapper_Company companyWrapper)
        {
            if (companyWrapper == null)
            {
                Debug.LogError("[spec-006][CashoutService] TryCashout: companyWrapper is null.");
                return false;
            }

            var healthState = companyWrapper.HealthState;
            var valuationModel = companyWrapper.ValuationModel;

            if (healthState == null)
            {
                Debug.LogError(
                    $"[spec-006][CashoutService] TryCashout: '{companyWrapper.name}' has no HealthState. " +
                    "Was it initialized via WrapCore?");
                return false;
            }

            if (healthState.PendingCollapse)
            {
                Debug.LogWarning(
                    $"[spec-006][CashoutService] TryCashout: '{companyWrapper.name}' is already PendingCollapse. " +
                    "Cannot cashout a doomed company.");
                return false;
            }

            if (companyWrapper.BoardItem == null)
            {
                Debug.LogError(
                    $"[spec-006][CashoutService] TryCashout: '{companyWrapper.name}' has no BoardItem.");
                return false;
            }

            if (!companyWrapper.BoardItem.TryGetPropertySpec(
                    out BoardItemPropertySpec_Destroyable destroyableSpec))
            {
                Debug.LogError(
                    $"[spec-006][CashoutService] TryCashout: '{companyWrapper.name}' missing Destroyable spec.");
                return false;
            }

            if (destroyableSpec.IsDestroying)
            {
                Debug.LogWarning(
                    $"[spec-006][CashoutService] TryCashout: '{companyWrapper.name}' is already being destroyed.");
                return false;
            }

            // Capture identity before destroy.
            string companyId = companyWrapper.Company?.CompanyId?.CompanyId ?? string.Empty;
            var boardPosition = new UnityEngine.Vector2Int(
                companyWrapper.BoardItem.BoardItemData.Col,
                companyWrapper.BoardItem.BoardItemData.Row);

            float payoutAmount = valuationModel?.CashoutValue ?? 0f;

            // Credit payout to player balance.
            CreditPlayerBalance(payoutAmount);

            // Remove from board (investment is returned as cashout payout).
            destroyableSpec.Destroy(null);

            // TODO(spec-005): Exclude companyId from RunCompanyPool re-offer list when spec 005 merges.
            Debug.Log(
                $"[spec-006][CashoutService] '{companyId}' cashed out at {boardPosition}. " +
                $"Payout={payoutAmount}.");

            // Emit cashout event.
            EventBus<CompanyCashedOutEvent>.Raise(
                new CompanyCashedOutEvent(companyId, payoutAmount, boardPosition));

            return true;
        }

        /// <summary>
        /// Adds <paramref name="amount"/> to the player's Balance GAS attribute.
        /// Mirrors the pattern used in Turn.ApplyTurnlyCosts() but applies a positive modifier.
        /// </summary>
        private void CreditPlayerBalance(float amount)
        {
            if (amount <= 0f)
                return;

            if (_turn?.Player?.AbilitySystemCharacter?.AttributeSystem == null)
            {
                Debug.LogError(
                    "[spec-006][CashoutService] CreditPlayerBalance: player AttributeSystem is null.");
                return;
            }

            var attributeSystem = _turn.Player.AbilitySystemCharacter.AttributeSystem;

            if (!attributeSystem.AttributeSet.TryGetAttributeByName(
                    BalanceAttributeName,
                    out AttributeScriptableObject balanceAttribute))
            {
                Debug.LogError(
                    $"[spec-006][CashoutService] CreditPlayerBalance: '{BalanceAttributeName}' attribute not found.");
                return;
            }

            attributeSystem.ModifyBaseValue(
                balanceAttribute,
                new AttributeModifier { Add = amount },
                out _);

            Debug.Log($"[spec-006][CashoutService] Credited {amount} to player Balance.");
        }
    }
}
