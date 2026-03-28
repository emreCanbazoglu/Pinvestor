using NUnit.Framework;
using Pinvestor.Game.Health;

/// <summary>
/// EditMode tests for CompanyHealthState (spec 006 T022).
/// Tests: damage reduces health, clamps at 0, sets PendingCollapse.
/// </summary>
public class CompanyHealthStateTests
{
    [Test]
    public void InitialHealthEqualsMaxHealth()
    {
        var state = new CompanyHealthState(10f);
        Assert.AreEqual(10f, state.MaxHealth);
        Assert.AreEqual(10f, state.CurrentHealth);
        Assert.IsFalse(state.IsDead);
        Assert.IsFalse(state.PendingCollapse);
    }

    [Test]
    public void TakeDamage_ReducesCurrentHealth()
    {
        var state = new CompanyHealthState(10f);
        state.TakeDamage(3f);
        Assert.AreEqual(7f, state.CurrentHealth, 0.001f);
        Assert.IsFalse(state.IsDead);
        Assert.IsFalse(state.PendingCollapse);
    }

    [Test]
    public void TakeDamage_ClampsToZero()
    {
        var state = new CompanyHealthState(5f);
        state.TakeDamage(100f);
        Assert.AreEqual(0f, state.CurrentHealth, 0.001f);
    }

    [Test]
    public void TakeDamage_SetsPendingCollapseAtZeroHealth()
    {
        var state = new CompanyHealthState(3f);
        state.TakeDamage(3f);
        Assert.IsTrue(state.IsDead);
        Assert.IsTrue(state.PendingCollapse);
    }

    [Test]
    public void TakeDamage_DoesNothingIfAlreadyPendingCollapse()
    {
        var state = new CompanyHealthState(3f);
        state.TakeDamage(3f); // collapses
        state.TakeDamage(5f); // should be ignored
        Assert.AreEqual(0f, state.CurrentHealth, 0.001f);
        Assert.IsTrue(state.PendingCollapse);
    }

    [Test]
    public void MarkPendingCollapse_SetsZeroHealthAndFlag()
    {
        var state = new CompanyHealthState(10f);
        state.MarkPendingCollapse();
        Assert.AreEqual(0f, state.CurrentHealth, 0.001f);
        Assert.IsTrue(state.IsDead);
        Assert.IsTrue(state.PendingCollapse);
    }

    [Test]
    public void MinMaxHealthIsOne_WhenInitializedWithZero()
    {
        // MaxHealth is clamped to at least 1 to avoid division-by-zero issues in UI.
        var state = new CompanyHealthState(0f);
        Assert.AreEqual(1f, state.MaxHealth);
    }
}
