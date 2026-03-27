using System.Collections.Generic;
using NUnit.Framework;
using Pinvestor.BoardSystem.Base;
using Pinvestor.Game.Economy;
using Pinvestor.RevenueGeneratorSystem.Core;
using UnityEngine;

namespace Pinvestor.Game.Economy.Tests
{
    /// <summary>
    /// EditMode tests for EconomyService.ApplyResolution():
    /// net worth delta must equal accumulated revenue minus total op-costs.
    ///
    /// These tests use a null GameConfigManager (op-costs from config = 0) so that
    /// the net-worth delta equals revenue only. Tests for op-cost deduction are covered
    /// in WinLossConditionTests where the full economy state is exercised.
    /// </summary>
    public class EconomyServiceResolutionTests
    {
        private PlayerEconomyState _economyState;
        private TurnRevenueAccumulator _accumulator;
        private EconomyService _economyService;
        private RevenueGenerator _generatorA;

        [SetUp]
        public void SetUp()
        {
            // PlayerEconomyState is a MonoBehaviour singleton — create in scene.
            var go = new GameObject("PlayerEconomyState");
            _economyState = go.AddComponent<PlayerEconomyState>();

            _accumulator = new TurnRevenueAccumulator();

            // EconomyService with null GameConfigManager — op-costs will be 0.
            _economyService = new EconomyService(_accumulator, null);

            _generatorA = new GameObject("GeneratorA").AddComponent<RevenueGenerator>();
        }

        [TearDown]
        public void TearDown()
        {
            _accumulator.Reset();

            if (_economyState != null)
                Object.DestroyImmediate(_economyState.gameObject);
            if (_generatorA != null)
                Object.DestroyImmediate(_generatorA.gameObject);
        }

        [Test]
        public void ApplyResolution_NoHitsNoOpCosts_NetWorthUnchanged()
        {
            _economyState.Initialize(1000f);

            _economyService.ApplyResolution(new List<BoardItem_Company>());

            Assert.AreEqual(1000f, _economyState.NetWorth, 0.001f);
        }

        [Test]
        public void ApplyResolution_WithRevenue_IncrementsNetWorth()
        {
            _economyState.Initialize(1000f);

            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 150f);
            _accumulator.UnsubscribeAll();

            _economyService.ApplyResolution(new List<BoardItem_Company>());

            // 1000 + 150 = 1150 (op-cost = 0 because no config)
            Assert.AreEqual(1150f, _economyState.NetWorth, 0.001f);
        }

        [Test]
        public void ApplyResolution_LastTurnRevenue_MatchesAccumulated()
        {
            _economyState.Initialize(500f);

            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 80f);
            SimulateHit(_generatorA, 20f);
            _accumulator.UnsubscribeAll();

            _economyService.ApplyResolution(new List<BoardItem_Company>());

            Assert.AreEqual(100f, _economyState.LastTurnRevenue, 0.001f);
        }

        [Test]
        public void ApplyResolution_NetWorthDeltaEqualsRevenueMinusOpCost()
        {
            // With null config, op-cost = 0, so delta = revenue.
            _economyState.Initialize(2000f);
            float expectedRevenue = 300f;

            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, expectedRevenue);
            _accumulator.UnsubscribeAll();

            float worthBefore = _economyState.NetWorth;
            _economyService.ApplyResolution(new List<BoardItem_Company>());
            float worthAfter = _economyState.NetWorth;

            float actualDelta = worthAfter - worthBefore;
            // delta = revenue (300) - opCost (0) = 300
            Assert.AreEqual(expectedRevenue, actualDelta, 0.001f);
        }

        [Test]
        public void ApplyResolution_WhenNotInitialized_DoesNotThrow()
        {
            // PlayerEconomyState not initialized — service should log warning and return.
            Assert.DoesNotThrow(() =>
                _economyService.ApplyResolution(new List<BoardItem_Company>()));
        }

        [Test]
        public void ApplyResolution_MultipleCallsPerTurn_AccumulatesCorrectly()
        {
            _economyState.Initialize(1000f);

            // Turn 1
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 200f);
            _accumulator.UnsubscribeAll();
            _economyService.ApplyResolution(new List<BoardItem_Company>());

            // Turn 2
            _accumulator.Reset();
            _accumulator.Subscribe(_generatorA);
            SimulateHit(_generatorA, 150f);
            _accumulator.UnsubscribeAll();
            _economyService.ApplyResolution(new List<BoardItem_Company>());

            // 1000 + 200 + 150 = 1350
            Assert.AreEqual(1350f, _economyState.NetWorth, 0.001f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SimulateHit(RevenueGenerator generator, float amount)
        {
            generator.OnRevenueGenerated?.Invoke(null, amount, amount);
        }
    }
}
