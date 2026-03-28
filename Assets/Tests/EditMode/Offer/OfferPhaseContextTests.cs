using System.Collections.Generic;
using NUnit.Framework;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;

namespace Pinvestor.Tests.EditMode.Offer
{
    /// <summary>
    /// EditMode tests for OfferPhaseContext.
    /// Verifies that the context holds offered companies, resolves on selection,
    /// supports the force-select fallback, and clears state correctly.
    ///
    /// Note: UniTask's SelectionTask is tested through synchronous completion
    /// source inspection (TrySetResult is synchronous) rather than async awaiting,
    /// since EditMode tests run without the Unity player loop.
    /// </summary>
    public class OfferPhaseContextTests
    {
        private static CompanyConfigModel MakeModel(string id)
        {
            return new CompanyConfigModel(
                id,
                new Dictionary<string, float>(),
                new Dictionary<string, float>(),
                maxHP: 100f,
                revenuePerHit: 50f,
                turnlyCost: 10f,
                hasMaxHP: true,
                hasRevenuePerHit: true,
                hasTurnlyCost: true);
        }

        [Test]
        public void OfferedCompanies_MatchesInput()
        {
            var models = new List<CompanyConfigModel>
            {
                MakeModel("CompA"),
                MakeModel("CompB"),
                MakeModel("CompC"),
            };

            var context = new OfferPhaseContext(models);

            Assert.AreEqual(3, context.OfferedCompanies.Count);
            Assert.AreEqual("CompA", context.OfferedCompanies[0].CompanyId);
            Assert.AreEqual("CompB", context.OfferedCompanies[1].CompanyId);
            Assert.AreEqual("CompC", context.OfferedCompanies[2].CompanyId);
        }

        [Test]
        public void ConfirmSelection_SetsConfirmedSelection()
        {
            var models = new List<CompanyConfigModel>
            {
                MakeModel("CompA"),
                MakeModel("CompB"),
            };
            var context = new OfferPhaseContext(models);

            context.ConfirmSelection(models[1]);

            Assert.AreEqual("CompB", context.ConfirmedSelection.CompanyId);
        }

        [Test]
        public void ConfirmSelection_Null_DoesNotSetConfirmedSelection()
        {
            var models = new List<CompanyConfigModel> { MakeModel("CompA") };
            var context = new OfferPhaseContext(models);

            context.ConfirmSelection(null);

            Assert.IsNull(context.ConfirmedSelection);
        }

        [Test]
        public void ConfirmSelection_CalledTwice_SecondIsIgnored()
        {
            var models = new List<CompanyConfigModel>
            {
                MakeModel("CompA"),
                MakeModel("CompB"),
            };
            var context = new OfferPhaseContext(models);

            context.ConfirmSelection(models[0]);
            context.ConfirmSelection(models[1]); // should be ignored

            Assert.AreEqual("CompA", context.ConfirmedSelection.CompanyId);
        }

        [Test]
        public void ForceSelectFirst_SelectsFirstOfferedCompany()
        {
            var models = new List<CompanyConfigModel>
            {
                MakeModel("CompA"),
                MakeModel("CompB"),
            };
            var context = new OfferPhaseContext(models);

            context.ForceSelectFirst();

            Assert.AreEqual("CompA", context.ConfirmedSelection.CompanyId);
        }

        [Test]
        public void ForceSelectFirst_AfterConfirm_IsNoOp()
        {
            var models = new List<CompanyConfigModel>
            {
                MakeModel("CompA"),
                MakeModel("CompB"),
            };
            var context = new OfferPhaseContext(models);

            context.ConfirmSelection(models[1]);
            context.ForceSelectFirst(); // task no longer pending, should be ignored

            Assert.AreEqual("CompB", context.ConfirmedSelection.CompanyId);
        }

        [Test]
        public void Clear_NullsConfirmedSelection()
        {
            var models = new List<CompanyConfigModel> { MakeModel("CompA") };
            var context = new OfferPhaseContext(models);

            context.ConfirmSelection(models[0]);
            context.Clear();

            Assert.IsNull(context.ConfirmedSelection);
        }

        [Test]
        public void ConfirmedSelection_IsNullBeforeAnySelection()
        {
            var models = new List<CompanyConfigModel> { MakeModel("CompA") };
            var context = new OfferPhaseContext(models);

            Assert.IsNull(context.ConfirmedSelection);
        }
    }
}
