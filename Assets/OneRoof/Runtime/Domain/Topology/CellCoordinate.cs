using System;

namespace OneRoof.Domain.Topology
{
    public readonly struct CellCoordinate : IEquatable<CellCoordinate>, IComparable<CellCoordinate>
    {
        public CellCoordinate(int x, int floor)
        {
            X = x;
            Floor = floor;
        }

        public int X { get; }

        public int Floor { get; }

        public bool Equals(CellCoordinate other) => X == other.X && Floor == other.Floor;

        public override bool Equals(object obj) => obj is CellCoordinate other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Floor);

        public int CompareTo(CellCoordinate other)
        {
            var floorComparison = Floor.CompareTo(other.Floor);
            return floorComparison != 0 ? floorComparison : X.CompareTo(other.X);
        }

        public override string ToString() => $"({X}, {Floor})";

        public static bool operator ==(CellCoordinate left, CellCoordinate right) => left.Equals(right);

        public static bool operator !=(CellCoordinate left, CellCoordinate right) => !left.Equals(right);
    }
}
