using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Transit
{
    public static class TwoFloorTransitFixture
    {
        public static readonly ContentId LobbyContentId = new ContentId("amenity:lobby");
        public static readonly ContentId OfficeContentId = new ContentId("commercial:office");

        public static BuildingTopology CreateTopology()
        {
            var floors = new List<FloorTopology>();

            // Floor 0
            var pElevator0 = new Portal(new EntityId(1), PortalType.ElevatorShaftDoor, new CellCoordinate(0, 0), new EntityId(3));
            var pLobby = new Portal(new EntityId(2), PortalType.Door, new CellCoordinate(2, 0), new EntityId(4));
            var rShaft0 = new Room(new EntityId(3), new ContentId("transit:shaft"), new CellBounds(0, 0, 1), new[] { pElevator0.Id }, 5);
            var rLobby = new Room(new EntityId(4), LobbyContentId, new CellBounds(0, 2, 5), new[] { pLobby.Id }, 20);
            floors.Add(new FloorTopology(0, new[] { rShaft0, rLobby }, new[] { pElevator0, pLobby }));

            // Floor 1
            var pElevator1 = new Portal(new EntityId(5), PortalType.ElevatorShaftDoor, new CellCoordinate(0, 1), new EntityId(7));
            var pOffice = new Portal(new EntityId(6), PortalType.Door, new CellCoordinate(2, 1), new EntityId(8));
            var rShaft1 = new Room(new EntityId(7), new ContentId("transit:shaft"), new CellBounds(1, 0, 1), new[] { pElevator1.Id }, 5);
            var rOffice = new Room(new EntityId(8), OfficeContentId, new CellBounds(1, 2, 5), new[] { pOffice.Id }, 10);
            floors.Add(new FloorTopology(1, new[] { rShaft1, rOffice }, new[] { pElevator1, pOffice }));

            return new BuildingTopology(floors);
        }

        public static HierarchicalTransitGraph CreateGraph()
        {
            return HierarchicalTransitGraph.FromBuildingTopology(CreateTopology());
        }
    }
}
