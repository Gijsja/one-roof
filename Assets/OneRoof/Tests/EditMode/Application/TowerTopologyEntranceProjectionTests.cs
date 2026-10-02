using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class TowerTopologyEntranceProjectionTests
    {
        [Test]
        public void Entrance_UsesActualDoorPortal_AndCopiesSourceCollections()
        {
            var roomId = new EntityId(1);
            var doorId = new EntityId(2);
            var shaftId = new EntityId(3);
            var room = new Room(roomId, new ContentId("residential:studio"), new CellBounds(2, -10, -3),
                new[] { shaftId, doorId }, 4);
            var rooms = new Dictionary<EntityId, Room> { [roomId] = room };
            var portals = new Dictionary<EntityId, Portal>
            {
                [shaftId] = new Portal(shaftId, PortalType.ElevatorShaftDoor, new CellCoordinate(-10, 2), roomId),
                [doorId] = new Portal(doorId, PortalType.Door, new CellCoordinate(-4, 2), roomId)
            };
            var projection = new TowerTopologyProjection(new Dictionary<int, CellBounds> { [2] = new CellBounds(2, -14, 16) }, rooms, portals);
            rooms.Clear();
            portals.Clear();
            Assert.That(projection.TryGetRoomEntrance(roomId, out var entrance), Is.True);
            Assert.That(entrance, Is.EqualTo(new CellCoordinate(-4, 2)), "Entrance must honor the authored door, rather than room center or shaft portal.");
            Assert.That(projection.Portals.Count, Is.EqualTo(2));
        }

        [TestCase(PortalType.StairwellDoor)]
        [TestCase(PortalType.ElevatorShaftDoor)]
        public void Entrance_RejectsRoomsWithOnlyTransitPortals_AndMissingRooms(PortalType type)
        {
            var roomId = new EntityId(1);
            var portalId = new EntityId(2);
            var room = new Room(roomId, new ContentId("service:maintenance"), new CellBounds(0, 0, 3), new[] { portalId }, 2);
            var projection = new TowerTopologyProjection(new Dictionary<int, CellBounds>(),
                new Dictionary<EntityId, Room> { [roomId] = room },
                new Dictionary<EntityId, Portal> { [portalId] = new Portal(portalId, type, new CellCoordinate(1, 0), roomId) });
            Assert.That(projection.TryGetRoomEntrance(roomId, out _), Is.False);
            Assert.That(projection.TryGetRoomEntrance(new EntityId(99), out _), Is.False);
        }
    }
}
