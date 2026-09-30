using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class ElevatorShaftWideningTests
    {
        private BuildingTopologyState _state;
        private static readonly Tick TestTick = new Tick(17);

        [SetUp]
        public void SetUp()
        {
            _state = new BuildingTopologyState();
            for (var floor = 0; floor <= 4; floor++)
                Assert.That(_state.Execute(new BuildFloorSlabCommand(floor, -10, 10), TestTick).Accepted, Is.True);
        }

        [Test]
        public void Widen_AlignedSpan_ExpandsAllRoomsPreservingRoomAndPortalIdsAndRebuildsGraph()
        {
            _state.Execute(new AddElevatorShaftCommand(0, 1, 0, 2), TestTick);
            var oldRoomIds = Enumerable.Range(0, 3).Select(f => _state.GetRoomsOnFloor(f)[0].Id).ToArray();
            var oldPortalIds = Enumerable.Range(0, 3).Select(f => _state.GetPortalsOnFloor(f)[0].Id).ToArray();
            var oldGraph = _state.TransitGraph;

            var result = _state.Execute(new WidenElevatorShaftCommand(0, 2, ElevatorShaftWideningSide.Right), TestTick);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events.Single().Type, Is.EqualTo(new ContentId("event:elevator_shaft_widened")));
            Assert.That(result.Events.Single().AffectedEntityIds.Count, Is.EqualTo(6));
            for (var floor = 0; floor <= 2; floor++)
            {
                Assert.That(_state.GetRoomsOnFloor(floor)[0].Bounds, Is.EqualTo(new CellBounds(floor, 0, 2)));
                Assert.That(_state.GetRoomsOnFloor(floor)[0].Id, Is.EqualTo(oldRoomIds[floor]));
                Assert.That(_state.GetPortalsOnFloor(floor)[0].Id, Is.EqualTo(oldPortalIds[floor]));
            }
            Assert.That(_state.TransitGraph, Is.Not.SameAs(oldGraph));
        }

        [Test]
        public void WidenLeft_RoundTripsBoundsAndStablePortalIds()
        {
            _state.Execute(new AddElevatorShaftCommand(0, 1, 0, 2), TestTick);
            var portalIds = Enumerable.Range(0, 3).Select(f => _state.GetPortalsOnFloor(f)[0].Id).ToArray();

            Assert.That(_state.Execute(new WidenElevatorShaftCommand(0, 2, ElevatorShaftWideningSide.Left), TestTick).Accepted, Is.True);
            var restored = BuildingTopologyState.FromSaveData(_state.ToSaveData());

            for (var floor = 0; floor <= 2; floor++)
            {
                Assert.That(restored.GetRoomsOnFloor(floor)[0].Bounds, Is.EqualTo(new CellBounds(floor, -1, 1)));
                Assert.That(restored.GetPortalsOnFloor(floor)[0].Id, Is.EqualTo(portalIds[floor]));
            }
        }

        [Test]
        public void Widen_ShaftGapInSelectedFloors_RejectsWithoutPartialChanges()
        {
            _state.Execute(new AddElevatorShaftCommand(0, 1, 0, 1), TestTick);
            _state.Execute(new AddElevatorShaftCommand(0, 1, 3, 4), TestTick);

            var result = _state.Execute(new WidenElevatorShaftCommand(0, 3, ElevatorShaftWideningSide.Left), TestTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:shaft_misaligned")));
            Assert.That(_state.GetRoomsOnFloor(0)[0].Bounds, Is.EqualTo(new CellBounds(0, 0, 1)));
        }

        [Test]
        public void Widen_MisalignedShaftWidths_Rejects()
        {
            _state.Execute(new AddElevatorShaftCommand(0, 1, 0, 1), TestTick);
            _state.Execute(new AddElevatorShaftCommand(0, 2, 2, 3), TestTick);

            var result = _state.Execute(new WidenElevatorShaftCommand(0, 3, ElevatorShaftWideningSide.Right), TestTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:shaft_misaligned")));
        }

        [Test]
        public void Widen_OverlapOnLaterFloor_RejectsAtomically()
        {
            _state.Execute(new AddElevatorShaftCommand(0, 1, 0, 1), TestTick);
            _state.Execute(new BuildRoomCommand(1, 2, 5, new ContentId("commercial:diner"), 8), TestTick);

            var result = _state.Execute(new WidenElevatorShaftCommand(0, 1, ElevatorShaftWideningSide.Right), TestTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:shaft_overlap")));
            Assert.That(_state.GetRoomsOnFloor(0).Single(r => r.ContentType == new ContentId("transit:elevator_shaft")).Bounds,
                Is.EqualTo(new CellBounds(0, 0, 1)));
        }

        [Test]
        public void Widen_MissingShaftOnSelectedFloor_Rejects()
        {
            var result = _state.Execute(new WidenElevatorShaftCommand(0, 1, ElevatorShaftWideningSide.Right), TestTick);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("topology:shaft_misaligned")));
        }
    }
}
