using Pinvestor.BoardSystem.Authoring;

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
        /// <param name="currentValue">Payout after previous modifiers.</param>
        /// <returns>The adjusted payout.</returns>
        float ModifyCashoutValue(float currentValue);
    }
}
