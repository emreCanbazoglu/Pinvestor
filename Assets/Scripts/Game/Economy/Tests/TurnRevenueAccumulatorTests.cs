using NUnit.Framework;
using Pinvestor.Game.Economy;
using Pinvestor.RevenueGeneratorSystem.Core;
using UnityEngine;

namespace Pinvestor.Game.Economy.Tests
{
    /// <summary>
    /// EditMode tests for TurnRevenueAccumulator: subscription lifecycle, revenue math,
    /// and cross-turn isolation.
    /// </summary>
    public class TurnRevenueAccumulatorTests
    {
        private TurnRevenueAccumulator _accumulator;
        private RevenueGenerator _generatorA;
        private RevenueGenerator _generatorB;

        [SetUp]
        public void SetUp()
        {
            _accumulator = new TurnRevenueAccumulator();

            // Create MonoBehaviour instances via temporary GameObjects.
            _generatorA = new GameObject("GeneratorA").AddComponent<RevenueGenerator>();
            _generatorB = new GameObject("GeneratorB").AddComponent<RevenueGenerator>();
        }

        [TearDown]
        public void TearDown()
        {
            _accumulator.Reset();

            if (_generatorA != null)
                Object.DestroyImmediate(_generatorA.gameObject);
            if (_generatorB != null)
                Object.DestroyImmediate(_generatorB.gameObject);
        }

        [Test]
        public void GetTotalTurnRevenue_InitiallyZero()
        {
            Assert.AreEqual(0f, _accumulator.GetTotalTurnRevenue());
        }

        [Test]
        public void SingleGenerator_SingleHit_AccumulatesCorrectly()
        {
            _accumulator.Subscribe(_generatorA);

            SimulateHit(_generatorA, 100f);

            Assert.AreEqual(100f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void SingleGenerator_MultipleHits_SumsCorrectly()
        {
            _accumulator.Subscribe(_generatorA);

            SimulateHit(_generatorA, 50f);
            SimulateHit(_generatorA, 75f);
            SimulateHit(_generatorA, 25f);

            Assert.AreEqual(150f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void MultipleGenerators_MultipleHits_SumsAcrossAll()
        {
            _accumulator.Subscribe(_generatorA);
            _accumulator.Subscribe(_generatorB);

            SimulateHit(_generatorA, 100f);
            SimulateHit(_generatorA, 50f);
            SimulateHit(_generatorB, 200f);

            // 100 + 50 + 200 = 350
            Assert.AreEqual(350f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void Reset_ClearsAccumulatedRevenue()
        {
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 300f);

            _accumulator.Reset();

            Assert.AreEqual(0f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void UnsubscribeAll_StopsAccumulation()
        {
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 100f);

            _accumulator.UnsubscribeAll();

            // Hit after unsubscribe should not be counted
            SimulateHit(_generatorA, 999f);

            Assert.AreEqual(100f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void Reset_PreventsHitsFromPreviousTurnLeakingIntoNextTurn()
        {
            // Turn 1
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 200f);
            _accumulator.UnsubscribeAll();

            // Turn 2 — reset clears previous turn's totals
            _accumulator.Reset();
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 50f);

            Assert.AreEqual(50f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        [Test]
        public void Subscribe_SameGeneratorTwice_DoesNotDoubleCount()
        {
            _accumulator.Subscribe(_generatorA);
            _accumulator.Subscribe(_generatorA); // second subscribe should no-op

            SimulateHit(_generatorA, 100f);

            Assert.AreEqual(100f, _accumulator.GetTotalTurnRevenue(), 0.001f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Directly fires the generator's OnRevenueGenerated event to simulate a ball hit.
        /// </summary>
        private static void SimulateHit(RevenueGenerator generator, float amount)
        {
            // Invoke the public Action directly — simulates what happens when the
            // gameplay effect applies revenue to the company's attribute.
            generator.OnRevenueGenerated?.Invoke(null, amount, amount);
        }
    }
}
