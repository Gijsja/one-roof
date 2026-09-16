using System;

namespace OneRoof.Application.Population
{
    /// <summary>
    /// Represents the vertical camera / viewport range of visible floors.
    /// Used by visibility and culling policies to prioritize active views.
    /// </summary>
    public readonly struct VisibleFloorRange : IEquatable<VisibleFloorRange>
    {
        public VisibleFloorRange(int minFloor, int maxFloor)
        {
            if (minFloor > maxFloor)
            {
                throw new ArgumentException($"minFloor ({minFloor}) must not exceed maxFloor ({maxFloor}).");
            }

            MinFloor = minFloor;
            MaxFloor = maxFloor;
        }

        public int MinFloor { get; }

        public int MaxFloor { get; }

        public bool Contains(int floor) => floor >= MinFloor && floor <= MaxFloor;

        public static VisibleFloorRange All(int maxFloor = 4) => new VisibleFloorRange(0, maxFloor);

        public static VisibleFloorRange SingleFloor(int floor) => new VisibleFloorRange(floor, floor);

        public bool Equals(VisibleFloorRange other) => MinFloor == other.MinFloor && MaxFloor == other.MaxFloor;

        public override bool Equals(object obj) => obj is VisibleFloorRange other && Equals(other);

        public override int GetHashCode() => unchecked((MinFloor * 397) ^ MaxFloor);

        public static bool operator ==(VisibleFloorRange left, VisibleFloorRange right) => left.Equals(right);

        public static bool operator !=(VisibleFloorRange left, VisibleFloorRange right) => !left.Equals(right);

        public override string ToString() => $"VisibleFloors [{MinFloor}..{MaxFloor}]";
    }
}
