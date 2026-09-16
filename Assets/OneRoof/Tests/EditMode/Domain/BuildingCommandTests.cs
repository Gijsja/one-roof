using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class BuildingCommandTests
    {
        private BuildingTopologyState _state;
        private readonly Tick _testTick = new Tick(100);

        [SetUp]
        public void SetUp()
        {
            _state = new BuildingTopologyState();
        }

        [Test]
        public void BuildFloorSlab_ValidSequentialFloor_AcceptsAndEmitsEvent()
        {
            var cmd0 = new BuildFloorSlabCommand(0, -30, 30);
            var result0 = _state.Execute(cmd0, _testTick);

            Assert.That(result0.Accepted, Is.True);
            Assert.That(_state.FloorCount, Is.EqualTo(1));
            Assert.That(_state.HasFloor(0), Is.True);
            Assert.That(result0.Events.Count, Is.EqualTo(1));
            Assert.That(result0.Events[0].Type, Is.EqualTo(new ContentId("event:floor_slab_built")));

            var cmd1 = new BuildFloorSlabCommand(1, -30, 30);
            var result1 = _state.Execute(cmd1, _testTick);

            Assert.That(result1.Accepted, Is.True);
            Assert.That(_state.FloorCount, Is.EqualTo(2));
            Assert.That(_state.HasFloor(1), Is.True);
        }

        [Test]
        public void BuildFloorSlab_NegativeFloor_Rejected()
        {
            var cmd = new BuildFloorSlabCommand(-1, -10, 10);
            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:negative_floor")));
        }

        [Test]
        public void BuildFloorSlab_DuplicateFloor_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);
            var result = _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:duplicate_floor")));
        }

        [Test]
        public void BuildFloorSlab_GapInFloors_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);
            // Skipping floor 1 to build floor 2
            var result = _state.Execute(new BuildFloorSlabCommand(2, -20, 20), _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:unsupported_floor")));
        }

        [Test]
        public void BuildRoom_ValidBounds_AddsRoomAndPortalAndUpdatesGraph()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);

            var cmd = new BuildRoomCommand(
                floor: 0,
                minX: -5,
                maxX: 5,
                contentType: new ContentId("commercial:diner"),
                capacity: 15,
                portalX: 0);

            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.True);
            Assert.That(_state.GetRoomsOnFloor(0).Count, Is.EqualTo(1));
            Assert.That(_state.GetPortalsOnFloor(0).Count, Is.EqualTo(1));

            var room = _state.GetRoomsOnFloor(0)[0];
            Assert.That(room.ContentType, Is.EqualTo(new ContentId("commercial:diner")));
            Assert.That(room.Capacity, Is.EqualTo(15));
            Assert.That(room.Bounds.MinX, Is.EqualTo(-5));
            Assert.That(room.Bounds.MaxX, Is.EqualTo(5));

            var graph = _state.TransitGraph;
            Assert.That(graph.Nodes.Count, Is.EqualTo(1));
            Assert.That(graph.Nodes[0].Location.X, Is.EqualTo(0));
            Assert.That(graph.Nodes[0].Location.Floor, Is.EqualTo(0));
        }

        [Test]
        public void BuildRoom_OutsideFloorSlab_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -10, 10), _testTick);

            var cmd = new BuildRoomCommand(
                floor: 0,
                minX: -15,
                maxX: 0,
                contentType: new ContentId("residential:apartment"),
                capacity: 4);

            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:outside_floor_slab")));
        }

        [Test]
        public void BuildRoom_UnsupportedByFloorBelow_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -10, 10), _testTick);
            _state.Execute(new BuildFloorSlabCommand(1, -20, 20), _testTick);

            // Upper room on floor 1 spans -18 to -12, which has no slab support on floor 0 (starts at -10)
            var cmd = new BuildRoomCommand(
                floor: 1,
                minX: -18,
                maxX: -12,
                contentType: new ContentId("residential:apartment"),
                capacity: 4);

            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:unsupported_room")));
        }

        [Test]
        public void BuildRoom_OverlappingExistingRoom_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);
            _state.Execute(new BuildRoomCommand(0, 0, 5, new ContentId("commercial:diner"), 10), _testTick);

            var cmd = new BuildRoomCommand(0, 3, 8, new ContentId("residential:apartment"), 4);
            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:room_overlap")));
        }

        [Test]
        public void BuildRoom_TooNarrow_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);

            // Width is 1 cell (0..0)
            var cmd = new BuildRoomCommand(0, 0, 0, new ContentId("commercial:diner"), 10);
            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:room_too_narrow")));
        }

        [Test]
        public void BuildRoom_PortalOutsideBounds_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);

            var cmd = new BuildRoomCommand(0, 2, 6, new ContentId("commercial:diner"), 10, portalX: 10);
            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:portal_outside_room")));
        }

        [Test]
        public void DemolishRoom_ExistingRoom_RemovesRoomAndPortalsAndUpdatesGraph()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);
            _state.Execute(new BuildRoomCommand(0, 0, 5, new ContentId("commercial:diner"), 10), _testTick);

            var room = _state.GetRoomsOnFloor(0)[0];
            Assert.That(_state.TransitGraph.Nodes.Count, Is.EqualTo(1));

            var result = _state.Execute(new DemolishRoomCommand(room.Id), _testTick);

            Assert.That(result.Accepted, Is.True);
            Assert.That(_state.GetRoomsOnFloor(0).Count, Is.EqualTo(0));
            Assert.That(_state.GetPortalsOnFloor(0).Count, Is.EqualTo(0));
            Assert.That(_state.TransitGraph.Nodes.Count, Is.EqualTo(0));
        }

        [Test]
        public void DemolishRoom_NonExistentRoom_Rejected()
        {
            var result = _state.Execute(new DemolishRoomCommand(new EntityId(9999)), _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:room_not_found")));
        }

        [Test]
        public void AddElevatorShaft_ValidSpan_CreatesShaftPortalsAndVerticalElevatorEdges()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);
            _state.Execute(new BuildFloorSlabCommand(1, -20, 20), _testTick);
            _state.Execute(new BuildFloorSlabCommand(2, -20, 20), _testTick);

            var cmd = new AddElevatorShaftCommand(
                shaftMinX: 0,
                shaftMaxX: 1,
                bottomFloor: 0,
                topFloor: 2,
                carCapacity: 12);

            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.True);
            Assert.That(_state.GetRoomsOnFloor(0).Count, Is.EqualTo(1));
            Assert.That(_state.GetRoomsOnFloor(1).Count, Is.EqualTo(1));
            Assert.That(_state.GetRoomsOnFloor(2).Count, Is.EqualTo(1));

            var graph = _state.TransitGraph;
            Assert.That(graph.Nodes.Count, Is.EqualTo(3));
            Assert.That(graph.Nodes.All(n => n.Type == TransitNodeType.ElevatorStop), Is.True);

            // Should have bidirectional vertical elevator edges between all 3 stops
            var elevatorEdges = graph.Edges.Where(e => e.Mode == TransitMode.Elevator).ToList();
            // Pairs: (0,1), (1,0), (0,2), (2,0), (1,2), (2,1) = 6 edges
            Assert.That(elevatorEdges.Count, Is.EqualTo(6));
        }

        [Test]
        public void AddElevatorShaft_OverlappingRoom_Rejected()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);
            _state.Execute(new BuildFloorSlabCommand(1, -20, 20), _testTick);
            _state.Execute(new BuildRoomCommand(1, 0, 5, new ContentId("residential:apartment"), 4), _testTick);

            // Shaft at [0..1] overlaps the room at [0..5] on floor 1
            var cmd = new AddElevatorShaftCommand(0, 1, 0, 1);
            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:shaft_overlap")));
        }

        [Test]
        public void BuildStairwell_ValidSpan_CreatesStairLandingsAndVerticalWalkEdges()
        {
            _state.Execute(new BuildFloorSlabCommand(0, -20, 20), _testTick);
            _state.Execute(new BuildFloorSlabCommand(1, -20, 20), _testTick);

            var cmd = new BuildStairwellCommand(
                stairMinX: 5,
                stairMaxX: 6,
                bottomFloor: 0,
                topFloor: 1);

            var result = _state.Execute(cmd, _testTick);

            Assert.That(result.Accepted, Is.True);
            var graph = _state.TransitGraph;
            Assert.That(graph.Nodes.Count, Is.EqualTo(2));
            Assert.That(graph.Nodes.All(n => n.Type == TransitNodeType.StairLanding), Is.True);

            // Stair edges are Walk mode
            var stairEdges = graph.Edges.Where(e => e.Mode == TransitMode.Walk).ToList();
            Assert.That(stairEdges.Count, Is.EqualTo(2));
        }

        [Test]
        public void BuildingTopologyState_CreateWithFixture_InitializesCorrectlyAndMatchesSnapshot()
        {
            var state = BuildingTopologyState.CreateWithFixture();

            Assert.That(state.FloorCount, Is.EqualTo(5));
            Assert.That(state.Rooms.Count, Is.EqualTo(23)); // 3 on floor 0 + 5*4 on floors 1..4 = 23 rooms
            Assert.That(state.Portals.Count, Is.EqualTo(23));

            var snapshot = state.ToSnapshot();
            Assert.That(snapshot.FloorCount, Is.EqualTo(5));
            Assert.That(state.TransitGraph.Nodes.Count, Is.EqualTo(23));
        }
    }
}
