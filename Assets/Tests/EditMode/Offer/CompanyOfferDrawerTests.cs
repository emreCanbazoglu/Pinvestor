using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pinvestor.Game.Offer;
using Pinvestor.GameConfigSystem;

namespace Pinvestor.Tests.EditMode.Offer
{
    /// <summary>
    /// EditMode tests for CompanyOfferDrawer.
    /// Verifies that 3 unique offers are drawn, placed/discarded companies are excluded,
    /// and depletion (fewer than 3 available) is handled without crashing.
    /// </summary>
    public class CompanyOfferDrawerTests
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

        private static CompanyConfigService BuildService(params string[] companyIds)
        {
            var models = companyIds
                .Select(id => MakeModel(id))
                .Cast<CompanyConfigModel>()
                .ToList();

            var rootModel = new GameConfigRootModel(
                schemaVersion: 1,
                generatedAtUtc: "2026-03-28T00:00:00Z",
                companies: models,
                balance: new NamedConfigSectionModel("balance", new Dictionary<string, float>()),
                roundCriteria: new NamedConfigSectionModel("roundCriteria", new Dictionary<string, float>()),
                runCycle: new RunCycleConfigModel(new List<RoundCycleConfigEntryModel>()),
                ball: new BallConfigModel(10f, 5f),
                shop: new NamedConfigSectionModel("shop", new Dictionary<string, float>()));

            var lookup = new GameConfigLookup();
            lookup.TryBuild(rootModel, out _);

            return new CompanyConfigService(lookup);
        }

        [Test]
        public void DrawOffer_Returns3Distinct_WhenPoolHas3OrMore()
        {
            var pool = new RunCompanyPool();
            pool.Initialize(new[] { "A", "B", "C", "D", "E" });

            var service = BuildService("A", "B", "C", "D", "E");
            var drawer = new CompanyOfferDrawer(pool, service);

            List<CompanyConfigModel> offer = drawer.DrawOffer();

            Assert.AreEqual(3, offer.Count);
            // All must be distinct.
            var ids = offer.Select(m => m.CompanyId).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
        }

        [Test]
        public void DrawOffer_ReturnsOnlyAvailable_WhenFewerThan3InPool()
        {
            var pool = new RunCompanyPool();
            pool.Initialize(new[] { "A", "B" });

            var service = BuildService("A", "B");
            var drawer = new CompanyOfferDrawer(pool, service);

            List<CompanyConfigModel> offer = drawer.DrawOffer();

            Assert.AreEqual(2, offer.Count);
        }

        [Test]
        public void DrawOffer_ReturnsOne_WhenOnlyOneInPool()
        {
            var pool = new RunCompanyPool();
            pool.Initialize(new[] { "A" });

            var service = BuildService("A");
            var drawer = new CompanyOfferDrawer(pool, service);

            List<CompanyConfigModel> offer = drawer.DrawOffer();

            Assert.AreEqual(1, offer.Count);
            Assert.AreEqual("A", offer[0].CompanyId);
        }

        [Test]
        public void DrawOffer_ReturnsEmpty_WhenPoolIsEmpty()
        {
            var pool = new RunCompanyPool();
            pool.Initialize(new string[0]);

            var service = BuildService("A", "B", "C");
            var drawer = new CompanyOfferDrawer(pool, service);

            List<CompanyConfigModel> offer = drawer.DrawOffer();

            Assert.AreEqual(0, offer.Count);
        }

        [Test]
        public void DrawOffer_NeverIncludesPlacedCompanies()
        {
            var pool = new RunCompanyPool();
            pool.Initialize(new[] { "A", "B", "C", "D" });
            pool.MarkPlaced("A");
            pool.MarkPlaced("B");

            var service = BuildService("A", "B", "C", "D");
            var drawer = new CompanyOfferDrawer(pool, service);

            // Run many times to catch randomness.
            for (int i = 0; i < 50; i++)
            {
                List<CompanyConfigModel> offer = drawer.DrawOffer();
                CollectionAssert.DoesNotContain(offer.Select(m => m.CompanyId).ToList(), "A");
                CollectionAssert.DoesNotContain(offer.Select(m => m.CompanyId).ToList(), "B");
            }
        }

        [Test]
        public void DrawOffer_NeverIncludesDiscardedCompanies()
        {
            var pool = new RunCompanyPool();
            pool.Initialize(new[] { "A", "B", "C", "D" });
            pool.MarkDiscarded("A");
            pool.MarkDiscarded("B");

            var service = BuildService("A", "B", "C", "D");
            var drawer = new CompanyOfferDrawer(pool, service);

            for (int i = 0; i < 50; i++)
            {
                List<CompanyConfigModel> offer = drawer.DrawOffer();
                CollectionAssert.DoesNotContain(offer.Select(m => m.CompanyId).ToList(), "A");
                CollectionAssert.DoesNotContain(offer.Select(m => m.CompanyId).ToList(), "B");
            }
        }

        [Test]
        public void DrawOffer_DoesNotModifyPool_OnDraw()
        {
            // Pool state should only change when MarkPlaced/MarkDiscarded is called,
            // not during DrawOffer itself.
            var pool = new RunCompanyPool();
            pool.Initialize(new[] { "A", "B", "C" });

            var service = BuildService("A", "B", "C");
            var drawer = new CompanyOfferDrawer(pool, service);

            drawer.DrawOffer();

            Assert.AreEqual(3, pool.Available.Count);
        }

        [Test]
        public void DrawOffer_NoDuplicatesInSingleDraw()
        {
            var pool = new RunCompanyPool();
            pool.Initialize(new[] { "A", "B", "C" });

            var service = BuildService("A", "B", "C");
            var drawer = new CompanyOfferDrawer(pool, service);

            for (int i = 0; i < 50; i++)
            {
                List<CompanyConfigModel> offer = drawer.DrawOffer();
                var ids = offer.Select(m => m.CompanyId).ToList();
                CollectionAssert.AllItemsAreUnique(ids);
            }
        }
    }
}
