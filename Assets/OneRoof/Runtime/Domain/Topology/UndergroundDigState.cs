using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Persistence;

namespace OneRoof.Domain.Topology
{
    /// <summary>Persistent excavated cells beneath the ground slab.</summary>
    public sealed class UndergroundDigState
    {
        public const int GridWidthCells = 16;
        public const int MaxDepthCells = 6;
        public const int MaxBrushSize = 3;

        private readonly HashSet<UndergroundCell> _excavated = new HashSet<UndergroundCell>();
        private readonly List<UndergroundCell> _ordered = new List<UndergroundCell>();
        private readonly ReadOnlyCollection<UndergroundCell> _readOnlyOrdered;
        private readonly HashSet<UndergroundCell> _floored = new HashSet<UndergroundCell>();
        private readonly List<UndergroundCell> _orderedFloored = new List<UndergroundCell>();
        private readonly ReadOnlyCollection<UndergroundCell> _readOnlyFloored;

        public UndergroundDigState()
        {
            _readOnlyOrdered = _ordered.AsReadOnly();
            _readOnlyFloored = _orderedFloored.AsReadOnly();
        }

        public int Revision { get; private set; }
        public IReadOnlyList<UndergroundCell> ExcavatedCells => _readOnlyOrdered;
        public IReadOnlyList<UndergroundCell> FlooredCells => _readOnlyFloored;

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

        public static UndergroundDigState FromSaveData(UndergroundCellSaveData[] data, UndergroundCellSaveData[] floors = null, bool legacyTowerGrid = false, CellBounds legacyGround = default)
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
                    var key = new UndergroundCell(x, depth);
                    if (x >= 0 && x < GridWidthCells && depth >= 0 && depth < MaxDepthCells && state._excavated.Contains(key))
                        state._floored.Add(key);
                }
            }
            state._ordered.AddRange(state._excavated);
            state._ordered.Sort();
            state._orderedFloored.AddRange(state._floored);
            state._orderedFloored.Sort();
            state.Revision = state._ordered.Count + state._orderedFloored.Count;
            return state;
        }
    }
}
