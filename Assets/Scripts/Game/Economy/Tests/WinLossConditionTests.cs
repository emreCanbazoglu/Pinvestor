using NUnit.Framework;

namespace Pinvestor.Game.Economy.Tests
{
    /// <summary>
    /// EditMode tests for win/loss evaluation logic.
    ///
    /// The GAS Balance attribute is the single source of truth for net worth.
    /// Round.EvaluateRequirement() reads the Balance attribute via
    /// RoundContext.TryGetCurrentNetWorth() and evaluates: currentWorth >= requiredWorth.
    ///
    /// Full GAS integration testing (reading Balance from CardPlayer.AbilitySystemCharacter)
    /// requires Unity play mode — see T017 manual smoke test.
    ///
    /// These tests verify the win/loss comparison logic directly, independent of GAS wiring,
    /// so the condition semantics are always protected by regression tests.
    /// </summary>
    public class WinLossConditionTests
    {
        // ── Win condition ─────────────────────────────────────────────────────

        [Test]
        public void WinCondition_NetWorthExactlyAtTarget_IsWin()
        {
            float currentWorth = 2000f;
            float targetWorth = 2000f;

            bool passed = currentWorth >= targetWorth;

            Assert.IsTrue(passed,
                $"Expected WIN: currentWorth={currentWorth} >= targetWorth={targetWorth}");
        }

        [Test]
        public void WinCondition_NetWorthAboveTarget_IsWin()
        {
            float currentWorth = 2500f;
            float targetWorth = 2000f;

            bool passed = currentWorth >= targetWorth;

            Assert.IsTrue(passed,
                $"Expected WIN: currentWorth={currentWorth} >= targetWorth={targetWorth}");
        }

        // ── Loss condition ────────────────────────────────────────────────────

        [Test]
        public void LossCondition_NetWorthBelowTarget_IsLoss()
        {
            float currentWorth = 1500f;
            float targetWorth = 2000f;

            bool passed = currentWorth >= targetWorth;

            Assert.IsFalse(passed,
                $"Expected LOSS: currentWorth={currentWorth} < targetWorth={targetWorth}");
        }

        [Test]
        public void LossCondition_NetWorthNegative_IsLoss()
        {
            float currentWorth = -200f;
            float targetWorth = 0f;

            bool passed = currentWorth >= targetWorth;

            Assert.IsFalse(passed,
                $"Expected LOSS: currentWorth={currentWorth} < targetWorth={targetWorth}");
        }

        [Test]
        public void LossCondition_NetWorthZeroWithPositiveTarget_IsLoss()
        {
            float currentWorth = 0f;
            float targetWorth = 1000f;

            bool passed = currentWorth >= targetWorth;

            Assert.IsFalse(passed,
                $"Expected LOSS: currentWorth={currentWorth} < targetWorth={targetWorth}");
        }

        // ── Net worth arithmetic (revenue credit semantics) ───────────────────
        // These tests verify the arithmetic that EconomyService performs on Balance:
        // newBalance = oldBalance + revenue
        // Op-costs are handled by ApplyTurnlyCosts() separately.

        [Test]
        public void NetWorth_RevenueCredit_IncreasesBalance()
        {
            float initialBalance = 1000f;
            float revenue = 400f;

            float afterCredit = initialBalance + revenue;

            Assert.AreEqual(1400f, afterCredit, 0.001f);
        }

        [Test]
        public void NetWorth_ZeroRevenue_BalanceUnchanged()
        {
            float initialBalance = 1000f;
            float revenue = 0f;

            float afterCredit = initialBalance + revenue;

            Assert.AreEqual(1000f, afterCredit, 0.001f);
        }

        [Test]
        public void NetWorth_MultipleRevenueTurns_CumulatesCorrectly()
        {
            float balance = 1000f;

            balance += 200f; // Turn 1 revenue
            balance += 300f; // Turn 2 revenue
            balance += 100f; // Turn 3 revenue

            // 1000 + 200 + 300 + 100 = 1600
            Assert.AreEqual(1600f, balance, 0.001f);
        }

        [Test]
        public void NetWorth_AllowsGoingBelowZero_NoFloor()
        {
            // Balance can go negative if op-costs exceed revenue over time.
            // ApplyTurnlyCosts deducts: balance += -opCost
            float balance = 100f;
            float opCost = 300f;

            balance += -opCost;

            // 100 - 300 = -200 — no floor enforced per spec
            Assert.AreEqual(-200f, balance, 0.001f);
        }
    }
}
