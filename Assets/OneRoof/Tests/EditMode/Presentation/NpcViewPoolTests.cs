using System;
using NUnit.Framework;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class NpcViewPoolTests
    {
        private GameObject _rootObject;
        private NpcViewPool _pool;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("NpcViewPool_TestRoot");
            _pool = _rootObject.AddComponent<NpcViewPool>();
            _pool.MaxCapacity = 40;
        }

        [TearDown]
        public void TearDown()
        {
            if (_rootObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_rootObject);
                _rootObject = null;
            }
        }

        [Test]
        public void Pool_CanAcquireUpToMaxCapacity()
        {
            for (var i = 1; i <= 40; i++)
            {
                var view = _pool.Acquire(i);
                Assert.That(view, Is.Not.Null);
            }

            Assert.That(_pool.ActiveCount, Is.EqualTo(40));
            Assert.That(_pool.TotalInstantiatedCount, Is.EqualTo(40));
        }

        [Test]
        public void Pool_AcquireBeyondMaxCapacity_ThrowsInvalidOperationException()
        {
            for (var i = 1; i <= 40; i++)
            {
                _pool.Acquire(i);
            }

            Assert.Throws<InvalidOperationException>(() => _pool.Acquire(41),
                "Acquiring view beyond 40-view cap must be rejected.");
        }

        [Test]
        public void Pool_ReleasingView_ReturnsToPoolAndDeactivates()
        {
            var view = _pool.Acquire(1);
            Assert.That(_pool.ActiveCount, Is.EqualTo(1));

            var released = _pool.Release(1);

            Assert.That(released, Is.True);
            Assert.That(_pool.ActiveCount, Is.EqualTo(0));
            Assert.That(view.gameObject.activeSelf, Is.False);
            Assert.That(view.IsBound, Is.False);
        }

        [Test]
        public void Pool_ReacquiringReusesRecycledGameObjectWithoutInstantiatingNew()
        {
            var firstView = _pool.Acquire(1);
            Assert.That(_pool.TotalInstantiatedCount, Is.EqualTo(1));

            _pool.Release(1);
            Assert.That(_pool.ActiveCount, Is.EqualTo(0));

            var secondView = _pool.Acquire(2);

            Assert.That(_pool.ActiveCount, Is.EqualTo(1));
            Assert.That(_pool.TotalInstantiatedCount, Is.EqualTo(1), "Recycled GameObject must be reused; no new allocation.");
            Assert.That(secondView, Is.SameAs(firstView));
        }

        [Test]
        public void Pool_ReleaseAll_ClearsAllActiveViews()
        {
            for (var i = 1; i <= 15; i++)
            {
                _pool.Acquire(i);
            }

            Assert.That(_pool.ActiveCount, Is.EqualTo(15));

            _pool.ReleaseAll();

            Assert.That(_pool.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Pool_Prewarm_InstantiatesRequestedViewsUpToCap()
        {
            _pool.Prewarm(20);

            Assert.That(_pool.TotalInstantiatedCount, Is.EqualTo(20));
            Assert.That(_pool.ActiveCount, Is.EqualTo(0));

            // Prewarming beyond cap respects cap
            _pool.Prewarm(50);
            Assert.That(_pool.TotalInstantiatedCount, Is.EqualTo(40));
        }
    }
}
