using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.Game
{
    /// <summary>
    /// Emitted when a company collapses (HP reaches 0) and is removed from the board.
    /// Investment capital is NOT refunded when this fires.
    /// </summary>
    public sealed class CompanyCollapsedEvent : IEvent
    {
        /// <summary>Company identifier string from the company config.</summary>
        public string CompanyId { get; }

        /// <summary>Board position the company occupied when it collapsed.</summary>
        public Vector2Int BoardPosition { get; }

        public CompanyCollapsedEvent(
            string companyId,
            Vector2Int boardPosition)
        {
            CompanyId = companyId;
            BoardPosition = boardPosition;
        }
    }
}
