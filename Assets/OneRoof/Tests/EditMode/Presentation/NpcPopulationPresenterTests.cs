using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Population;
using OneRoof.Presentation.Population;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class NpcPopulationPresenterTests
    {
        private GameObject _rootObject;
        private NpcPopulationPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("NpcPopulationPresenter_TestRoot");
            _presenter = _rootObject.AddComponent<NpcPopulationPresenter>();
            _presenter.EnsureDependencies();
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
        public void Presenter_FiftyPersistentProjections_BindsAtMostFortyViews()
        {
            // 50 residents distributed across 5 floors
            var fiftyProjections = CreateFiftyResidentProjections();
            Assert.That(fiftyProjections.Count, Is.EqualTo(50));

            _presenter.UpdatePresentation(fiftyProjections);

            Assert.That(_presenter.ViewPool.ActiveCount, Is.EqualTo(40),
                "Acceptance criterion: exactly 40 views are bound while 50 residents persist.");
            Assert.That(_presenter.ViewPool.TotalInstantiatedCount, Is.EqualTo(40));
        }

        [Test]
        public void Presenter_ChangingVisibleFloors_RecyclesViewsWithinBudget()
        {
            var fiftyProjections = CreateFiftyResidentProjections();

            // Frame 1: View only floors 0 and 1 (10 residents per floor = 20 residents)
            _presenter.VisibleFloors = new VisibleFloorRange(0, 1);
            _presenter.UpdatePresentation(fiftyProjections);

            var activeBefore = _presenter.ViewPool.ActiveCount;
            Assert.That(activeBefore, Is.EqualTo(40), "Top 40 are prioritized, with floors 0-1 scored highest.");

            // Frame 2: Move camera to floors 3 and 4
            _presenter.VisibleFloors = new VisibleFloorRange(3, 4);
            _presenter.UpdatePresentation(fiftyProjections);

            // Active count still 40, instantiated still 40
            Assert.That(_presenter.ViewPool.ActiveCount, Is.EqualTo(40));
            Assert.That(_presenter.ViewPool.TotalInstantiatedCount, Is.EqualTo(40));

            // Verify that views now prioritize floor 3 and 4 residents
            foreach (var view in _presenter.ViewPool.ActiveViews)
            {
                Assert.That(view.IsBound, Is.True);
                Assert.That(view.gameObject.activeSelf, Is.True);
            }
        }

        [Test]
        public void Presenter_RebindingSameProjections_UpdatesWithoutAllocatingNewViews()
        {
            var fiftyProjections = CreateFiftyResidentProjections();

            _presenter.UpdatePresentation(fiftyProjections);
            Assert.That(_presenter.ViewPool.TotalInstantiatedCount, Is.EqualTo(40));

            // Second update with same residents
            _presenter.UpdatePresentation(fiftyProjections);

            Assert.That(_presenter.ViewPool.TotalInstantiatedCount, Is.EqualTo(40),
                "No additional GameObjects must be instantiated on subsequent updates.");
            Assert.That(_presenter.ViewPool.ActiveCount, Is.EqualTo(40));
        }

        [Test]
        public void Presenter_AllActiveViews_AreBoundToValidProjections()
        {
            var fiftyProjections = CreateFiftyResidentProjections();
            _presenter.UpdatePresentation(fiftyProjections);

            foreach (var view in _presenter.ViewPool.ActiveViews)
            {
                Assert.That(view.BoundEntityId.HasValue, Is.True);
                Assert.That(view.BoundEntityId.Value, Is.InRange(1, 50));
                Assert.That(view.gameObject.activeSelf, Is.True);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(12)]
        public void CalculateWorldPosition_UsesSharedTowerFloorCoordinates(int floor)
        {
            var projection = new NpcProjection(1, 1, floor, 1, NpcActivityKind.Idle, false, null, null, 0, 2.5f);

            var position = NpcPopulationPresenter.CalculateWorldPosition(projection);

            Assert.That(position.x, Is.EqualTo(2.5f));
            Assert.That(position.y, Is.EqualTo(TowerStructurePresenter.FloorY(floor)).Within(0.0001f));
        }

        private static List<NpcProjection> CreateFiftyResidentProjections()
        {
            var list = new List<NpcProjection>(50);
            for (var i = 1; i <= 50; i++)
            {
                var floor = (i - 1) % 5;
                var roomId = 100 + floor * 10 + (i % 3);
                var isTransit = i % 4 == 0;
                var activity = isTransit ? NpcActivityKind.Idle : (i % 2 == 0 ? NpcActivityKind.Working : NpcActivityKind.Sleeping);
                var waitTicks = isTransit ? (i * 2) : 0;
                list.Add(new NpcProjection(
                    personId: i,
                    householdId: 1 + (i % 16),
                    floor: floor,
                    roomId: roomId,
                    currentActivity: activity,
                    isInTransit: isTransit,
                    destinationFloor: isTransit ? (floor + 1) % 5 : (int?)null,
                    destinationRoomId: isTransit ? 200 : (int?)null,
                    waitTicks: waitTicks,
                    horizontalPosition: 1.0f + (i % 10) * 0.4f));
            }

            return list;
        }
    }
}
