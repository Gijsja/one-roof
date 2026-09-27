using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Persistence;

namespace OneRoof.Domain.Topology
{
    /// <summary>Persistent excavated cells beneath the ground slab.</summary>
    public sealed class UndergroundDigState
    {
        public const int GridWidthCells = 32;
        public const int MaxDepthCells = 12;
        public const int MaxBrushSize = 3;

        private readonly HashSet<UndergroundCell> _excavated = new HashSet<UndergroundCell>();
        private readonly List<UndergroundCell> _ordered = new List<UndergroundCell>();
        private readonly ReadOnlyCollection<UndergroundCell> _readOnlyOrdered;
        private readonly HashSet<UndergroundCell> _floored = new HashSet<UndergroundCell>();
        private readonly List<UndergroundCell> _orderedFloored = new List<UndergroundCell>();
        private readonly ReadOnlyCollection<UndergroundCell> _readOnlyFloored;
        private readonly HashSet<UndergroundCell> _corridors = new HashSet<UndergroundCell>();
        private readonly HashSet<UndergroundCell> _shaft = new HashSet<UndergroundCell>();
        private readonly List<UndergroundCell> _orderedCorridors = new List<UndergroundCell>();
        private readonly List<UndergroundCell> _orderedShaft = new List<UndergroundCell>();
        private readonly List<UndergroundRoom> _rooms = new List<UndergroundRoom>();
        private readonly ReadOnlyCollection<UndergroundRoom> _readOnlyRooms;
        private UndergroundCell? _accessCore;

        public UndergroundDigState()
        {
            _readOnlyOrdered = _ordered.AsReadOnly();
            _readOnlyFloored = _orderedFloored.AsReadOnly();
            _readOnlyRooms = _rooms.AsReadOnly();
        }

        public int Revision { get; private set; }
        public IReadOnlyList<UndergroundCell> ExcavatedCells => _readOnlyOrdered;
        public IReadOnlyList<UndergroundCell> FlooredCells => _readOnlyFloored;
        public IReadOnlyList<UndergroundCell> Corridors => _orderedCorridors.AsReadOnly();
        public IReadOnlyList<UndergroundCell> ServiceShaftCells => _orderedShaft.AsReadOnly();
        public IReadOnlyList<UndergroundRoom> Rooms => _readOnlyRooms;
        public UndergroundCell? AccessCore => _accessCore;
        public bool IsFloored(int x, int depth) => _floored.Contains(new UndergroundCell(x, depth));
        public bool IsCorridor(int x, int depth) => _corridors.Contains(new UndergroundCell(x, depth));
        public bool IsShaft(int x, int depth) => _shaft.Contains(new UndergroundCell(x, depth));

        public bool IsExcavated(int x, int depth) => _excavated.Contains(new UndergroundCell(x, depth));

        public bool CanExcavate(int x, int depth, int size)
        {
            return size >= 1 && size <= MaxBrushSize && depth >= 0 &&
                   (long)depth + size <= MaxDepthCells && x >= 0 &&
                   (long)x + size <= GridWidthCells;
        }

        public bool CanBuildFloor(int x, int depth, int size)
        {
            if (size < 1 || size > MaxBrushSize || x < 0 || depth < 0 ||
                (long)x + size > GridWidthCells || (long)depth + size > MaxDepthCells) return false;
            for (var dy = 0; dy < size; dy++)
            for (var dx = 0; dx < size; dx++)
            {
                var cell = new UndergroundCell(x + dx, depth + dy);
                if (!_excavated.Contains(cell) || _floored.Contains(cell)) return false;
            }
            return true;
        }

        internal bool BuildFloor(int x, int depth, int size)
        {
            var changed = false;
            for (var dy = 0; dy < size; dy++)
            for (var dx = 0; dx < size; dx++)
                changed |= _floored.Add(new UndergroundCell(x + dx, depth + dy));
            if (!changed) return false;
            _orderedFloored.Clear();
            _orderedFloored.AddRange(_floored);
            _orderedFloored.Sort();
            Revision++;
            return true;
        }

