using UnityEngine;

namespace Pinvestor.Game.Economy
{
    /// <summary>
    /// Singleton runtime model holding the player's economy state for the current run.
    /// Exposed as read-only to all systems; only EconomyService writes to it.
    /// </summary>
    public sealed class PlayerEconomyState : Singleton<PlayerEconomyState>
    {
        // ── Read-only API ──────────────────────────────────────────────────────
        public float NetWorth { get; private set; }
        public float InitialCapital { get; private set; }
        public float LastTurnRevenue { get; private set; }
        public float LastTurnOpCost { get; private set; }
        public bool IsInitialized { get; private set; }

        // ── Write API (package-internal; only EconomyService calls these) ─────

        internal void Initialize(float initialCapital)
        {
            InitialCapital = initialCapital;
            NetWorth = initialCapital;
            LastTurnRevenue = 0f;
            LastTurnOpCost = 0f;
            IsInitialized = true;

            Debug.Log(
                $"[PlayerEconomyState] Initialized: initialCapital={initialCapital}");
        }

        internal void ApplyResolutionDelta(float revenue, float opCost)
        {
            LastTurnRevenue = revenue;
            LastTurnOpCost = opCost;

            float worthBefore = NetWorth;
            NetWorth += revenue - opCost;

            Debug.Log(
                $"[PlayerEconomyState] ApplyResolutionDelta: revenue={revenue}, opCost={opCost}, " +
                $"netWorth {worthBefore} → {NetWorth}");
        }

        protected override void OnDestroyCore()
        {
            IsInitialized = false;
        }
    }
}
