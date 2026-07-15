using Pinvestor.BoardSystem.Authoring;
using System.Collections.Generic;

namespace Pinvestor.Game.Health
{
    /// <summary>
    /// Implemented by ability specs that can intercept a company collapse during the
    /// Resolution Phase. Interceptors on every active board company are consulted for
    /// every collapsing company; the first one to return true defers that collapse to
    /// the end of the current round (the company stays on the board until then).
    /// </summary>
    public interface ICollapseInterceptor
    {
        /// <param name="collapsingCompany">The company about to be removed (may be the owner itself).</param>
        /// <returns>True to defer this collapse until round end.</returns>
        bool TryInterceptCollapse(BoardItemWrapper_Company collapsingCompany);
    }

    /// <summary>
    /// Implemented by ability specs that modify their own company's cashout payout.
    /// Called by CashoutService when the owning company is cashed out; modifiers are
    /// applied in GrantedAbilities order.
    /// </summary>
    public interface ICashoutValueModifier
    {
        /// <param name="cashingOutCompany">The company whose payout is being calculated.</param>
        /// <param name="currentValue">Payout after previous modifiers.</param>
        /// <returns>The adjusted payout.</returns>
        float ModifyCashoutValue(
            BoardItemWrapper_Company cashingOutCompany,
            float currentValue);
    }

    /// <summary>
    /// Deterministic cashout-modifier composition shared by runtime code and tests.
    /// </summary>
    public static class CashoutModifierPipeline
    {
        public static float Apply(
            IEnumerable<ICashoutValueModifier> modifiers,
            BoardItemWrapper_Company cashingOutCompany,
            float payoutAmount)
        {
            if (modifiers == null)
                return payoutAmount;

            foreach (ICashoutValueModifier modifier in modifiers)
            {
                if (modifier == null)
                    continue;

                payoutAmount = modifier.ModifyCashoutValue(
                    cashingOutCompany,
                    payoutAmount);
            }

            return payoutAmount;
        }
    }
}