        internal bool Excavate(int x, int depth, int size)
        {
            var changed = false;
            for (var dy = 0; dy < size; dy++)
            for (var dx = 0; dx < size; dx++)
                changed |= _excavated.Add(new UndergroundCell(x + dx, depth + dy));
            if (changed)
            {
                _ordered.Clear();
                _ordered.AddRange(_excavated);
                _ordered.Sort();
                Revision++;
            }
            return changed;
        }

        public bool CanBuildCore(int x, int depth)
        {
            if (x != GridWidthCells / 2 || depth < 0 || depth >= MaxDepthCells || IsShaft(x, depth)) return false;
            for (var d = 0; d <= depth; d++)
                if (!IsFloored(x, d) || IsRoomCell(x, d)) return false;
            return true;
        }

        internal void BuildCore(int x, int depth)
        {
            _accessCore = new UndergroundCell(x, 0);
            for (var d = 0; d <= depth; d++) _shaft.Add(new UndergroundCell(x, d));
            Reorder(_shaft, _orderedShaft);
            RefreshReachability();
            Revision++;
        }

        public bool CanBuildCorridor(int x, int depth, int width)
        {
            if (!_accessCore.HasValue || width < 1 || width > GridWidthCells || x < 0 ||
                depth < 0 || depth >= MaxDepthCells || (long)x + width > GridWidthCells) return false;
            var attached = false;
            var newCell = false;
            var reached = CollectReachableNetwork();
            for (var dx = 0; dx < width; dx++)
            {
                var cx = x + dx;
                if (!IsFloored(cx, depth) || IsRoomCell(cx, depth) || IsShaft(cx, depth)) return false;
                if (!IsCorridor(cx, depth)) newCell = true;
                if (AdjacentToNetwork(cx, depth, reached)) attached = true;
            }
            return attached && newCell;
        }

        internal void BuildCorridor(int x, int depth, int width)
        {
            var changed = false;
            for (var dx = 0; dx < width; dx++) changed |= _corridors.Add(new UndergroundCell(x + dx, depth));
            if (changed) { Reorder(_corridors, _orderedCorridors); RefreshReachability(); Revision++; }
        }

        public bool CanZoneRoom(UndergroundRoomType type, int x, int depth, int width, int height)
        {
            if (!UndergroundRoomCatalog.IsDefined(type) || !_accessCore.HasValue ||
                width < 2 || height < 1 || x < 0 || depth < 0 ||
                (long)x + width > GridWidthCells || (long)depth + height > MaxDepthCells) return false;
            var entrance = false;
            var reached = CollectReachableNetwork();
            for (var dy = 0; dy < height; dy++)
            for (var dx = 0; dx < width; dx++)
            {
                var cx = x + dx;
                var cd = depth + dy;
                if (!IsFloored(cx, cd) || IsRoomCell(cx, cd) || IsCorridor(cx, cd) || IsShaft(cx, cd)) return false;
                if ((dx == 0 && IsCorridor(cx - 1, cd) && reached.Contains(new UndergroundCell(cx - 1, cd))) ||
                    (dx == width - 1 && IsCorridor(cx + 1, cd) && reached.Contains(new UndergroundCell(cx + 1, cd))) ||
                    (dy == 0 && IsCorridor(cx, cd - 1) && reached.Contains(new UndergroundCell(cx, cd - 1))) ||
                    (dy == height - 1 && IsCorridor(cx, cd + 1) && reached.Contains(new UndergroundCell(cx, cd + 1)))) entrance = true;
            }
            return entrance;
        }

        internal UndergroundRoom ZoneRoom(int id, UndergroundRoomType type, int x, int depth, int width, int height)
        {
            var room = new UndergroundRoom(id, type, x, depth, width, height, UndergroundRoomCatalog.Get(type));
            _rooms.Add(room);
            RefreshReachability();
            Revision++;
            return room;
        }

        public bool IsRoomReachable(int roomId)
        {
            for (var i = 0; i < _rooms.Count; i++) if (_rooms[i].Id == roomId) return _rooms[i].IsReachable;
            return false;
        }

        private bool IsRoomCell(int x, int depth)
        {
            for (var i = 0; i < _rooms.Count; i++)
            {
                var r = _rooms[i];
                if (x >= r.X && x < r.X + r.Width && depth >= r.Depth && depth < r.Depth + r.Height) return true;
            }
            return false;
        }

