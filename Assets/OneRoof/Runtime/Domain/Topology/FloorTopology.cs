using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

namespace OneRoof.Domain.Topology
{
    public sealed class FloorTopology
    {
        private readonly Dictionary<EntityId, Room> _roomsById;
        private readonly Dictionary<EntityId, Portal> _portalsById;

        public FloorTopology(int floorLevel, IEnumerable<Room> rooms, IEnumerable<Portal> portals)
        {
            FloorLevel = floorLevel;
            _roomsById = new Dictionary<EntityId, Room>();
            _portalsById = new Dictionary<EntityId, Portal>();

            var roomList = new List<Room>();
            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    if (room == null)
                    {
                        throw new ArgumentNullException(nameof(rooms), "Room cannot be null.");
                    }

                    if (room.Floor != floorLevel)
                    {
                        throw new ArgumentException($"Room {room.Id} is on floor {room.Floor}, but expected floor {floorLevel}.");
                    }

                    if (_roomsById.ContainsKey(room.Id))
                    {
                        throw new ArgumentException($"Duplicate room ID {room.Id} on floor {floorLevel}.");
                    }

                    // Check for overlap with existing rooms on this floor
                    foreach (var existing in roomList)
                    {
                        if (existing.Overlaps(room))
                        {
                            throw new InvalidOperationException($"Room {room.Id} overlaps with room {existing.Id} on floor {floorLevel}.");
                        }
                    }

                    _roomsById.Add(room.Id, room);
                    roomList.Add(room);
                }
            }

            var portalList = new List<Portal>();
            if (portals != null)
            {
                foreach (var portal in portals)
                {
                    if (portal == null)
                    {
                        throw new ArgumentNullException(nameof(portals), "Portal cannot be null.");
                    }

                    if (portal.Location.Floor != floorLevel)
                    {
                        throw new ArgumentException($"Portal {portal.Id} is at floor {portal.Location.Floor}, expected {floorLevel}.");
                    }

                    if (_portalsById.ContainsKey(portal.Id))
                    {
                        throw new ArgumentException($"Duplicate portal ID {portal.Id} on floor {floorLevel}.");
                    }

                    _portalsById.Add(portal.Id, portal);
                    portalList.Add(portal);
                }
            }

            Rooms = new ReadOnlyCollection<Room>(roomList);
            Portals = new ReadOnlyCollection<Portal>(portalList);
        }

        public int FloorLevel { get; }

        public IReadOnlyList<Room> Rooms { get; }

        public IReadOnlyList<Portal> Portals { get; }

        public bool TryGetRoom(EntityId id, out Room room) => _roomsById.TryGetValue(id, out room);

        public bool TryGetPortal(EntityId id, out Portal portal) => _portalsById.TryGetValue(id, out portal);

        public Room GetRoomAt(CellCoordinate coordinate)
        {
            if (coordinate.Floor != FloorLevel)
            {
                return null;
            }

            foreach (var room in Rooms)
            {
                if (room.Contains(coordinate))
                {
                    return room;
                }
            }

            return null;
        }
    }
}
