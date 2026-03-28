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

        public ShowCompanyOfferPanelEvent(OfferPhaseContext context)
        {
            Context = context;
        }
    }

    /// <summary>
    /// Raised by the turn phase after the offer selection has been confirmed
    /// and the offer panel should be hidden.
    /// </summary>
    public class HideCompanyOfferPanelEvent : IEvent { }
}