        private static bool AdjacentToNetwork(int x, int depth, HashSet<UndergroundCell> reached)
        {
            return reached.Contains(new UndergroundCell(x - 1, depth)) || reached.Contains(new UndergroundCell(x + 1, depth)) ||
                   reached.Contains(new UndergroundCell(x, depth - 1)) || reached.Contains(new UndergroundCell(x, depth + 1));
        }

        private HashSet<UndergroundCell> CollectReachableNetwork()
        {
            var visited = new HashSet<UndergroundCell>();
            if (!_accessCore.HasValue) return visited;
            visited.Add(_accessCore.Value);
            var queue = new Queue<UndergroundCell>();
            queue.Enqueue(_accessCore.Value);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                Enqueue(c.X - 1, c.Depth); Enqueue(c.X + 1, c.Depth);
                Enqueue(c.X, c.Depth - 1); Enqueue(c.X, c.Depth + 1);
            }
            return visited;
            void Enqueue(int cx, int cd)
            {
                var next = new UndergroundCell(cx, cd);
                if ((_corridors.Contains(next) || _shaft.Contains(next)) && visited.Add(next)) queue.Enqueue(next);
            }
        }

        private static void Reorder(HashSet<UndergroundCell> source, List<UndergroundCell> target)
        {
            target.Clear(); target.AddRange(source); target.Sort();
        }

        public UndergroundCellSaveData[] ToSaveData()
        {
            var data = new UndergroundCellSaveData[_ordered.Count];
            for (var i = 0; i < _ordered.Count; i++)
                data[i] = new UndergroundCellSaveData { x = _ordered[i].X, depth = _ordered[i].Depth };
            return data;
        }

        public UndergroundCellSaveData[] FloorsToSaveData()
        {
            var data = new UndergroundCellSaveData[_orderedFloored.Count];
            for (var i = 0; i < _orderedFloored.Count; i++)
                data[i] = new UndergroundCellSaveData { x = _orderedFloored[i].X, depth = _orderedFloored[i].Depth };
            return data;
        }

        public UndergroundCellSaveData[] CorridorsToSaveData() => CellsToSaveData(_orderedCorridors);
        public UndergroundCellSaveData[] ShaftToSaveData() => CellsToSaveData(_orderedShaft);

        public UndergroundRoomSaveData[] RoomsToSaveData()
        {
            var data = new UndergroundRoomSaveData[_rooms.Count];
            for (var i = 0; i < data.Length; i++)
            {
                var r = _rooms[i];
                data[i] = new UndergroundRoomSaveData { id = r.Id, type = (int)r.Type, x = r.X, depth = r.Depth, width = r.Width, height = r.Height };
            }
            return data;
        }

        private static UndergroundCellSaveData[] CellsToSaveData(List<UndergroundCell> cells)
        {
            var data = new UndergroundCellSaveData[cells.Count];
            for (var i = 0; i < data.Length; i++) data[i] = new UndergroundCellSaveData { x = cells[i].X, depth = cells[i].Depth };
            return data;
        }

