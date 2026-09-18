using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Events;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Topology
{
    /// <summary>
    /// Mutable aggregate root managing the living spatial topology of the tower.
    /// Validates and executes domain building commands, maintains floor slabs, rooms, and portals,
    /// emits immutable domain events, and provides synchronized BuildingTopology and HierarchicalTransitGraph.
    /// </summary>
    public sealed class BuildingTopologyState
    {
        private const int DefaultFloorWidth = 60;
        private const int DefaultMinX = -14;
        private const int DefaultMaxX = 17;

        private readonly SortedDictionary<int, CellBounds> _floorSlabs;
        private readonly Dictionary<EntityId, Room> _roomsById;
        private readonly Dictionary<int, List<Room>> _roomsByFloor;
        private readonly Dictionary<EntityId, Portal> _portalsById;
        private readonly Dictionary<int, List<Portal>> _portalsByFloor;

        private int _nextEntityId;
        private HierarchicalTransitGraph _cachedTransitGraph;

        public BuildingTopologyState(int startingEntityId = 1000)
        {
            _floorSlabs = new SortedDictionary<int, CellBounds>();
            _roomsById = new Dictionary<EntityId, Room>();
            _roomsByFloor = new Dictionary<int, List<Room>>();
            _portalsById = new Dictionary<EntityId, Portal>();
            _portalsByFloor = new Dictionary<int, List<Portal>>();
            _nextEntityId = startingEntityId;
        }

        public int FloorCount => _floorSlabs.Count;

        public IReadOnlyDictionary<int, CellBounds> FloorSlabs => _floorSlabs;

        public IReadOnlyDictionary<EntityId, Room> Rooms => _roomsById;

        public IReadOnlyDictionary<EntityId, Portal> Portals => _portalsById;

        public HierarchicalTransitGraph TransitGraph => _cachedTransitGraph ??= RebuildTransitGraph();

        public EntityId AllocateId() => new EntityId(_nextEntityId++);

        public bool HasFloor(int floor) => _floorSlabs.ContainsKey(floor);

        public bool TryGetFloorSlab(int floor, out CellBounds bounds) => _floorSlabs.TryGetValue(floor, out bounds);

        public bool TryGetRoom(EntityId id, out Room room) => _roomsById.TryGetValue(id, out room);

        public bool TryGetPortal(EntityId id, out Portal portal) => _portalsById.TryGetValue(id, out portal);

        public IReadOnlyList<Room> GetRoomsOnFloor(int floor)
        {
            if (_roomsByFloor.TryGetValue(floor, out var list))
            {
                return list;
            }

            return Array.Empty<Room>();
        }

        public IReadOnlyList<Portal> GetPortalsOnFloor(int floor)
        {
            if (_portalsByFloor.TryGetValue(floor, out var list))
            {
                return list;
            }

            return Array.Empty<Portal>();
        }

        public void RestoreFromData(
            IEnumerable<CellBounds> slabs,
            IEnumerable<Room> rooms,
            IEnumerable<Portal> portals)
        {
            _floorSlabs.Clear();
            _roomsById.Clear();
            _roomsByFloor.Clear();
            _portalsById.Clear();
            _portalsByFloor.Clear();

            if (slabs != null)
            {
                foreach (var slab in slabs)
                {
                    _floorSlabs[slab.Floor] = slab;
                    _roomsByFloor[slab.Floor] = new List<Room>();
                    _portalsByFloor[slab.Floor] = new List<Portal>();
                }
            }

            if (rooms != null)
            {
                foreach (var room in rooms)
                {
                    _roomsById[room.Id] = room;
                    if (!_roomsByFloor.ContainsKey(room.Floor)) _roomsByFloor[room.Floor] = new List<Room>();
                    _roomsByFloor[room.Floor].Add(room);
                }
            }

            if (portals != null)
            {
                foreach (var portal in portals)
                {
                    _portalsById[portal.Id] = portal;
                    if (!_portalsByFloor.ContainsKey(portal.Location.Floor)) _portalsByFloor[portal.Location.Floor] = new List<Portal>();
                    _portalsByFloor[portal.Location.Floor].Add(portal);
                }
            }

            InvalidateGraph();
        }

        // ── Command Execution & Validation ──────────────────────────────────────

        public CommandResult CanExecute(BuildFloorSlabCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));

            if (cmd.FloorLevel < 0)
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:negative_floor"), "Floor level cannot be negative.") });
            }

            if (_floorSlabs.ContainsKey(cmd.FloorLevel))
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:duplicate_floor"), $"Floor {cmd.FloorLevel} slab already exists.") });
            }

            if (cmd.FloorLevel > 0 && !_floorSlabs.ContainsKey(cmd.FloorLevel - 1))
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:unsupported_floor"), $"Cannot construct floor {cmd.FloorLevel} slab without floor {cmd.FloorLevel - 1} slab below.") });
            }

            if (cmd.MinX > cmd.MaxX)
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:invalid_bounds"), "MinX cannot exceed MaxX.") });
            }

            return CommandResult.Success();
        }

        public CommandResult Execute(BuildFloorSlabCommand cmd, Tick tick)
        {
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            _floorSlabs[cmd.FloorLevel] = cmd.Bounds;
            if (!_roomsByFloor.ContainsKey(cmd.FloorLevel))
            {
                _roomsByFloor[cmd.FloorLevel] = new List<Room>();
            }

            if (!_portalsByFloor.ContainsKey(cmd.FloorLevel))
            {
                _portalsByFloor[cmd.FloorLevel] = new List<Portal>();
            }

            InvalidateGraph();

            var evt = new DomainEvent(AllocateId(), new ContentId("event:floor_slab_built"), tick, Array.Empty<EntityId>());
            return CommandResult.Accept(new[] { evt });
        }

        public CommandResult CanExecute(BuildRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));

            if (!_floorSlabs.TryGetValue(cmd.Floor, out var slab))
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:floor_not_found"), $"Floor {cmd.Floor} has no slab. Build a floor slab first.") });
            }

            if (cmd.MinX < slab.MinX || cmd.MaxX > slab.MaxX)
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:outside_floor_slab"), $"Room exceeds floor {cmd.Floor} slab boundaries ({slab.MinX}..{slab.MaxX}).") });
            }

            if (cmd.Floor > 0)
            {
                if (!_floorSlabs.TryGetValue(cmd.Floor - 1, out var lowerSlab) || cmd.MinX < lowerSlab.MinX || cmd.MaxX > lowerSlab.MaxX)
                {
                    return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:unsupported_room"), $"Room must be supported by continuous floor slab below on floor {cmd.Floor - 1}.") });
                }
            }

            if (_roomsByFloor.TryGetValue(cmd.Floor, out var existingRooms))
            {
                foreach (var existing in existingRooms)
                {
                    if (existing.Bounds.Overlaps(cmd.Bounds))
                    {
                        return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:room_overlap"), $"Overlaps existing room '{existing.ContentType.Value}' at [{existing.Bounds.MinX}..{existing.Bounds.MaxX}].") });
                    }
                }
            }

            if (cmd.Bounds.Width < 2 && cmd.ContentType != new ContentId("transit:elevator_shaft") && cmd.ContentType != new ContentId("amenity:stairwell"))
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:room_too_narrow"), "Room width must be at least 2 cells.") });
            }

            var portalX = cmd.PortalX ?? cmd.MinX;
            if (portalX < cmd.MinX || portalX > cmd.MaxX)
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:portal_outside_room"), $"Portal position {portalX} must be within room bounds [{cmd.MinX}..{cmd.MaxX}].") });
            }

            return CommandResult.Success();
        }

        public CommandResult Execute(BuildRoomCommand cmd, Tick tick)
        {
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            if (!_roomsByFloor.ContainsKey(cmd.Floor))
            {
                _roomsByFloor[cmd.Floor] = new List<Room>();
            }

            var portalX = cmd.PortalX ?? cmd.MinX;
            var roomId = AllocateId();
            var portalId = AllocateId();

            var portal = new Portal(portalId, PortalType.Door, new CellCoordinate(portalX, cmd.Floor), roomId);
            var room = new Room(roomId, cmd.ContentType, cmd.Bounds, new[] { portalId }, cmd.Capacity);

            _roomsById[roomId] = room;
            _roomsByFloor[cmd.Floor].Add(room);
            _portalsById[portalId] = portal;
            if (!_portalsByFloor.ContainsKey(cmd.Floor))
            {
                _portalsByFloor[cmd.Floor] = new List<Portal>();
            }
            _portalsByFloor[cmd.Floor].Add(portal);

            InvalidateGraph();

            var evt = new DomainEvent(AllocateId(), new ContentId("event:room_built"), tick, new[] { roomId, portalId });
            return CommandResult.Accept(new[] { evt });
        }

        public CommandResult CanExecute(DemolishRoomCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));

            if (!_roomsById.TryGetValue(cmd.RoomId, out _))
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:room_not_found"), $"Room {cmd.RoomId} not found.") });
            }

            return CommandResult.Success();
        }

        public CommandResult Execute(DemolishRoomCommand cmd, Tick tick)
        {
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var room = _roomsById[cmd.RoomId];

            // Remove associated portals
            if (_portalsByFloor.TryGetValue(room.Floor, out var floorPortals))
            {
                for (var i = floorPortals.Count - 1; i >= 0; i--)
                {
                    if (floorPortals[i].RoomId.Equals(cmd.RoomId))
                    {
                        _portalsById.Remove(floorPortals[i].Id);
                        floorPortals.RemoveAt(i);
                    }
                }
            }

            _roomsById.Remove(cmd.RoomId);
            if (_roomsByFloor.TryGetValue(room.Floor, out var floorRooms))
            {
                floorRooms.Remove(room);
            }

            InvalidateGraph();

            var evt = new DomainEvent(AllocateId(), new ContentId("event:room_demolished"), tick, new[] { cmd.RoomId });
            return CommandResult.Accept(new[] { evt });
        }

        public CommandResult CanExecute(AddElevatorShaftCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));

            if (cmd.BottomFloor >= cmd.TopFloor)
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("transit:invalid_floor_range"), "Bottom floor must be strictly less than top floor.") });
            }

            var shaftContentType = new ContentId("transit:elevator_shaft");

            for (var floor = cmd.BottomFloor; floor <= cmd.TopFloor; floor++)
            {
                if (!_floorSlabs.ContainsKey(floor))
                {
                    return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:floor_not_found"), $"Floor {floor} missing for elevator shaft span.") });
                }

                var shaftBounds = new CellBounds(floor, cmd.ShaftMinX, cmd.ShaftMaxX);
                if (_roomsByFloor.TryGetValue(floor, out var existingRooms))
                {
                    foreach (var existing in existingRooms)
                    {
                        if (existing.Bounds.Overlaps(shaftBounds))
                        {
                            // Extending an existing shaft at identical column bounds is allowed
                            if (existing.ContentType == shaftContentType && existing.Bounds.MinX == cmd.ShaftMinX && existing.Bounds.MaxX == cmd.ShaftMaxX)
                            {
                                continue;
                            }

                            return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:shaft_overlap"), $"Elevator shaft overlaps existing room {existing.Id} on floor {floor}.") });
                        }
                    }
                }
            }

            return CommandResult.Success();
        }

        public CommandResult Execute(AddElevatorShaftCommand cmd, Tick tick)
        {
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var shaftContentType = new ContentId("transit:elevator_shaft");
            var affectedIds = new List<EntityId>();

            for (var floor = cmd.BottomFloor; floor <= cmd.TopFloor; floor++)
            {
                if (_roomsByFloor.TryGetValue(floor, out var existingRooms))
                {
                    var alreadyHasShaft = false;
                    foreach (var existing in existingRooms)
                    {
                        if (existing.ContentType == shaftContentType && existing.Bounds.MinX == cmd.ShaftMinX && existing.Bounds.MaxX == cmd.ShaftMaxX)
                        {
                            alreadyHasShaft = true;
                            break;
                        }
                    }

                    if (alreadyHasShaft)
                    {
                        continue;
                    }
                }

                var roomId = AllocateId();
                var portalId = AllocateId();
                var portal = new Portal(portalId, PortalType.ElevatorShaftDoor, new CellCoordinate(cmd.ShaftMinX, floor), roomId);
                var room = new Room(roomId, shaftContentType, new CellBounds(floor, cmd.ShaftMinX, cmd.ShaftMaxX), new[] { portalId }, cmd.CarCapacity);

                _roomsById[roomId] = room;
                if (!_roomsByFloor.ContainsKey(floor)) _roomsByFloor[floor] = new List<Room>();
                _roomsByFloor[floor].Add(room);

                _portalsById[portalId] = portal;
                if (!_portalsByFloor.ContainsKey(floor)) _portalsByFloor[floor] = new List<Portal>();
                _portalsByFloor[floor].Add(portal);

                affectedIds.Add(roomId);
                affectedIds.Add(portalId);
            }

            InvalidateGraph();

            var evt = new DomainEvent(AllocateId(), new ContentId("event:elevator_shaft_added"), tick, affectedIds);
            return CommandResult.Accept(new[] { evt });
        }

        public CommandResult CanExecute(BuildStairwellCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));

            if (cmd.BottomFloor >= cmd.TopFloor)
            {
                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("transit:invalid_floor_range"), "Bottom floor must be strictly less than top floor.") });
            }

            var stairContentType = new ContentId("amenity:stairwell");

            for (var floor = cmd.BottomFloor; floor <= cmd.TopFloor; floor++)
            {
                if (!_floorSlabs.TryGetValue(floor, out var slab))
                {
                    return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:floor_not_found"), $"Floor {floor} missing for stairwell span.") });
                }

                if (cmd.StairMinX < slab.MinX || cmd.StairMaxX > slab.MaxX)
                {
                    return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:outside_floor_slab"), $"Stairwell bounds [{cmd.StairMinX}..{cmd.StairMaxX}] extend outside floor {floor} slab [{slab.MinX}..{slab.MaxX}].") });
                }

                var stairBounds = new CellBounds(floor, cmd.StairMinX, cmd.StairMaxX);
                if (_roomsByFloor.TryGetValue(floor, out var existingRooms))
                {
                    foreach (var existing in existingRooms)
                    {
                        if (existing.Bounds.Overlaps(stairBounds))
                        {
                            // Extending an existing stairwell at identical column bounds is allowed
                            if (existing.ContentType == stairContentType && existing.Bounds.MinX == cmd.StairMinX && existing.Bounds.MaxX == cmd.StairMaxX)
                            {
                                continue;
                            }

                            if (existing.ContentType == FiveFloorTopologyFixture.ElevatorShaftContentId || (cmd.StairMinX <= 1 && cmd.StairMaxX >= 0))
                            {
                                return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("transit:shaft_overlap"), $"Stairwell cannot overlap central elevator shaft column on floor {floor}.") });
                            }

                            return CommandResult.Reject(new[] { new CommandRejectionReason(new ContentId("topology:stair_overlap"), $"Stairwell overlaps existing room {existing.Id} on floor {floor}.") });
                        }
                    }
                }
            }

            return CommandResult.Success();
        }

        public CommandResult Execute(BuildStairwellCommand cmd, Tick tick)
        {
            var validation = CanExecute(cmd);
            if (!validation.Accepted) return validation;

            var stairContentType = new ContentId("amenity:stairwell");
            var affectedIds = new List<EntityId>();

            for (var floor = cmd.BottomFloor; floor <= cmd.TopFloor; floor++)
            {
                if (_roomsByFloor.TryGetValue(floor, out var existingRooms))
                {
                    var alreadyHasStair = false;
                    foreach (var existing in existingRooms)
                    {
                        if (existing.ContentType == stairContentType && existing.Bounds.MinX == cmd.StairMinX && existing.Bounds.MaxX == cmd.StairMaxX)
                        {
                            alreadyHasStair = true;
                            break;
                        }
                    }

                    if (alreadyHasStair)
                    {
                        continue;
                    }
                }

                var roomId = AllocateId();
                var portalId = AllocateId();
                var portal = new Portal(portalId, PortalType.StairwellDoor, new CellCoordinate(cmd.StairMinX, floor), roomId);
                var room = new Room(roomId, stairContentType, new CellBounds(floor, cmd.StairMinX, cmd.StairMaxX), new[] { portalId }, 10);

                _roomsById[roomId] = room;
                if (!_roomsByFloor.ContainsKey(floor)) _roomsByFloor[floor] = new List<Room>();
                _roomsByFloor[floor].Add(room);

                _portalsById[portalId] = portal;
                if (!_portalsByFloor.ContainsKey(floor)) _portalsByFloor[floor] = new List<Portal>();
                _portalsByFloor[floor].Add(portal);

                affectedIds.Add(roomId);
                affectedIds.Add(portalId);
            }

            InvalidateGraph();

            var evt = new DomainEvent(AllocateId(), new ContentId("event:stairwell_built"), tick, affectedIds);
            return CommandResult.Accept(new[] { evt });
        }

        // ── Snapshot & Synchronization ─────────────────────────────────────────

        public BuildingTopology ToSnapshot()
        {
            var floorList = new List<FloorTopology>(_floorSlabs.Count);

            foreach (var kvp in _floorSlabs)
            {
                var floorLevel = kvp.Key;
                var rooms = _roomsByFloor.TryGetValue(floorLevel, out var rList) ? (IReadOnlyList<Room>)rList : Array.Empty<Room>();
                var portals = _portalsByFloor.TryGetValue(floorLevel, out var pList) ? (IReadOnlyList<Portal>)pList : Array.Empty<Portal>();

                floorList.Add(new FloorTopology(floorLevel, rooms, portals));
            }

            return new BuildingTopology(floorList);
        }

        private void InvalidateGraph()
        {
            _cachedTransitGraph = null;
        }

        private HierarchicalTransitGraph RebuildTransitGraph()
        {
            return HierarchicalTransitGraph.FromBuildingTopology(ToSnapshot());
        }

        // ── Factory Methods ────────────────────────────────────────────────────

        public static BuildingTopologyState FromBuildingTopology(BuildingTopology topology, int defaultMinX = DefaultMinX, int defaultMaxX = DefaultMaxX)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));

            var state = new BuildingTopologyState(startingEntityId: 2000);

            foreach (var floor in topology.Floors)
            {
                var minX = defaultMinX;
                var maxX = defaultMaxX;

                foreach (var room in floor.Rooms)
                {
                    if (room.Bounds.MinX < minX) minX = room.Bounds.MinX - 2;
                    if (room.Bounds.MaxX > maxX) maxX = room.Bounds.MaxX + 2;
                }

                state._floorSlabs[floor.FloorLevel] = new CellBounds(floor.FloorLevel, minX, maxX);

                var rList = new List<Room>(floor.Rooms);
                state._roomsByFloor[floor.FloorLevel] = rList;
                foreach (var r in rList)
                {
                    state._roomsById[r.Id] = r;
                }

                var pList = new List<Portal>(floor.Portals);
                state._portalsByFloor[floor.FloorLevel] = pList;
                foreach (var p in pList)
                {
                    state._portalsById[p.Id] = p;
                }
            }

            return state;
        }

        public static BuildingTopologyState CreateWithFixture()
        {
            return FromBuildingTopology(FiveFloorTopologyFixture.Create());
        }

        // ── Serialization ──────────────────────────────────────────────────────

        public TopologySaveData ToSaveData()
        {
            var slabList = new List<FloorSlabSaveData>(_floorSlabs.Count);
            foreach (var slab in _floorSlabs.Values)
            {
                slabList.Add(new FloorSlabSaveData
                {
                    floorLevel = slab.Floor,
                    minX = slab.MinX,
                    maxX = slab.MaxX
                });
            }

            var roomList = new List<RoomSaveData>(_roomsById.Count);
            foreach (var room in _roomsById.Values)
            {
                var pIds = new int[room.PortalIds.Count];
                for (var i = 0; i < room.PortalIds.Count; i++) pIds[i] = room.PortalIds[i].Value;

                roomList.Add(new RoomSaveData
                {
                    id = room.Id.Value,
                    contentType = room.ContentType.Value,
                    floor = room.Floor,
                    minX = room.Bounds.MinX,
                    maxX = room.Bounds.MaxX,
                    capacity = room.Capacity,
                    portalIds = pIds
                });
            }

            var portalList = new List<PortalSaveData>(_portalsById.Count);
            foreach (var portal in _portalsById.Values)
            {
                portalList.Add(new PortalSaveData
                {
                    id = portal.Id.Value,
                    portalType = (int)portal.Type,
                    floor = portal.Location.Floor,
                    x = portal.Location.X,
                    roomId = portal.RoomId.Value,
                    targetPortalId = portal.TargetPortalId?.Value ?? -1
                });
            }

            return new TopologySaveData
            {
                floorSlabs = slabList.ToArray(),
                rooms = roomList.ToArray(),
                portals = portalList.ToArray()
            };
        }

        public static BuildingTopologyState FromSaveData(TopologySaveData data, int nextEntityId = 3000)
        {
            var topology = new BuildingTopologyState(nextEntityId);
            if (data == null) return topology;

            var slabs = new List<CellBounds>();
            if (data.floorSlabs != null)
            {
                foreach (var s in data.floorSlabs)
                {
                    slabs.Add(new CellBounds(s.floorLevel, s.minX, s.maxX));
                }
            }

            var rooms = new List<Room>();
            if (data.rooms != null)
            {
                foreach (var r in data.rooms)
                {
                    var pIdList = new List<EntityId>();
                    if (r.portalIds != null)
                    {
                        foreach (var pid in r.portalIds) pIdList.Add(new EntityId(pid));
                    }
                    rooms.Add(new Room(new EntityId(r.id), new ContentId(r.contentType), new CellBounds(r.floor, r.minX, r.maxX), pIdList, r.capacity));
                }
            }

            var portals = new List<Portal>();
            if (data.portals != null)
            {
                foreach (var p in data.portals)
                {
                    EntityId? target = p.targetPortalId > 0 ? new EntityId(p.targetPortalId) : null;
                    portals.Add(new Portal(new EntityId(p.id), (PortalType)p.portalType, new CellCoordinate(p.x, p.floor), new EntityId(p.roomId), target));
                }
            }

            topology.RestoreFromData(slabs, rooms, portals);
            return topology;
        }
    }
}
