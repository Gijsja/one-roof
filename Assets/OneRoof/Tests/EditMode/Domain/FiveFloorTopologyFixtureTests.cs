using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class FiveFloorTopologyFixtureTests
    {
        [Test]
        public void FixtureCreatesFiveFloorsWithExpectedFloorLevels()
        {
            var topology = FiveFloorTopologyFixture.Create();

            Assert.That(topology.FloorCount, Is.EqualTo(5));
            for (var level = 0; level < 5; level++)
            {
                Assert.That(topology.TryGetFloor(level, out var floor), Is.True);
                Assert.That(floor.FloorLevel, Is.EqualTo(level));
            }
        }

        [Test]
        public void FixtureConnectsElevatorPortalsOnEveryFloor()
        {
            var topology = FiveFloorTopologyFixture.Create();

            for (var level = 0; level < 5; level++)
            {
                var floor = topology.GetFloor(level);
                var elevatorPortal = floor.Portals.FirstOrDefault(p => p.Type == PortalType.ElevatorShaftDoor);

                Assert.That(elevatorPortal, Is.Not.Null);
                Assert.That(elevatorPortal.Location.X, Is.EqualTo(0));
                Assert.That(elevatorPortal.Location.Floor, Is.EqualTo(level));
            }
        }

        [Test]
        public void FixtureProvidesSufficientLobbyAndResidentialCapacityForFiftyResidents()
        {
            var topology = FiveFloorTopologyFixture.Create();

            var lobbyFloor = topology.GetFloor(0);
            var lobby = lobbyFloor.Rooms.FirstOrDefault(r => r.ContentType == FiveFloorTopologyFixture.LobbyContentId);
            Assert.That(lobby, Is.Not.Null);
            Assert.That(lobby.Capacity, Is.GreaterThanOrEqualTo(50));

            var totalResidentialCapacity = 0;
            for (var level = 1; level < 5; level++)
            {
                var floor = topology.GetFloor(level);
                var residentialRooms = floor.Rooms.Where(r => r.ContentType == FiveFloorTopologyFixture.ResidentialContentId);
                totalResidentialCapacity += residentialRooms.Sum(r => r.Capacity);
            }

            Assert.That(totalResidentialCapacity, Is.GreaterThanOrEqualTo(50));
        }

        [Test]
        public void RoomAtCoordinateReturnsCorrectRoom()
        {
            var topology = FiveFloorTopologyFixture.Create();

            var lobby = topology.GetRoomAt(new CellCoordinate(5, 0));
            Assert.That(lobby, Is.Not.Null);
            Assert.That(lobby.ContentType, Is.EqualTo(FiveFloorTopologyFixture.LobbyContentId));

            var upperRoom = topology.GetRoomAt(new CellCoordinate(4, 2));
            Assert.That(upperRoom, Is.Not.Null);
            Assert.That(upperRoom.ContentType, Is.EqualTo(FiveFloorTopologyFixture.ResidentialContentId));
            Assert.That(upperRoom.Floor, Is.EqualTo(2));
        }
    }
}
