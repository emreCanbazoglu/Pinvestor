using System;
using System.Collections.Generic;
using AbilitySystem;
using Pinvestor.RevenueGeneratorSystem.Core;
using UnityEngine;

namespace Pinvestor.Game.Economy
{
    /// <summary>
    /// Accumulates hit-revenue for a single turn.
    /// Subscribe at turn start; unsubscribe after Launch Phase ends.
    /// Reset at the start of each turn to prevent cross-turn leakage.
    /// </summary>
    public sealed class TurnRevenueAccumulator
    {
        // Maps each subscribed generator -> accumulated revenue from it this turn
        private readonly Dictionary<RevenueGenerator, float> _revenueByGenerator
            = new Dictionary<RevenueGenerator, float>();

        // Maps each generator -> the per-generator delegate registered on it
        // (stored so we can cleanly unsubscribe the exact same delegate)
        private readonly Dictionary<RevenueGenerator, Action<AbilitySystemCharacter, float, float>> _handlerByGenerator
            = new Dictionary<RevenueGenerator, Action<AbilitySystemCharacter, float, float>>();

        // ── Subscription management ───────────────────────────────────────────

        /// <summary>
        /// Subscribe to the given generator's OnRevenueGenerated event.
        /// Call at turn start for every active RevenueGenerator on the board.
        /// No-op if already subscribed.
        /// </summary>
        public void Subscribe(RevenueGenerator generator)
        {
            if (generator == null)
                return;

            if (_handlerByGenerator.ContainsKey(generator))
                return;

            // Capture generator in a per-instance lambda so we know which
            // generator produced each hit when the event fires.
            RevenueGenerator captured = generator;
            Action<AbilitySystemCharacter, float, float> handler =
                (source, amount, newBalance) => OnRevenueGenerated(captured, amount);

            generator.OnRevenueGenerated += handler;
            _handlerByGenerator[generator] = handler;

            // Ensure the revenue bucket exists for this generator
            if (!_revenueByGenerator.ContainsKey(generator))
                _revenueByGenerator[generator] = 0f;
        }

        /// <summary>
        /// Unsubscribe from all registered generators.
        /// Call after Launch Phase ends, before resolution.
        /// </summary>
        public void UnsubscribeAll()
        {
            foreach (var kvp in _handlerByGenerator)
            {
                if (kvp.Key != null)
                    kvp.Key.OnRevenueGenerated -= kvp.Value;
            }

            _handlerByGenerator.Clear();

            Debug.Log("[TurnRevenueAccumulator] Unsubscribed from all generators.");
        }

        // ── Per-turn reset ────────────────────────────────────────────────────

        /// <summary>
        /// Clear all accumulated revenue totals. Call at the start of each turn
        /// (before re-subscribing for that turn's generators).
        /// Also unsubscribes any still-active subscriptions as a safety measure.
        /// </summary>
        public void Reset()
        {
            UnsubscribeAll();
            _revenueByGenerator.Clear();

            Debug.Log("[TurnRevenueAccumulator] Reset for new turn.");
        }

        // ── Revenue query ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the total revenue accumulated across all companies this turn.
        /// </summary>
        public float GetTotalTurnRevenue()
        {
            float total = 0f;
            foreach (float value in _revenueByGenerator.Values)
                total += value;

            return total;
        }

        // ── Internal handler ──────────────────────────────────────────────────

        private void OnRevenueGenerated(RevenueGenerator generator, float amount)
        {
            if (!_revenueByGenerator.ContainsKey(generator))
                _revenueByGenerator[generator] = 0f;

            _revenueByGenerator[generator] += amount;

            Debug.Log(
                $"[TurnRevenueAccumulator] Revenue hit: amount={amount}, " +
                $"generatorTotal={_revenueByGenerator[generator]}, " +
                $"runningTotal={GetTotalTurnRevenue()}");
        }
    }
}
