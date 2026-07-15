using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.Game
{
    /// <summary>
    /// Emitted once when a company is logically recognized as collapsed.
    /// Physical removal normally happens immediately, but an interceptor such as
    /// AuditFog may defer removal until round end. Investment is never refunded.
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
