using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Application.Tower
{
    /// <summary>Stable, read-only geometry for binding tower views by room ID.</summary>
    public sealed class TowerTopologyProjection
    {
        private readonly Dictionary<int, IReadOnlyList<Room>> _roomsByFloor = new Dictionary<int, IReadOnlyList<Room>>();

        internal TowerTopologyProjection(BuildingTopologyState topology)
        {
            FloorCount = topology.FloorCount;
            Rooms = new ReadOnlyDictionary<EntityId, Room>(new Dictionary<EntityId, Room>(topology.Rooms));
            FloorSlabs = new ReadOnlyDictionary<int, CellBounds>(new Dictionary<int, CellBounds>(topology.FloorSlabs));
            for (var floor = 0; floor < FloorCount; floor++)
                _roomsByFloor[floor] = new ReadOnlyCollection<Room>(new List<Room>(topology.GetRoomsOnFloor(floor)));
        }

        public int FloorCount { get; }
        public IReadOnlyDictionary<EntityId, Room> Rooms { get; }
        public IReadOnlyDictionary<int, CellBounds> FloorSlabs { get; }

        public bool TryGetRoom(EntityId id, out Room room) => Rooms.TryGetValue(id, out room);
        public bool TryGetFloorSlab(int floor, out CellBounds slab) => FloorSlabs.TryGetValue(floor, out slab);
        public IReadOnlyList<Room> GetRoomsOnFloor(int floor) =>
            _roomsByFloor.TryGetValue(floor, out var rooms) ? rooms : System.Array.Empty<Room>();
    }
}
