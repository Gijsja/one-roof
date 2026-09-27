using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Topology;

namespace OneRoof.Application.Tower
{
    /// <summary>Immutable underground cell snapshot for presentation.</summary>
    public sealed class UndergroundDigProjection
    {
        private readonly HashSet<UndergroundCell> _lookup;
        private readonly HashSet<UndergroundCell> _flooredLookup;

        public UndergroundDigProjection(UndergroundDigState state)
        {
            Revision = state.Revision;
            var cells = new List<UndergroundCell>(state.ExcavatedCells);
            ExcavatedCells = new ReadOnlyCollection<UndergroundCell>(cells);
            _lookup = new HashSet<UndergroundCell>(cells);
            var floors = new List<UndergroundCell>(state.FlooredCells);
            FlooredCells = new ReadOnlyCollection<UndergroundCell>(floors);
            _flooredLookup = new HashSet<UndergroundCell>(floors);
        }

        public int Revision { get; }
        public IReadOnlyList<UndergroundCell> ExcavatedCells { get; }
        public IReadOnlyList<UndergroundCell> FlooredCells { get; }
        public bool IsExcavated(int x, int depth)
        {
            return _lookup.Contains(new UndergroundCell(x, depth));
        }

        public bool IsFloored(int x, int depth) => _flooredLookup.Contains(new UndergroundCell(x, depth));
    }
}
