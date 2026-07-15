using NUnit.Framework;
using Pinvestor.Game.Health;

public class DeferredDamageStateTests
{
    [Test]
    public void TryDeferDamage_StopsAtConfiguredCap()
    {
        var state = new DeferredDamageState(3);

        Assert.That(state.TryDeferDamage(), Is.True);
        Assert.That(state.TryDeferDamage(), Is.True);
        Assert.That(state.TryDeferDamage(), Is.True);
        Assert.That(state.TryDeferDamage(), Is.False);
        Assert.That(state.PendingDamage, Is.EqualTo(3));
    }

    [Test]
    public void ConsumePendingDamage_ReturnsDebtAndClearsState()
    {
        var state = new DeferredDamageState(3);
        state.TryDeferDamage();
        state.TryDeferDamage();

        Assert.That(state.ConsumePendingDamage(), Is.EqualTo(2));
        Assert.That(state.PendingDamage, Is.Zero);
        Assert.That(state.ConsumePendingDamage(), Is.Zero);
    }
}
