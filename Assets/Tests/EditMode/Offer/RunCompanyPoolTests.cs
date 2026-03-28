using System.Collections.Generic;
using NUnit.Framework;
using Pinvestor.Game.Offer;

namespace Pinvestor.Tests.EditMode.Offer
{
    /// <summary>
    /// EditMode tests for RunCompanyPool.
    /// Verifies pool initialization, tracking of placed/discarded companies,
    /// and exclusion from future availability.
    /// </summary>
    public class RunCompanyPoolTests
    {
        private RunCompanyPool _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new RunCompanyPool();
        }

        [Test]
        public void Initialize_PopulatesAvailable_WithAllIds()
        {
            _pool.Initialize(new[] { "CompA", "CompB", "CompC" });

            Assert.AreEqual(3, _pool.Available.Count);
            Assert.IsTrue(_pool.IsAvailable("CompA"));
            Assert.IsTrue(_pool.IsAvailable("CompB"));
            Assert.IsTrue(_pool.IsAvailable("CompC"));
        }

        [Test]
        public void Initialize_ClearsPriorState()
        {
            _pool.Initialize(new[] { "CompA", "CompB" });
            _pool.Initialize(new[] { "CompX" });

            Assert.AreEqual(1, _pool.Available.Count);
            Assert.IsTrue(_pool.IsAvailable("CompX"));
            Assert.IsFalse(_pool.IsAvailable("CompA"));
        }

        [Test]
        public void MarkPlaced_RemovesFromAvailable_AddsToPlaced()
        {
            _pool.Initialize(new[] { "CompA", "CompB", "CompC" });
            _pool.MarkPlaced("CompA");

            Assert.AreEqual(2, _pool.Available.Count);
            Assert.IsFalse(_pool.IsAvailable("CompA"));
            Assert.IsTrue(_pool.Placed.Contains("CompA"));
        }

        [Test]
        public void MarkDiscarded_RemovesFromAvailable_AddsToDiscarded()
        {
            _pool.Initialize(new[] { "CompA", "CompB", "CompC" });
            _pool.MarkDiscarded("CompB");

            Assert.AreEqual(2, _pool.Available.Count);
            Assert.IsFalse(_pool.IsAvailable("CompB"));
            Assert.IsTrue(_pool.Discarded.Contains("CompB"));
        }

        [Test]
        public void PlacedAndDiscarded_AreExcludedFromAvailable()
        {
            _pool.Initialize(new[] { "CompA", "CompB", "CompC", "CompD" });
            _pool.MarkPlaced("CompA");
            _pool.MarkDiscarded("CompB");

            Assert.IsFalse(_pool.IsAvailable("CompA"));
            Assert.IsFalse(_pool.IsAvailable("CompB"));
            Assert.IsTrue(_pool.IsAvailable("CompC"));
            Assert.IsTrue(_pool.IsAvailable("CompD"));
            Assert.AreEqual(2, _pool.Available.Count);
        }

        [Test]
        public void Clear_ResetsAllState()
        {
            _pool.Initialize(new[] { "CompA", "CompB" });
            _pool.MarkPlaced("CompA");
            _pool.MarkDiscarded("CompB");

            _pool.Clear();

            Assert.AreEqual(0, _pool.Available.Count);
            Assert.AreEqual(0, _pool.Placed.Count);
            Assert.AreEqual(0, _pool.Discarded.Count);
        }

        [Test]
        public void MarkPlaced_NullOrEmpty_IsNoOp()
        {
            _pool.Initialize(new[] { "CompA" });

            Assert.DoesNotThrow(() => _pool.MarkPlaced(null));
            Assert.DoesNotThrow(() => _pool.MarkPlaced(""));
            Assert.AreEqual(1, _pool.Available.Count);
        }

        [Test]
        public void MarkDiscarded_NullOrEmpty_IsNoOp()
        {
            _pool.Initialize(new[] { "CompA" });

            Assert.DoesNotThrow(() => _pool.MarkDiscarded(null));
            Assert.DoesNotThrow(() => _pool.MarkDiscarded(""));
            Assert.AreEqual(1, _pool.Available.Count);
        }

        [Test]
        public void Initialize_IgnoresNullOrWhitespaceIds()
        {
            _pool.Initialize(new[] { "CompA", null, "", "   ", "CompB" });

            Assert.AreEqual(2, _pool.Available.Count);
            Assert.IsTrue(_pool.IsAvailable("CompA"));
            Assert.IsTrue(_pool.IsAvailable("CompB"));
        }

        [Test]
        public void IsAvailable_ReturnsFalse_ForUnknownId()
        {
            _pool.Initialize(new[] { "CompA" });

            Assert.IsFalse(_pool.IsAvailable("CompUnknown"));
        }
    }
}
