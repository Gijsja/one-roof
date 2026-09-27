using System;

namespace OneRoof.Domain.Topology
{
    /// <summary>One square soil cell, addressed horizontally and by depth below ground.</summary>
    public readonly struct UndergroundCell : IEquatable<UndergroundCell>, IComparable<UndergroundCell>
    {
        public UndergroundCell(int x, int depth)
        {
            X = x;
            Depth = depth;
        }

        public int X { get; }
        public int Depth { get; }

        public bool Equals(UndergroundCell other) => X == other.X && Depth == other.Depth;
        public override bool Equals(object obj) => obj is UndergroundCell other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Depth);
        public int CompareTo(UndergroundCell other)
        {
            var depthComparison = Depth.CompareTo(other.Depth);
            return depthComparison != 0 ? depthComparison : X.CompareTo(other.X);
        }
    }
}
