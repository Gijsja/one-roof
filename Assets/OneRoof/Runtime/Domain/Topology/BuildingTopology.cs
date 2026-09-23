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
        private readonly Dictionary<ContentId, IReadOnlyList<Room>> _roomsByContentType;
        private readonly Dictionary<EntityId, int> _roomOrder;

        public BuildingTopology(IEnumerable<FloorTopology> floors)
        {
            _floorsByLevel = new Dictionary<int, FloorTopology>();
            _allRooms = new Dictionary<EntityId, Room>();
            _allPortals = new Dictionary<EntityId, Portal>();
            var mutableRoomsByContentType = new Dictionary<ContentId, List<Room>>();

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
                        if (!mutableRoomsByContentType.TryGetValue(room.ContentType, out var roomsOfType))
                        {
                            roomsOfType = new List<Room>();
                            mutableRoomsByContentType.Add(room.ContentType, roomsOfType);
                        }
                        roomsOfType.Add(room);
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
            _roomOrder = new Dictionary<EntityId, int>(_allRooms.Count);
            var roomOrder = 0;
            for (var floorIndex = 0; floorIndex < floorList.Count; floorIndex++)
                for (var roomIndex = 0; roomIndex < floorList[floorIndex].Rooms.Count; roomIndex++)
                    _roomOrder.Add(floorList[floorIndex].Rooms[roomIndex].Id, roomOrder++);

            _roomsByContentType = new Dictionary<ContentId, IReadOnlyList<Room>>(mutableRoomsByContentType.Count);
            foreach (var entry in mutableRoomsByContentType)
            {
                entry.Value.Sort((a, b) => _roomOrder[a.Id].CompareTo(_roomOrder[b.Id]));
                _roomsByContentType.Add(entry.Key, new ReadOnlyCollection<Room>(entry.Value));
            }
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

        /// <summary>Returns rooms with the exact authored content ID, in topology order.</summary>
        public IReadOnlyList<Room> GetRoomsByContentType(ContentId contentType)
        {
            return _roomsByContentType.TryGetValue(contentType, out var rooms) ? rooms : Array.Empty<Room>();
        }

        /// <summary>
        /// Returns rooms whose content ID starts with the supplied prefix. This walks
        /// the compact content-type index, not every room, and is intended for setup-time
        /// queries whose result can be cached by the caller.
        /// </summary>
        public List<Room> GetRoomsByContentPrefix(string prefix)
        {
            return GetRoomsByContentPrefixes(prefix, null);
        }

        public List<Room> GetRoomsByContentPrefixes(string firstPrefix, string secondPrefix)
        {
            var matches = new List<Room>();
            if (string.IsNullOrEmpty(firstPrefix) && string.IsNullOrEmpty(secondPrefix)) return matches;
            foreach (var entry in _roomsByContentType)
            {
                var value = entry.Key.Value;
                var matchesFirst = !string.IsNullOrEmpty(firstPrefix) && value.StartsWith(firstPrefix, StringComparison.Ordinal);
                var matchesSecond = !string.IsNullOrEmpty(secondPrefix) && value.StartsWith(secondPrefix, StringComparison.Ordinal);
                if (!matchesFirst && !matchesSecond) continue;
                for (var i = 0; i < entry.Value.Count; i++) matches.Add(entry.Value[i]);
            }
            matches.Sort((a, b) =>
            {
                return _roomOrder[a.Id].CompareTo(_roomOrder[b.Id]);
            });
            return matches;
        }

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
