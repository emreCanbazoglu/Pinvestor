using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinvestor.Game.Offer
{
    /// <summary>
    /// Tracks the available, placed, and discarded companies for a single run.
    /// Initialized once at run start from the full GameConfig company list.
    /// Persists across turns within a run and is cleared on run end.
    /// </summary>
    public class RunCompanyPool
    {
        private readonly List<string> _available = new List<string>();
        private readonly HashSet<string> _placed = new HashSet<string>();
        private readonly HashSet<string> _discarded = new HashSet<string>();

        public IReadOnlyList<string> Available => _available;
        public IReadOnlyCollection<string> Placed => _placed;
        public IReadOnlyCollection<string> Discarded => _discarded;

        /// <summary>
        /// Builds the pool from the provided company IDs. Clears any prior state.
        /// </summary>
        public void Initialize(IEnumerable<string> allCompanyIds)
        {
            _available.Clear();
            _placed.Clear();
            _discarded.Clear();

            foreach (string id in allCompanyIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _available.Add(id);
            }

            Debug.Log($"[RunCompanyPool] Initialized with {_available.Count} companies.");
        }

        /// <summary>
        /// Removes the company from available and records it as placed.
        /// Called when the player confirms placement on the board.
        /// </summary>
        public void MarkPlaced(string companyId)
        {
            if (string.IsNullOrWhiteSpace(companyId))
                return;

            _available.Remove(companyId);
            _placed.Add(companyId);

            Debug.Log($"[RunCompanyPool] Marked placed: {companyId}. Available: {_available.Count}");
        }

        /// <summary>
        /// Removes the company from available and records it as discarded.
        /// Called for the 2 unselected offer cards at the end of each offer phase.
        /// </summary>
        public void MarkDiscarded(string companyId)
        {
            if (string.IsNullOrWhiteSpace(companyId))
                return;

            _available.Remove(companyId);
            _discarded.Add(companyId);

            Debug.Log($"[RunCompanyPool] Marked discarded: {companyId}. Available: {_available.Count}");
        }

        /// <summary>
        /// Returns true if the company is currently in the available pool.
        /// </summary>
        public bool IsAvailable(string companyId)
        {
            return _available.Contains(companyId);
        }

        /// <summary>
        /// Clears all state. Call on run end to prevent stale state in the next run.
        /// </summary>
        public void Clear()
        {
            int count = _available.Count + _placed.Count + _discarded.Count;
            _available.Clear();
            _placed.Clear();
            _discarded.Clear();

            Debug.Log($"[RunCompanyPool] Cleared. Released {count} companies.");
        }
    }
}
