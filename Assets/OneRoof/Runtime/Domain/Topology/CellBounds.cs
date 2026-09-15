using System;

namespace OneRoof.Domain.Topology
{
    public readonly struct CellBounds : IEquatable<CellBounds>
    {
        public CellBounds(int floor, int minX, int maxX)
        {
            if (minX > maxX)
            {
                throw new ArgumentException($"minX ({minX}) cannot be greater than maxX ({maxX}).", nameof(minX));
            }

            Floor = floor;
            MinX = minX;
            MaxX = maxX;
        }

        public int Floor { get; }

        public int MinX { get; }

        public int MaxX { get; }

        public int Width => MaxX - MinX + 1;

        public bool Contains(CellCoordinate coordinate)
        {
            return coordinate.Floor == Floor && coordinate.X >= MinX && coordinate.X <= MaxX;
        }

        public bool Overlaps(CellBounds other)
        {
            if (Floor != other.Floor)
            {
                return false;
            }

            return Math.Max(MinX, other.MinX) <= Math.Min(MaxX, other.MaxX);
        }

        public bool Equals(CellBounds other)
        {
            return Floor == other.Floor && MinX == other.MinX && MaxX == other.MaxX;
        }

        public override bool Equals(object obj) => obj is CellBounds other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Floor, MinX, MaxX);

        public override string ToString() => $"Floor {Floor}: [{MinX}..{MaxX}]";

        public static bool operator ==(CellBounds left, CellBounds right) => left.Equals(right);

        public static bool operator !=(CellBounds left, CellBounds right) => !left.Equals(right);
    }
}
