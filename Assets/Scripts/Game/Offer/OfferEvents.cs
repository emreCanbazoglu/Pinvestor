using System.Collections.Generic;
using Pinvestor.GameConfigSystem;

namespace Pinvestor.Game.Offer
{
    /// <summary>
    /// Raised by the turn phase to open the offer panel and pass the context.
    /// The UI panel listens to this to populate itself.
    /// </summary>
    public class ShowCompanyOfferPanelEvent : IEvent
    {
        public OfferPhaseContext Context { get; }

        /// <summary>
        /// The active turn raising this offer. Gives UI access to the board and
        /// CashoutService for the portfolio/cashout section. May be null in tests.
        /// </summary>
        public Turn Turn { get; }

        public ShowCompanyOfferPanelEvent(
            OfferPhaseContext context,
            Turn turn = null)
        {
            Context = context;
            Turn = turn;
        }
    }

    /// <summary>
    /// Raised by the turn phase after the offer selection has been confirmed
    /// and the offer panel should be hidden.
    /// </summary>
    public class HideCompanyOfferPanelEvent : IEvent { }
}
