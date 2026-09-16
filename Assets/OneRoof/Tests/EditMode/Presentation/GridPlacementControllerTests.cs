using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Economy;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class GridPlacementControllerTests
    {
        private GameObject _holder;
        private GridPlacementController _gridPlacement;
        private PlacementGhostPresenter _ghostPresenter;
        private TowerSimulationSession _session;
        private ModeShellSession _modeSession;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_GridPlacement_World");
            _session = new TowerSimulationSession();
            _modeSession = new ModeShellSession();

            _ghostPresenter = _holder.AddComponent<PlacementGhostPresenter>();
            _gridPlacement = _holder.AddComponent<GridPlacementController>();

            _gridPlacement.SimulationSession = _session;
            _gridPlacement.ModeSession = _modeSession;
            _gridPlacement.GhostPresenter = _ghostPresenter;
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void CellToWorld_And_TryGetCellFromWorld_RoundTripAccurately()
        {
            for (var floor = 0; floor <= 6; floor++)
            {
                for (var cellX = -15; cellX <= 15; cellX += 3)
                {
                    var worldPos = _gridPlacement.CellToWorld(floor, cellX, widthInCells: 1);
                    var success = _gridPlacement.TryGetCellFromWorld(worldPos, out var recoveredFloor, out var recoveredCellX);

                    Assert.That(success, Is.True);
                    Assert.That(recoveredFloor, Is.EqualTo(floor), $"Floor mismatch at {floor}, {cellX}");
                    Assert.That(recoveredCellX, Is.EqualTo(cellX), $"CellX mismatch at {floor}, {cellX}");
                }
            }
        }

        [Test]
        public void GetToolWidthInCells_ReturnsCorrectDimensions()
        {
            Assert.That(GridPlacementController.GetToolWidthInCells("residential:apartment"), Is.EqualTo(6));
            Assert.That(GridPlacementController.GetToolWidthInCells("room:apartment"), Is.EqualTo(6));
            Assert.That(GridPlacementController.GetToolWidthInCells("commercial:diner"), Is.EqualTo(10));
            Assert.That(GridPlacementController.GetToolWidthInCells("transit:elevator_car"), Is.EqualTo(2));
            Assert.That(GridPlacementController.GetToolWidthInCells("transit:elevator_shaft"), Is.EqualTo(2));
            Assert.That(GridPlacementController.GetToolWidthInCells("floor:slab"), Is.EqualTo(24));
        }

        [Test]
        public void ValidatePlacement_RejectsRoomWhenFloorHasNoSlab()
        {
            var valid = _gridPlacement.ValidatePlacement("residential:apartment", floor: 5, cellX: 0, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("has no slab"));
        }

        [Test]
        public void ValidatePlacement_RejectsRoomWhenExtendingBeyondSlabEdge()
        {
            // Floor 1 slab is [-14..16]. Placing apartment (width 6) at cellX = 14 extends to 19.
            var valid = _gridPlacement.ValidatePlacement("residential:apartment", floor: 1, cellX: 14, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("exceeds floor 1 slab boundaries"));
        }

        [Test]
        public void ValidatePlacement_RejectsRoomWhenOverlappingExistingRoom()
        {
            // Floor 1 has rooms at -12..-7, -6..-1, 0..1 (shaft), 2..7, 8..13.
            // Placing at cellX = 2 overlaps 2..7.
            var valid = _gridPlacement.ValidatePlacement("residential:apartment", floor: 1, cellX: 2, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Overlaps existing room"));
        }

        [Test]
        public void ValidatePlacement_RejectsWhenTreasuryCannotAfford()
        {
            _session.Economy.SandboxMode = false;
            _session.Economy.TryDeduct(_session.Economy.CashBalance); // deplete treasury to 0

            var valid = _gridPlacement.ValidatePlacement("transit:elevator_car", floor: 0, cellX: 0, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("Insufficient funds"));
        }

        [Test]
        public void TryExecutePlacement_BuildsFloorSlabAndFiresEvent()
        {
            var initialFloors = _session.FloorCount;
            var eventFired = false;
            _gridPlacement.PlacementExecuted += res => eventFired = res.Accepted;

            var success = _gridPlacement.TryExecutePlacement("floor:slab", floor: 5, cellX: -12, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(eventFired, Is.True);
            Assert.That(_session.FloorCount, Is.EqualTo(initialFloors + 1));
            Assert.That(_session.Topology.FloorSlabs.ContainsKey(5), Is.True);
        }

        [Test]
        public void TryExecutePlacement_BuildsRoomOnNewFloorSlab()
        {
            // First build floor slab on floor 5
            _gridPlacement.TryExecutePlacement("floor:slab", floor: 5, cellX: -12, out _);

            var initialRooms = _session.Topology.Rooms.Count;
            var success = _gridPlacement.TryExecutePlacement("residential:apartment", floor: 5, cellX: -10, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(_session.Topology.Rooms.Count, Is.EqualTo(initialRooms + 1));
        }

        [Test]
        public void TryExecutePlacement_AddsElevatorCar()
        {
            var initialCars = _session.ElevatorBank.Cars.Count;

            var success = _gridPlacement.TryExecutePlacement("transit:elevator_car", floor: 0, cellX: 0, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(_session.ElevatorBank.Cars.Count, Is.EqualTo(initialCars + 1));
        }

        [Test]
        public void GhostPresenter_ShowAndHide_TogglesVisibilityAndValidity()
        {
            Assert.That(_ghostPresenter.IsVisible, Is.False);

            _ghostPresenter.ShowGhost(new Vector3(1f, 2f, 0f), new Vector2(3f, 1.5f), isValid: true);

            Assert.That(_ghostPresenter.IsVisible, Is.True);
            Assert.That(_ghostPresenter.IsValid, Is.True);
            Assert.That(_ghostPresenter.CurrentPosition.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(_ghostPresenter.CurrentPosition.y, Is.EqualTo(2f).Within(0.01f));

            _ghostPresenter.ShowGhost(new Vector3(2f, 3f, 0f), new Vector2(3f, 1.5f), isValid: false);
            Assert.That(_ghostPresenter.IsValid, Is.False);

            _ghostPresenter.HideGhost();
            Assert.That(_ghostPresenter.IsVisible, Is.False);
        }
    }
}
