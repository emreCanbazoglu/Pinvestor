using NUnit.Framework;
using Pinvestor.Game;
using Pinvestor.Game.Health;
using UnityEngine;

/// <summary>
/// EditMode tests for company collapse detection logic (spec 006 T024).
///
/// Full board removal (Turn.RemoveCollapsedCompanies) requires MonoBehaviour/Board
/// infrastructure and is covered by the manual smoke test (T025).
/// These tests verify the pure-C# collapse-detection models used by Turn.
/// </summary>
public class CompanyCollapseHandlerTests
{
    [Test]
    public void CompanyHealthState_NotPendingCollapse_WhenHealthAboveZero()
    {
        var state = new CompanyHealthState(5f);
        state.TakeDamage(3f);
        Assert.IsFalse(state.PendingCollapse, "Should not be pending collapse with 2 HP remaining.");
    }

    [Test]
    public void CompanyHealthState_PendingCollapse_WhenHealthReachesZero()
    {
        var state = new CompanyHealthState(5f);
        state.TakeDamage(5f);
        Assert.IsTrue(state.PendingCollapse, "Should be pending collapse when HP = 0.");
    }

    [Test]
    public void CompanyHealthState_PendingCollapse_WhenHealthExceedsMax()
    {
        var state = new CompanyHealthState(3f);
        state.TakeDamage(99f);
        Assert.IsTrue(state.PendingCollapse, "Should be pending collapse when damage exceeds HP.");
        Assert.AreEqual(0f, state.CurrentHealth, 0.001f, "Health should be clamped to 0.");
    }

    [Test]
    public void CompanyCollapsedEvent_HoldsExpectedData()
    {
        string expectedId = "CloutHub";
        var expectedPos = new Vector2Int(2, 3);

        var evt = new CompanyCollapsedEvent(expectedId, expectedPos);

        Assert.AreEqual(expectedId, evt.CompanyId);
        Assert.AreEqual(expectedPos, evt.BoardPosition);
    }

    [Test]
    public void CompanyCashedOutEvent_HoldsExpectedData()
    {
        string expectedId = "CloutHub";
        float expectedPayout = 150f;
        var expectedPos = new Vector2Int(1, 1);

        var evt = new CompanyCashedOutEvent(expectedId, expectedPayout, expectedPos);

        Assert.AreEqual(expectedId, evt.CompanyId);
        Assert.AreEqual(expectedPayout, evt.PayoutAmount, 0.001f);
        Assert.AreEqual(expectedPos, evt.BoardPosition);
    }

    [Test]
    public void MultipleCompanies_CollapseFlagsAreIndependent()
    {
        var stateA = new CompanyHealthState(5f);
        var stateB = new CompanyHealthState(5f);

        stateA.TakeDamage(5f); // A collapses

        Assert.IsTrue(stateA.PendingCollapse, "Company A should be pending collapse.");
        Assert.IsFalse(stateB.PendingCollapse, "Company B should not be pending collapse.");
    }
}
