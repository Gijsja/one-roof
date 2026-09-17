using System.Linq;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Topology;
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
            Assert.That(GridPlacementController.GetToolWidthInCells("commercial:office"), Is.EqualTo(8));
            Assert.That(GridPlacementController.GetToolWidthInCells("commercial:diner"), Is.EqualTo(10));
            Assert.That(GridPlacementController.GetToolWidthInCells("transit:elevator_car"), Is.EqualTo(2));
            Assert.That(GridPlacementController.GetToolWidthInCells("transit:elevator_shaft"), Is.EqualTo(2));
            Assert.That(GridPlacementController.GetToolWidthInCells("transit:stairwell"), Is.EqualTo(2));
            Assert.That(GridPlacementController.GetToolWidthInCells("demolish:room"), Is.EqualTo(1));
            Assert.That(GridPlacementController.GetToolWidthInCells("floor:slab"), Is.EqualTo(31));
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
        public void ValidatePlacement_RejectsFloorSlab_WhenFloorAlreadyExists()
        {
            var valid = _gridPlacement.ValidatePlacement("floor:slab", floor: 1, cellX: -12, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("slab already exists"));
        }

        [Test]
        public void ValidatePlacement_RejectsFloorSlab_WhenLowerFloorMissing()
        {
            var valid = _gridPlacement.ValidatePlacement("floor:slab", floor: 6, cellX: -12, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("without floor 5 slab below"));
        }

        [Test]
        public void ValidatePlacement_RejectsElevatorShaft_WhenGroundFloorOrBelow()
        {
            var valid = _gridPlacement.ValidatePlacement("transit:elevator_shaft", floor: 0, cellX: 0, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("must extend to floor 1 or higher"));
        }

        [Test]
        public void ValidatePlacement_RejectsElevatorShaft_WhenMisalignedFromColumn()
        {
            var valid = _gridPlacement.ValidatePlacement("transit:elevator_shaft", floor: 2, cellX: 5, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("must align with the central elevator column"));
        }

        [Test]
        public void ValidatePlacement_RejectsElevatorCar_WhenBankAtMaxCapacity()
        {
            // Initial fixture has 1 car; add 3 more to reach 4 (MaxCarsPerBank)
            _session.AddCapacity();
            _session.AddCapacity();
            _session.AddCapacity();

            var valid = _gridPlacement.ValidatePlacement("transit:elevator_car", floor: 0, cellX: 0, out var reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("maximum capacity"));
        }

        [Test]
        public void TryExecutePlacement_RejectsAndDoesNotPlace_WhenPlacementInvalid()
        {
            // Attempt to place an apartment overlapping an existing room on floor 1
            var success = _gridPlacement.TryExecutePlacement("residential:apartment", floor: 1, cellX: 2, out var result);

            Assert.That(success, Is.False);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections.Count, Is.GreaterThan(0));
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

            var success = _gridPlacement.TryExecutePlacement("floor:slab", floor: 5, cellX: 0, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(eventFired, Is.True);
            Assert.That(_session.FloorCount, Is.EqualTo(initialFloors + 1));
            Assert.That(_session.Topology.FloorSlabs.ContainsKey(5), Is.True);
            Assert.That(_session.Topology.FloorSlabs[5].MinX, Is.EqualTo(-14));
            Assert.That(_session.Topology.FloorSlabs[5].MaxX, Is.EqualTo(16));
        }

        [Test]
        public void TryGetToolPlacementBounds_SnapsFloorSlabToTowerBounds()
        {
            var hasBounds = _gridPlacement.TryGetToolPlacementBounds("floor:slab", floor: 5, cellX: 3, out var bounds);

            Assert.That(hasBounds, Is.True);
            Assert.That(bounds.MinX, Is.EqualTo(-14));
            Assert.That(bounds.MaxX, Is.EqualTo(16));
            Assert.That(bounds.Width, Is.EqualTo(31));
        }

        [Test]
        public void TryGetToolPlacementBounds_SnapsShaftToCentralColumn()
        {
            var hasBounds = _gridPlacement.TryGetToolPlacementBounds("transit:elevator_shaft", floor: 2, cellX: 1, out var bounds);

            Assert.That(hasBounds, Is.True);
            Assert.That(bounds.MinX, Is.EqualTo(0));
            Assert.That(bounds.MaxX, Is.EqualTo(1));
            Assert.That(bounds.Width, Is.EqualTo(2));
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

        [Test]
        public void IsPointerOverUI_IdentifiesBottomAndTopUIRegions()
        {
            // Bottom UI region (Mode bar, context overlay, build palette)
            // guiY is Screen.height - screenPos.y, so a screenPos near y=30 has guiY = Screen.height - 30 (in bottom UI)
            var bottomScreenPos = new Vector3(100f, 30f, 0f);
            Assert.That(GridPlacementController.IsPointerOverUI(bottomScreenPos), Is.True);

            // Center of screen (world area)
            var centerScreenPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Assert.That(GridPlacementController.IsPointerOverUI(centerScreenPos), Is.False);
        }

        [Test]
        public void Camera_Property_FindsCameraEvenWhenNotTaggedMainCamera()
        {
            var camObj = new GameObject("Untagged_Test_Camera");
            var cam = camObj.AddComponent<Camera>();
            camObj.tag = "Untagged";

            try
            {
                _gridPlacement.Camera = null;
                var foundCam = _gridPlacement.Camera;
                Assert.That(foundCam, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(camObj);
            }
        }

        [Test]
        public void DemolishTool_TryGetToolPlacementBounds_SnapsToHoveredRoom()
        {
            // Floor 1 has rooms, e.g. apartment at [-12..-7]
            var hasBounds = _gridPlacement.TryGetToolPlacementBounds("demolish:room", floor: 1, cellX: -10, out var bounds);

            Assert.That(hasBounds, Is.True);
            Assert.That(bounds.MinX, Is.EqualTo(-12));
            Assert.That(bounds.MaxX, Is.EqualTo(-7));
        }

        [Test]
        public void DemolishTool_ValidatePlacement_RejectsEmptyCellAndLobby()
        {
            // Empty cell on floor 4 at cellX = -14
            var validEmpty = _gridPlacement.ValidatePlacement("demolish:room", floor: 4, cellX: -14, out var emptyReason);
            Assert.That(validEmpty, Is.False);
            Assert.That(emptyReason, Does.Contain("No room"));

            // Reception lobby on floor 0
            var validLobby = _gridPlacement.ValidatePlacement("demolish:room", floor: 0, cellX: -5, out var lobbyReason);
            Assert.That(validLobby, Is.False);
            Assert.That(lobbyReason, Does.Contain("Cannot demolish main reception lobby"));
        }

        [Test]
        public void DemolishTool_TryExecutePlacement_DismantlesRoomAndFiresEvent()
        {
            // Build floor slab and apartment on floor 5
            _gridPlacement.TryExecutePlacement("floor:slab", floor: 5, cellX: 0, out _);
            _gridPlacement.TryExecutePlacement("residential:apartment", floor: 5, cellX: -10, out _);

            var roomCountBefore = _session.Topology.Rooms.Count;
            var eventFired = false;
            _gridPlacement.PlacementExecuted += res => eventFired = res.Accepted;

            var success = _gridPlacement.TryExecutePlacement("demolish:room", floor: 5, cellX: -8, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(eventFired, Is.True);
            Assert.That(_session.Topology.Rooms.Count, Is.EqualTo(roomCountBefore - 1));
        }

        [Test]
        public void StairwellTool_ValidatePlacement_RequiresValidAdjacentFloorsAndAvoidsElevatorShaft()
        {
            // Overlapping central elevator shaft [0..1]
            var shaftOverlap = _gridPlacement.ValidatePlacement("transit:stairwell", floor: 0, cellX: 0, out var shaftReason);
            Assert.That(shaftOverlap, Is.False);
            Assert.That(shaftReason, Does.Contain("elevator shaft column"));

            // Valid unoccupied cells on floor 0 [-14..-13]
            var validStair = _gridPlacement.ValidatePlacement("transit:stairwell", floor: 0, cellX: -14, out var validReason);
            Assert.That(validStair, Is.True, $"Expected valid stairwell, failed with: {validReason}");
        }

        [Test]
        public void StairwellTool_TryExecutePlacement_BuildsStairwellAndPortals()
        {
            var success = _gridPlacement.TryExecutePlacement("transit:stairwell", floor: 0, cellX: -14, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Accepted, Is.True);

            // Verify stairwell rooms were created on floors 0 and 1
            var roomsF0 = _session.Topology.GetRoomsOnFloor(0);
            var hasStairF0 = false;
            for (var i = 0; i < roomsF0.Count; i++)
            {
                if (roomsF0[i].ContentType.Value == "amenity:stairwell" && roomsF0[i].Bounds.MinX == -14)
                {
                    hasStairF0 = true;
                    break;
                }
            }
            Assert.That(hasStairF0, Is.True, "Expected amenity:stairwell room on floor 0");
        }

        [Test]
        public void OfficeTool_ValidateAndExecute_Builds8CellWorkplaceRoom()
        {
            // Build slab on floor 5
            _gridPlacement.TryExecutePlacement("floor:slab", floor: 5, cellX: 0, out _);

            var initialRooms = _session.Topology.Rooms.Count;
            var success = _gridPlacement.TryExecutePlacement("commercial:office", floor: 5, cellX: -10, out var result);

            Assert.That(success, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(_session.Topology.Rooms.Count, Is.EqualTo(initialRooms + 1));

            var roomsF5 = _session.Topology.GetRoomsOnFloor(5);
            Room office = null;
            for (var i = 0; i < roomsF5.Count; i++)
            {
                if (roomsF5[i].ContentType.Value == "commercial:office")
                {
                    office = roomsF5[i];
                    break;
                }
            }
            Assert.That(office, Is.Not.Null);
            Assert.That(office.Bounds.Width, Is.EqualTo(8));
            Assert.That(office.Capacity, Is.EqualTo(8));
        }
    }
}
