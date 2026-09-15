using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Topology
{
    public sealed class BuildingTopology
    {
        private readonly Dictionary<int, FloorTopology> _floorsByLevel;
        private readonly Dictionary<EntityId, Room> _allRooms;
        private readonly Dictionary<EntityId, Portal> _allPortals;

        public BuildingTopology(IEnumerable<FloorTopology> floors)
        {
            _floorsByLevel = new Dictionary<int, FloorTopology>();
            _allRooms = new Dictionary<EntityId, Room>();
            _allPortals = new Dictionary<EntityId, Portal>();

            var floorList = new List<FloorTopology>();
            if (floors != null)
            {
                foreach (var floor in floors)
                {
                    if (floor == null)
                    {
                        throw new ArgumentNullException(nameof(floors), "Floor topology cannot be null.");
                    }

                    if (_floorsByLevel.ContainsKey(floor.FloorLevel))
                    {
                        throw new ArgumentException($"Duplicate floor level {floor.FloorLevel} in building topology.");
                    }

                    _floorsByLevel.Add(floor.FloorLevel, floor);
                    floorList.Add(floor);

                    foreach (var room in floor.Rooms)
                    {
                        if (_allRooms.ContainsKey(room.Id))
                        {
                            throw new ArgumentException($"Duplicate room ID {room.Id} across building topology.");
                        }

                        _allRooms.Add(room.Id, room);
                    }

                    foreach (var portal in floor.Portals)
                    {
                        if (_allPortals.ContainsKey(portal.Id))
                        {
                            throw new ArgumentException($"Duplicate portal ID {portal.Id} across building topology.");
                        }

                        _allPortals.Add(portal.Id, portal);
                    }
                }
            }

            floorList.Sort((a, b) => a.FloorLevel.CompareTo(b.FloorLevel));
            Floors = new ReadOnlyCollection<FloorTopology>(floorList);
        }

        public IReadOnlyList<FloorTopology> Floors { get; }

        public int FloorCount => Floors.Count;

        public bool TryGetFloor(int floorLevel, out FloorTopology floor) => _floorsByLevel.TryGetValue(floorLevel, out floor);

        public FloorTopology GetFloor(int floorLevel)
        {
            if (!_floorsByLevel.TryGetValue(floorLevel, out var floor))
            {
                throw new KeyNotFoundException($"Floor level {floorLevel} not found in building topology.");
            }

            return floor;
        }

        public bool TryGetRoom(EntityId id, out Room room) => _allRooms.TryGetValue(id, out room);

        public bool TryGetPortal(EntityId id, out Portal portal) => _allPortals.TryGetValue(id, out portal);

        public Room GetRoomAt(CellCoordinate coordinate)
        {
            if (_floorsByLevel.TryGetValue(coordinate.Floor, out var floor))
            {
                return floor.GetRoomAt(coordinate);
            }

            return null;
        }
    }
}
