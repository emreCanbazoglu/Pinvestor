using NUnit.Framework;
using Pinvestor.Game.Economy;
using UnityEngine;

namespace Pinvestor.Game.Economy.Tests
{
    /// <summary>
    /// EditMode tests for win/loss evaluation:
    /// win when net worth >= target, loss when net worth &lt; target.
    ///
    /// These tests exercise PlayerEconomyState directly to validate the economy
    /// state that Round.cs reads when evaluating the run outcome.
    /// </summary>
    public class WinLossConditionTests
    {
        private PlayerEconomyState _economyState;
        private TurnRevenueAccumulator _accumulator;
        private EconomyService _economyService;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("PlayerEconomyState");
            _economyState = go.AddComponent<PlayerEconomyState>();

            _accumulator = new TurnRevenueAccumulator();
            _economyService = new EconomyService(_accumulator, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_economyState != null)
                Object.DestroyImmediate(_economyState.gameObject);
        }

        // ── Win condition ─────────────────────────────────────────────────────

        [Test]
        public void WinCondition_NetWorthExactlyAtTarget_IsWin()
        {
            float targetWorth = 2000f;
            _economyState.Initialize(1000f);

            // Simulate resolution that brings net worth exactly to target.
            _economyState.ApplyResolutionDelta(revenue: 1000f, opCost: 0f);

            bool isWin = _economyState.NetWorth >= targetWorth;

            Assert.IsTrue(isWin,
                $"Expected WIN: netWorth={_economyState.NetWorth} >= targetWorth={targetWorth}");
        }

        [Test]
        public void WinCondition_NetWorthAboveTarget_IsWin()
        {
            float targetWorth = 2000f;
            _economyState.Initialize(1000f);
            _economyState.ApplyResolutionDelta(revenue: 1500f, opCost: 0f);

            bool isWin = _economyState.NetWorth >= targetWorth;

            Assert.IsTrue(isWin,
                $"Expected WIN: netWorth={_economyState.NetWorth} >= targetWorth={targetWorth}");
        }

        // ── Loss condition ────────────────────────────────────────────────────

        [Test]
        public void LossCondition_NetWorthBelowTarget_IsLoss()
        {
            float targetWorth = 2000f;
            _economyState.Initialize(1000f);
            _economyState.ApplyResolutionDelta(revenue: 500f, opCost: 0f);

            bool isWin = _economyState.NetWorth >= targetWorth;

            Assert.IsFalse(isWin,
                $"Expected LOSS: netWorth={_economyState.NetWorth} < targetWorth={targetWorth}");
        }

        [Test]
        public void LossCondition_OpCostsDriveNetWorthNegative_IsLoss()
        {
            float targetWorth = 0f;
            _economyState.Initialize(100f);

            // Very high op costs with no revenue — net worth goes negative.
            _economyState.ApplyResolutionDelta(revenue: 0f, opCost: 500f);

            bool isWin = _economyState.NetWorth >= targetWorth;

            Assert.IsFalse(isWin,
                $"Expected LOSS: netWorth={_economyState.NetWorth} < targetWorth={targetWorth}");
        }

        // ── Net worth arithmetic ──────────────────────────────────────────────

        [Test]
        public void NetWorth_StartsAtInitialCapital()
        {
            _economyState.Initialize(1500f);

            Assert.AreEqual(1500f, _economyState.NetWorth, 0.001f);
        }

        [Test]
        public void NetWorth_AfterResolution_EqualsInitialPlusRevenueMinus0pCost()
        {
            _economyState.Initialize(1000f);
            _economyState.ApplyResolutionDelta(revenue: 400f, opCost: 150f);

            // 1000 + 400 - 150 = 1250
            Assert.AreEqual(1250f, _economyState.NetWorth, 0.001f);
        }

        [Test]
        public void NetWorth_MultipleResolutions_CumulatesCorrectly()
        {
            _economyState.Initialize(1000f);

            _economyState.ApplyResolutionDelta(revenue: 200f, opCost: 50f);  // +150 → 1150
            _economyState.ApplyResolutionDelta(revenue: 300f, opCost: 100f); // +200 → 1350
            _economyState.ApplyResolutionDelta(revenue: 100f, opCost: 200f); // -100 → 1250

            Assert.AreEqual(1250f, _economyState.NetWorth, 0.001f);
        }

        [Test]
        public void LastTurnRevenue_ReflectsMostRecentResolution()
        {
            _economyState.Initialize(1000f);
            _economyState.ApplyResolutionDelta(revenue: 300f, opCost: 100f);
            _economyState.ApplyResolutionDelta(revenue: 500f, opCost: 200f);

            Assert.AreEqual(500f, _economyState.LastTurnRevenue, 0.001f);
            Assert.AreEqual(200f, _economyState.LastTurnOpCost, 0.001f);
        }

        [Test]
        public void NetWorth_AllowsGoingNegative_NoFloor()
        {
            _economyState.Initialize(100f);
            _economyState.ApplyResolutionDelta(revenue: 0f, opCost: 300f);

            // 100 - 300 = -200 — no floor enforced per spec
            Assert.AreEqual(-200f, _economyState.NetWorth, 0.001f);
        }
    }
}
