using AttributeSystem.Authoring;
using AttributeSystem.Components;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.Game.Economy
{
    /// <summary>
    /// Credits accumulated turn revenue to the CardPlayer's Balance attribute via GAS.
    /// Op-cost deduction is handled exclusively by Turn.ApplyTurnlyCosts() — do not
    /// deduct op-costs here.
    /// </summary>
    public sealed class EconomyService
    {
        private readonly TurnRevenueAccumulator _accumulator;

        private const string BalanceAttributeName = "Balance";

        public EconomyService(TurnRevenueAccumulator accumulator)
        {
            _accumulator = accumulator;
        }

        /// <summary>
        /// Credits accumulated turn revenue to the CardPlayer's GAS Balance attribute.
        /// Called from Turn.RunResolutionPhase() after the Launch Phase.
        /// Op-costs are NOT deducted here — ApplyTurnlyCosts() in Turn.cs handles that.
        /// </summary>
        public void ApplyResolution(CardPlayer cardPlayer)
        {
            if (cardPlayer == null || cardPlayer.AbilitySystemCharacter == null)
            {
                Debug.LogWarning("[EconomyService] CardPlayer or AbilitySystemCharacter is null. Skipping revenue credit.");
                return;
            }

            AttributeSystemComponent attributeSystem
                = cardPlayer.AbilitySystemCharacter.AttributeSystem;
            if (attributeSystem == null || attributeSystem.AttributeSet == null)
            {
                Debug.LogWarning("[EconomyService] AttributeSystemComponent or AttributeSet is null. Skipping revenue credit.");
                return;
            }

            if (!attributeSystem.AttributeSet.TryGetAttributeByName(
                    BalanceAttributeName,
                    out AttributeScriptableObject balanceAttribute))
            {
                Debug.LogWarning($"[EconomyService] Balance attribute '{BalanceAttributeName}' not found. Skipping revenue credit.");
                return;
            }

            float totalRevenue = _accumulator.GetTotalTurnRevenue();

            if (!attributeSystem.TryGetAttributeValue(balanceAttribute, out AttributeValue balanceBefore))
            {
                Debug.LogWarning("[EconomyService] Could not read Balance attribute value. Skipping revenue credit.");
                return;
            }

            float worthBefore = balanceBefore.CurrentValue;

            if (totalRevenue > 0f)
            {
                attributeSystem.ModifyBaseValue(
                    balanceAttribute,
                    new AttributeModifier { Add = totalRevenue },
                    out _);
            }

            attributeSystem.TryGetAttributeValue(balanceAttribute, out AttributeValue balanceAfter);
            float worthAfter = balanceAfter.CurrentValue;

            Debug.Log(
                $"[EconomyService] Revenue credited: turnRevenue={totalRevenue}, " +
                $"balance {worthBefore} → {worthAfter}");
        }
    }
}
