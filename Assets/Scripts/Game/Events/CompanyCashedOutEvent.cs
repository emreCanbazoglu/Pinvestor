using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.Game
{
    /// <summary>
    /// Emitted when the player proactively cashes out a company during the Offer Phase.
    /// The payout amount has already been credited to the player's Balance attribute.
    /// </summary>
    public sealed class CompanyCashedOutEvent : IEvent
    {
        /// <summary>Company identifier string from the company config.</summary>
        public string CompanyId { get; }

        /// <summary>Amount credited to the player's balance as cashout payout.</summary>
        public float PayoutAmount { get; }

        /// <summary>Board position the company occupied at cashout time.</summary>
        public Vector2Int BoardPosition { get; }

        public CompanyCashedOutEvent(
            string companyId,
            float payoutAmount,
            Vector2Int boardPosition)
        {
            CompanyId = companyId;
            PayoutAmount = payoutAmount;
            BoardPosition = boardPosition;
        }
    }
}
