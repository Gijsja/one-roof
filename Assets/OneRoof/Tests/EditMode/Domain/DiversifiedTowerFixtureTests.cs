using System.Linq;
using System.Collections.Generic;
using OneRoof.Domain.Transit;
using NUnit.Framework;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class DiversifiedTowerFixtureTests
    {
        [TestCase(6)]
        [TestCase(ElevatorBank.MaxSupportedFloor + 1)]
        public void Create_ReservesDisjointEntityIdsAndRuntimeAllocators(int floorCount)
        {
            var sim = DiversifiedTowerFixture.Create(floorCount);
            var ids = new HashSet<int>();
            void Check(int id) => Assert.That(ids.Add(id), Is.True, $"Duplicate entity ID {id}");
            foreach (var room in sim.Topology.Rooms.Values) Check(room.Id.Value);
            foreach (var portal in sim.Topology.Portals.Values) Check(portal.Id.Value);
            foreach (var car in sim.ElevatorBank.Cars) Check(car.Id.Value);
            foreach (var household in sim.Population.Households) Check(household.Id.Value);
            foreach (var person in sim.Population.Persons) Check(person.Id.Value);

            var save = sim.ExportSaveData();
            Assert.That(save.nextEntityId, Is.GreaterThan(ids.Max()));
            Check(sim.Topology.AllocateId().Value);
            Assert.That(sim.AddElevatorCar().Accepted, Is.True);
            Check(sim.ElevatorBank.Cars.Last().Id.Value);
        }

        [Test]
        public void Create_GeneratesMixOfOneByOneTwoByTwoAndThreeByThreeRooms()
        {
            var sim = DiversifiedTowerFixture.Create(floorCount: 6);

            var residentialRooms = sim.Topology.Rooms.Values
                .Where(r => r.ContentType.Value.StartsWith("residential:", System.StringComparison.Ordinal))
                .ToList();

            Assert.That(residentialRooms.Count, Is.GreaterThan(0));

            // Check that we have 1x1 (3 cells), 2x2 (6 cells), and 3x3 (9 cells)
            var has1x1 = residentialRooms.Any(r => r.Bounds.Width == 3);
            var has2x2 = residentialRooms.Any(r => r.Bounds.Width == 6);
            var has3x3 = residentialRooms.Any(r => r.Bounds.Width == 9);

            Assert.That(has1x1, Is.True, "Should generate 1x1 (3-cell) rooms");
            Assert.That(has2x2, Is.True, "Should generate 2x2 (6-cell) rooms");
            Assert.That(has3x3, Is.True, "Should generate 3x3 (9-cell) rooms");
        }

        [Test]
        public void Create_HouseholdsMatchMaxInhabitantsPerRoom()
        {
            var sim = DiversifiedTowerFixture.Create(floorCount: 6);

            foreach (var household in sim.Population.Households)
            {
                Assert.That(sim.Topology.TryGetRoom(household.HomeRoomId, out var room), Is.True);

                // Inhabitants in the household must strictly match the room's authored capacity
                Assert.That(household.MemberIds.Count, Is.EqualTo(room.Capacity),
                    $"Household {household.Id} member count ({household.MemberIds.Count}) must match room {room.Id} capacity ({room.Capacity})");

                if (room.Bounds.Width == 3)
                {
                    Assert.That(room.Capacity, Is.EqualTo(1), "1x1 rooms must have capacity 1");
                }
                else if (room.Bounds.Width == 6)
                {
                    Assert.That(room.Capacity, Is.EqualTo(2), "2x2 rooms must have capacity 2");
                }
                else if (room.Bounds.Width == 9)
                {
                    Assert.That(room.Capacity, Is.EqualTo(3), "3x3 rooms must have capacity 3");
                }
            }
        }

        [Test]
        public void Create_NoRoomsOverlapAndTransitRoutesExist()
        {
            var sim = DiversifiedTowerFixture.Create(floorCount: 6);

            for (var floor = 0; floor < sim.Topology.FloorCount; floor++)
            {
                var rooms = sim.Topology.GetRoomsOnFloor(floor);
                for (var a = 0; a < rooms.Count; a++)
                {
                    for (var b = a + 1; b < rooms.Count; b++)
                    {
                        var overlap = rooms[a].Bounds.Overlaps(rooms[b].Bounds);
                        Assert.That(overlap, Is.False,
                            $"Floor {floor}: Room {rooms[a].Id} [{rooms[a].Bounds}] overlaps Room {rooms[b].Id} [{rooms[b].Bounds}]");
                    }
                }
            }

            var lobby = sim.Topology.Rooms.Values.First(r => r.ContentType.Value == "amenity:lobby");
            var destNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(lobby.Id);
            Assert.That(destNode, Is.Not.Null);

            foreach (var household in sim.Population.Households)
            {
                var originNode = sim.Topology.TransitGraph.GetPortalNodeForRoom(household.HomeRoomId);
                Assert.That(originNode, Is.Not.Null);
                var route = sim.Planner.FindRoute(originNode.Id, destNode.Id);
                Assert.That(route, Is.Not.Null, $"Route from {household.HomeRoomId} to lobby should exist");
            }
        }
    }
}
