using System;
using System.Collections.Generic;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Infrastructure
{
    public enum UndergroundUtilityKind { Power, Water }
    public enum UtilityPathPointKind { SurfaceRoom, UndergroundCell, UndergroundRoom }
    public enum UndergroundUtilityCause { None, NoSurfaceSource, NoAccessCore, DisconnectedPath, SurfaceServiceUnavailable }

    /// <summary>Coordinates remain in their authoritative surface or underground grid.</summary>
    public readonly struct UtilityPathPoint
    {
        public UtilityPathPoint(UtilityPathPointKind kind, int x, int depth, int roomId = 0)
        { Kind = kind; X = x; Depth = depth; RoomId = roomId; }

        public UtilityPathPointKind Kind { get; }
        public int X { get; }
        public int Depth { get; }
        public int RoomId { get; }
    }

    public readonly struct UndergroundUtilitySegment
    {
        public UndergroundUtilitySegment(string id, UndergroundUtilityKind kind, UtilityPathPoint from,
            UtilityPathPoint to, bool isConnected, bool isFlowing)
        { Id = id; Kind = kind; From = from; To = to; IsConnected = isConnected; IsFlowing = isFlowing; }

        public string Id { get; }
        public UndergroundUtilityKind Kind { get; }
        public UtilityPathPoint From { get; }
        public UtilityPathPoint To { get; }
        public bool IsConnected { get; }
        public bool IsFlowing { get; }
    }

    public readonly struct UndergroundRoomUtilityStatus
    {
        public UndergroundRoomUtilityStatus(int roomId, UndergroundUtilityKind kind, bool isConnected,
            bool isFlowing, UndergroundUtilityCause cause)
        { RoomId = roomId; Kind = kind; IsConnected = isConnected; IsFlowing = isFlowing; Cause = cause; }

        public int RoomId { get; }
        public UndergroundUtilityKind Kind { get; }
        public bool IsConnected { get; }
        public bool IsFlowing { get; }
        public UndergroundUtilityCause Cause { get; }
    }

    public sealed class UndergroundUtilityPathSnapshot
    {
        public UndergroundUtilityPathSnapshot(IReadOnlyList<UndergroundUtilitySegment> powerSegments,
            IReadOnlyList<UndergroundUtilitySegment> waterSegments,
            IReadOnlyList<UndergroundRoomUtilityStatus> roomStatuses)
        { PowerSegments = powerSegments; WaterSegments = waterSegments; RoomStatuses = roomStatuses; }

        public IReadOnlyList<UndergroundUtilitySegment> PowerSegments { get; }
        public IReadOnlyList<UndergroundUtilitySegment> WaterSegments { get; }
        public IReadOnlyList<UndergroundRoomUtilityStatus> RoomStatuses { get; }
    }

    /// <summary>
    /// Read-only paths through the built lobby core, shaft, corridors, and room entrances.
    /// The access core is the surface/underground utility bridge; no negative tower floors are created.
    /// </summary>
    public static class UndergroundUtilityPathState
    {
        public static UndergroundUtilityPathSnapshot Project(BuildingTopologyState tower, UndergroundDigState underground,
            ElectricalGridSnapshot electrical, WaterWasteNetworkSnapshot water)
        {
            if (tower == null) throw new ArgumentNullException(nameof(tower));
            if (underground == null) throw new ArgumentNullException(nameof(underground));
            if (electrical == null) throw new ArgumentNullException(nameof(electrical));
            if (water == null) throw new ArgumentNullException(nameof(water));

            var distances = CollectDistances(underground);
            var power = Build(UndergroundUtilityKind.Power, tower, underground, distances,
                ElectricalGridState.SubstationContentId.Value, HasGroundPower(electrical));
            var waterPaths = Build(UndergroundUtilityKind.Water, tower, underground, distances,
                WaterWasteNetworkState.WaterPumpContentId.Value, HasGroundWater(water));
            var statuses = new List<UndergroundRoomUtilityStatus>(power.Item2.Count + waterPaths.Item2.Count);
            statuses.AddRange(power.Item2);
            statuses.AddRange(waterPaths.Item2);
            return new UndergroundUtilityPathSnapshot(power.Item1.AsReadOnly(), waterPaths.Item1.AsReadOnly(),
                statuses.AsReadOnly());
        }

        private static (List<UndergroundUtilitySegment>, List<UndergroundRoomUtilityStatus>) Build(
            UndergroundUtilityKind kind, BuildingTopologyState tower, UndergroundDigState underground,
            Dictionary<UndergroundCell, int> distances, string sourceContentId, bool sourceServiceActive)
        {
            var segments = new List<UndergroundUtilitySegment>();
            var statuses = new List<UndergroundRoomUtilityStatus>();
            var prefix = kind == UndergroundUtilityKind.Power ? "power" : "water";
            var sources = new List<Room>();
            foreach (var room in tower.GetRoomsOnFloor(0))
                if (room.ContentType.Value == sourceContentId) sources.Add(room);
            sources.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            var core = underground.AccessCore;
            var hasCorePath = core.HasValue && distances.ContainsKey(core.Value);
            var hasSource = sources.Count > 0;
            var flowing = hasSource && hasCorePath && sourceServiceActive;

            for (var i = 0; i < sources.Count && hasCorePath; i++)
            {
                var source = sources[i];
                segments.Add(new UndergroundUtilitySegment($"{prefix}:source:{source.Id.Value}:core", kind,
                    new UtilityPathPoint(UtilityPathPointKind.SurfaceRoom,
                        (source.Bounds.MinX + source.Bounds.MaxX) / 2, 0, source.Id.Value),
                    CellPoint(core.Value), true, sourceServiceActive));
            }

            var built = new HashSet<UndergroundCell>(underground.ServiceShaftCells);
            built.UnionWith(underground.Corridors);
            var cells = new List<UndergroundCell>(built);
            cells.Sort();
            for (var i = 0; i < cells.Count; i++)
            {
                var from = cells[i];
                AddAdjacent(from, new UndergroundCell(from.X + 1, from.Depth));
                AddAdjacent(from, new UndergroundCell(from.X, from.Depth + 1));
            }

            var rooms = new List<UndergroundRoom>(underground.Rooms);
            rooms.Sort((a, b) => a.Id.CompareTo(b.Id));
            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                var port = FindPort(room, built, distances);
                var pathConnected = room.IsReachable && port.HasValue && distances.ContainsKey(port.Value);
                var connected = hasSource && hasCorePath && pathConnected;
                var roomFlowing = connected && sourceServiceActive;
                var cause = !hasSource ? UndergroundUtilityCause.NoSurfaceSource
                    : !hasCorePath ? UndergroundUtilityCause.NoAccessCore
                    : !pathConnected ? UndergroundUtilityCause.DisconnectedPath
                    : !sourceServiceActive ? UndergroundUtilityCause.SurfaceServiceUnavailable
                    : UndergroundUtilityCause.None;
                statuses.Add(new UndergroundRoomUtilityStatus(room.Id, kind, connected, roomFlowing, cause));
                if (!port.HasValue) continue;
                segments.Add(new UndergroundUtilitySegment($"{prefix}:room:{room.Id}:port:{port.Value.X}:{port.Value.Depth}",
                    kind, CellPoint(port.Value), new UtilityPathPoint(UtilityPathPointKind.UndergroundRoom,
                        room.X + room.Width / 2, room.Depth + room.Height / 2, room.Id),
                    connected, roomFlowing));
            }
            return (segments, statuses);

            void AddAdjacent(UndergroundCell from, UndergroundCell to)
            {
                if (!built.Contains(to)) return;
                var hasFrom = distances.TryGetValue(from, out var fromDistance);
                var hasTo = distances.TryGetValue(to, out var toDistance);
                var connected = hasFrom && hasTo;
                var forward = !connected || fromDistance <= toDistance;
                segments.Add(new UndergroundUtilitySegment(
                    $"{prefix}:cell:{from.X}:{from.Depth}:{to.X}:{to.Depth}", kind,
                    CellPoint(forward ? from : to), CellPoint(forward ? to : from), connected, connected && flowing));
            }
        }

        private static UtilityPathPoint CellPoint(UndergroundCell cell) =>
            new UtilityPathPoint(UtilityPathPointKind.UndergroundCell, cell.X, cell.Depth);

        private static UndergroundCell? FindPort(UndergroundRoom room, HashSet<UndergroundCell> built,
            Dictionary<UndergroundCell, int> distances)
        {
            var candidates = new List<UndergroundCell>();
            for (var depth = room.Depth; depth < room.Depth + room.Height; depth++)
            {
                Add(room.X - 1, depth); Add(room.X + room.Width, depth);
            }
            for (var x = room.X; x < room.X + room.Width; x++)
            {
                Add(x, room.Depth - 1); Add(x, room.Depth + room.Height);
            }
            candidates.Sort();
            for (var i = 0; i < candidates.Count; i++) if (distances.ContainsKey(candidates[i])) return candidates[i];
            return candidates.Count > 0 ? candidates[0] : (UndergroundCell?)null;

            void Add(int x, int depth)
            {
                var cell = new UndergroundCell(x, depth);
                if (built.Contains(cell)) candidates.Add(cell);
            }
        }

        private static Dictionary<UndergroundCell, int> CollectDistances(UndergroundDigState underground)
        {
            var distances = new Dictionary<UndergroundCell, int>();
            var core = underground.AccessCore;
            if (!core.HasValue || !underground.IsShaft(core.Value.X, core.Value.Depth)) return distances;
            var queue = new Queue<UndergroundCell>();
            queue.Enqueue(core.Value);
            distances.Add(core.Value, 0);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var nextDistance = distances[cell] + 1;
                Visit(cell.X - 1, cell.Depth, nextDistance); Visit(cell.X + 1, cell.Depth, nextDistance);
                Visit(cell.X, cell.Depth - 1, nextDistance); Visit(cell.X, cell.Depth + 1, nextDistance);
            }
            return distances;

            void Visit(int x, int depth, int distance)
            {
                var cell = new UndergroundCell(x, depth);
                if ((underground.IsShaft(x, depth) || underground.IsCorridor(x, depth)) &&
                    !distances.ContainsKey(cell))
                {
                    distances.Add(cell, distance);
                    queue.Enqueue(cell);
                }
            }
        }

        private static bool HasGroundPower(ElectricalGridSnapshot electrical)
        {
            for (var i = 0; i < electrical.Floors.Count; i++)
                if (electrical.Floors[i].Floor == 0)
                    return electrical.Floors[i].IsConnected &&
                        electrical.Floors[i].Voltage >= ElectricalGridState.BrownoutVoltageThreshold;
            return false;
        }

        private static bool HasGroundWater(WaterWasteNetworkSnapshot water)
        {
            for (var i = 0; i < water.Floors.Count; i++)
                if (water.Floors[i].Floor == 0) return water.Floors[i].HasWaterService;
            return false;
        }
    }
}
