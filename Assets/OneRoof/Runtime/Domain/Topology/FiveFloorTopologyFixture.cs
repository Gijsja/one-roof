using System.Collections.Generic;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Topology
{
    public static class FiveFloorTopologyFixture
    {
        public static readonly ContentId LobbyContentId = new ContentId("amenity:lobby");
        public static readonly ContentId CommercialContentId = new ContentId("commercial:diner");
        public static readonly ContentId ResidentialContentId = new ContentId("residential:studio");
        public static readonly ContentId ElevatorShaftContentId = new ContentId("transit:elevator_shaft");

        public static BuildingTopology Create()
        {
            var floors = new List<FloorTopology>();
            var nextEntityId = 1;

            // Floor 0: Lobby, Diner, Elevator Shaft
            {
                var floorLevel = 0;
                var pElevator = new Portal(new EntityId(nextEntityId++), PortalType.ElevatorShaftDoor, new CellCoordinate(0, floorLevel), new EntityId(nextEntityId + 2));
                var pDiner = new Portal(new EntityId(nextEntityId++), PortalType.Door, new CellCoordinate(-1, floorLevel), new EntityId(nextEntityId + 2));
                var pLobby = new Portal(new EntityId(nextEntityId++), PortalType.Door, new CellCoordinate(2, floorLevel), new EntityId(nextEntityId + 2));

                var rElevator = new Room(new EntityId(nextEntityId++), ElevatorShaftContentId, new CellBounds(floorLevel, 0, 1), new[] { pElevator.Id }, 10);
                var rDiner = new Room(new EntityId(nextEntityId++), CommercialContentId, new CellBounds(floorLevel, -10, -1), new[] { pDiner.Id }, 20);
                var rLobby = new Room(new EntityId(nextEntityId++), LobbyContentId, new CellBounds(floorLevel, 2, 14), new[] { pLobby.Id }, 50);

                floors.Add(new FloorTopology(
                    floorLevel,
                    new[] { rElevator, rDiner, rLobby },
                    new[] { pElevator, pDiner, pLobby }));
            }

            // Floors 1 to 4: Elevator shaft + 4 residential apartments per floor
            for (var floorLevel = 1; floorLevel <= 4; floorLevel++)
            {
                var pElevator = new Portal(new EntityId(nextEntityId++), PortalType.ElevatorShaftDoor, new CellCoordinate(0, floorLevel), new EntityId(nextEntityId + 4));
                var pFarWest = new Portal(new EntityId(nextEntityId++), PortalType.Door, new CellCoordinate(-7, floorLevel), new EntityId(nextEntityId + 4));
                var pWest = new Portal(new EntityId(nextEntityId++), PortalType.Door, new CellCoordinate(-1, floorLevel), new EntityId(nextEntityId + 4));
                var pEast = new Portal(new EntityId(nextEntityId++), PortalType.Door, new CellCoordinate(2, floorLevel), new EntityId(nextEntityId + 4));
                var pFarEast = new Portal(new EntityId(nextEntityId++), PortalType.Door, new CellCoordinate(8, floorLevel), new EntityId(nextEntityId + 4));

                var rElevator = new Room(new EntityId(nextEntityId++), ElevatorShaftContentId, new CellBounds(floorLevel, 0, 1), new[] { pElevator.Id }, 10);
                var rFarWest = new Room(new EntityId(nextEntityId++), ResidentialContentId, new CellBounds(floorLevel, -12, -7), new[] { pFarWest.Id }, 5);
                var rWest = new Room(new EntityId(nextEntityId++), ResidentialContentId, new CellBounds(floorLevel, -6, -1), new[] { pWest.Id }, 5);
                var rEast = new Room(new EntityId(nextEntityId++), ResidentialContentId, new CellBounds(floorLevel, 2, 7), new[] { pEast.Id }, 5);
                var rFarEast = new Room(new EntityId(nextEntityId++), ResidentialContentId, new CellBounds(floorLevel, 8, 13), new[] { pFarEast.Id }, 5);

                floors.Add(new FloorTopology(
                    floorLevel,
                    new[] { rElevator, rFarWest, rWest, rEast, rFarEast },
                    new[] { pElevator, pFarWest, pWest, pEast, pFarEast }));
            }

            return new BuildingTopology(floors);
        }
    }
}
