using NUnit.Framework;
using Pinvestor.Game.Economy;

/// <summary>
/// EditMode tests for CompanyValuationModel and CashoutService payout math (spec 006 T023).
/// Tests: correct payout calculation, cashout rate clamping, default rate fallback.
/// Note: CashoutService integration tests require Unity MonoBehaviour infrastructure
/// (AbilitySystemCharacter, Board) and are covered by the manual smoke test (T025).
/// </summary>
public class CompanyValuationModelTests
{
    [Test]
    public void CashoutValue_IsProductOfCostAndRate()
    {
        var model = new CompanyValuationModel(purchaseCost: 200f, cashoutRate: 0.5f);
        Assert.AreEqual(100f, model.CashoutValue, 0.001f);
    }

    [Test]
    public void CashoutValue_WithFullRate_ReturnsPurchaseCost()
    {
        var model = new CompanyValuationModel(purchaseCost: 300f, cashoutRate: 1.0f);
        Assert.AreEqual(300f, model.CashoutValue, 0.001f);
    }

    [Test]
    public void CashoutValue_WithZeroRate_ReturnsZero()
    {
        var model = new CompanyValuationModel(purchaseCost: 500f, cashoutRate: 0f);
        Assert.AreEqual(0f, model.CashoutValue, 0.001f);
    }

    [Test]
    public void CashoutRate_ClampedTo01Range()
    {
        var modelHigh = new CompanyValuationModel(purchaseCost: 100f, cashoutRate: 5f);
        Assert.AreEqual(1f, modelHigh.CashoutRate, 0.001f);

        var modelLow = new CompanyValuationModel(purchaseCost: 100f, cashoutRate: -1f);
        Assert.AreEqual(0f, modelLow.CashoutRate, 0.001f);
    }

    [Test]
    public void PurchaseCost_ClampedToZero_WhenNegative()
    {
        var model = new CompanyValuationModel(purchaseCost: -100f, cashoutRate: 0.5f);
        Assert.AreEqual(0f, model.PurchaseCost, 0.001f);
    }

    [Test]
    public void DefaultCashoutRate_IsPointFive()
    {
        Assert.AreEqual(0.5f, CompanyValuationModel.DefaultCashoutRate, 0.001f);
    }

    [Test]
    public void CashoutRateKey_IsCorrectString()
    {
        Assert.AreEqual("cashout_rate", CompanyValuationModel.CashoutRateKey);
    }
}
