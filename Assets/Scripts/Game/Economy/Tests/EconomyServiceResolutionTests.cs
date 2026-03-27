using NUnit.Framework;
using Pinvestor.Game.Economy;
using Pinvestor.RevenueGeneratorSystem.Core;
using UnityEngine;

namespace Pinvestor.Game.Economy.Tests
{
    /// <summary>
    /// EditMode tests for EconomyService.ApplyResolution().
    ///
    /// EconomyService credits revenue via GAS AttributeSystemComponent.ModifyBaseValue().
    /// Full GAS integration (CardPlayer with AttributeSystemComponent + AttributeSet SO)
    /// requires Unity play mode — see T017 manual smoke test.
    ///
    /// These tests verify:
    ///   - Null-guard: ApplyResolution(null) logs a warning and does not throw.
    ///   - Accumulator integration: GetTotalTurnRevenue() returns the correct total that
    ///     EconomyService would pass to the Balance attribute.
    ///   - Accumulator reset isolation: revenue does not leak across turns.
    /// </summary>
    public class EconomyServiceResolutionTests
    {
        private TurnRevenueAccumulator _accumulator;
        private EconomyService _economyService;
        private RevenueGenerator _generatorA;

        [SetUp]
        public void SetUp()
        {
            _accumulator = new TurnRevenueAccumulator();
            _economyService = new EconomyService(_accumulator);

            _generatorA = new GameObject("GeneratorA").AddComponent<RevenueGenerator>();
        }

        [TearDown]
        public void TearDown()
        {
            _accumulator.Reset();

            if (_generatorA != null)
                Object.DestroyImmediate(_generatorA.gameObject);
        }

        // ── Null-guard ────────────────────────────────────────────────────────

        [Test]
        public void ApplyResolution_NullCardPlayer_DoesNotThrow()
        {
            // Should log a warning and return — must not throw.
            Assert.DoesNotThrow(() =>
                _economyService.ApplyResolution(null));
        }

        // ── Accumulator state (revenue that EconomyService would credit) ──────

        [Test]
        public void Accumulator_NoHits_TotalRevenueIsZero()
        {
            // No hits fired — accumulator should report 0.
            Assert.AreEqual(0f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void Accumulator_SingleHit_TotalRevenueMatchesHitAmount()
        {
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 150f);
            _accumulator.UnsubscribeAll();

            // EconomyService will read this total and credit it to Balance.
            Assert.AreEqual(150f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void Accumulator_MultipleHits_TotalRevenueSumsAll()
        {
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 80f);
            SimulateHit(_generatorA, 20f);
            _accumulator.UnsubscribeAll();

            Assert.AreEqual(100f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void Accumulator_AfterReset_TotalRevenueIsZero()
        {
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 300f);
            _accumulator.UnsubscribeAll();

            // Reset simulates turn boundary — next turn starts clean.
            _accumulator.Reset();

            Assert.AreEqual(0f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void Accumulator_CrossTurnIsolation_RevenueDoesNotLeak()
        {
            // Turn 1
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 200f);
            _accumulator.UnsubscribeAll();

            // Turn 2 — reset clears previous turn's totals
            _accumulator.Reset();
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 50f);

            // Only turn 2's revenue should be visible
            Assert.AreEqual(50f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SimulateHit(RevenueGenerator generator, float amount)
        {
            generator.OnRevenueGenerated?.Invoke(null, amount, amount);
        }
    }
}