        public static UndergroundDigState FromSaveData(UndergroundCellSaveData[] data, UndergroundCellSaveData[] floors = null, bool legacyTowerGrid = false, CellBounds legacyGround = default,
            bool legacy16Grid = false, UndergroundCellSaveData[] corridors = null, UndergroundCellSaveData[] shaft = null,
            UndergroundCellSaveData core = null, UndergroundRoomSaveData[] rooms = null)
        {
            var state = new UndergroundDigState();
            if (data == null) return state;
            for (var i = 0; i < data.Length; i++)
            {
                var cell = data[i];
                if (cell == null) continue;
                var x = cell.x;
                var depth = cell.depth;
                if (legacyTowerGrid)
                {
                    // Earlier prototypes stored half-meter tower cells relative to
                    // the slab; migrate their centers into the independent 1 m board.
                    var groundLeft = legacyGround.Width > 0 ? legacyGround.MinX : -14;
                    x = (int)Math.Floor((cell.x - groundLeft) * 0.5);
                    depth = cell.depth / 2;
                }
                if (legacy16Grid) { x += 8; depth += 3; }
                if (depth < 0 || depth >= MaxDepthCells || x < 0 || x >= GridWidthCells) continue;
                state._excavated.Add(new UndergroundCell(x, depth));
            }
            if (floors != null)
            {
                for (var i = 0; i < floors.Length; i++)
                {
                    var cell = floors[i];
                    if (cell == null) continue;
                    var x = legacyTowerGrid ? (int)Math.Floor((cell.x - (legacyGround.Width > 0 ? legacyGround.MinX : -14)) * 0.5) : cell.x;
                    var depth = legacyTowerGrid ? cell.depth / 2 : cell.depth;
                    if (legacy16Grid) { x += 8; depth += 3; }
                    var key = new UndergroundCell(x, depth);
                    if (x >= 0 && x < GridWidthCells && depth >= 0 && depth < MaxDepthCells && state._excavated.Contains(key))
                        state._floored.Add(key);
                }
            }
            state._ordered.AddRange(state._excavated);
            state._ordered.Sort();
            state._orderedFloored.AddRange(state._floored);
            state._orderedFloored.Sort();
            if (core != null && core.x == GridWidthCells / 2 && core.depth == 0 && state.IsFloored(core.x, 0))
                state._accessCore = new UndergroundCell(core.x, 0);
            if (shaft != null && state._accessCore.HasValue)
                foreach (var c in shaft)
                    if (c != null && c.x == state._accessCore.Value.X && state.IsFloored(c.x, c.depth)) state._shaft.Add(new UndergroundCell(c.x, c.depth));
            if (corridors != null)
                foreach (var c in corridors)
                    if (c != null && state.IsFloored(c.x, c.depth) && !state.IsShaft(c.x, c.depth)) state._corridors.Add(new UndergroundCell(c.x, c.depth));
            Reorder(state._shaft, state._orderedShaft);
            Reorder(state._corridors, state._orderedCorridors);
            if (rooms != null)
                foreach (var saved in rooms)
                    if (saved != null && UndergroundRoomCatalog.IsDefined((UndergroundRoomType)saved.type) &&
                        saved.width >= 2 && saved.height >= 1 && saved.x >= 0 && saved.depth >= 0 &&
                        saved.x + saved.width <= GridWidthCells && saved.depth + saved.height <= MaxDepthCells)
                        state._rooms.Add(new UndergroundRoom(saved.id, (UndergroundRoomType)saved.type, saved.x, saved.depth,
                            saved.width, saved.height, UndergroundRoomCatalog.Get((UndergroundRoomType)saved.type)));
            state.RefreshReachability();
            state.Revision = state._ordered.Count + state._orderedFloored.Count;
            return state;
        }

        private void RefreshReachability()
        {
            var reached = new HashSet<UndergroundCell>();
            var queue = new Queue<UndergroundCell>();
            if (_accessCore.HasValue) { reached.Add(_accessCore.Value); queue.Enqueue(_accessCore.Value); }
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                Visit(cell.X - 1, cell.Depth); Visit(cell.X + 1, cell.Depth);
                Visit(cell.X, cell.Depth - 1); Visit(cell.X, cell.Depth + 1);
            }
            for (var i = 0; i < _rooms.Count; i++)
            {
                var r = _rooms[i];
                r.IsReachable = false;
                for (var d = r.Depth; d < r.Depth + r.Height; d++)
                    if (reached.Contains(new UndergroundCell(r.X - 1, d)) ||
                        reached.Contains(new UndergroundCell(r.X + r.Width, d))) r.IsReachable = true;
                for (var x = r.X; x < r.X + r.Width; x++)
                    if (reached.Contains(new UndergroundCell(x, r.Depth - 1)) ||
                        reached.Contains(new UndergroundCell(x, r.Depth + r.Height))) r.IsReachable = true;
            }
            void Visit(int x, int depth)
            {
                var next = new UndergroundCell(x, depth);
                if ((_corridors.Contains(next) || _shaft.Contains(next)) && reached.Add(next)) queue.Enqueue(next);
            }
        }
    }
}
