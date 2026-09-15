using System;

namespace OneRoof.Domain.Transit
{
    public readonly struct ElevatorTimingConfig : IEquatable<ElevatorTimingConfig>
    {
        public ElevatorTimingConfig(int floorTravelTicks, int doorCycleTicks, int dwellTicks)
        {
            if (floorTravelTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(floorTravelTicks), floorTravelTicks, "Floor travel ticks must be positive.");
            }

            if (doorCycleTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(doorCycleTicks), doorCycleTicks, "Door cycle ticks cannot be negative.");
            }

            if (dwellTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dwellTicks), dwellTicks, "Dwell ticks cannot be negative.");
            }

            FloorTravelTicks = floorTravelTicks;
            DoorCycleTicks = doorCycleTicks;
            DwellTicks = dwellTicks;
        }

        public int FloorTravelTicks { get; }

        public int DoorCycleTicks { get; }

        public int DwellTicks { get; }

        public static ElevatorTimingConfig Default => new ElevatorTimingConfig(5, 2, 2);

        public bool Equals(ElevatorTimingConfig other) =>
            FloorTravelTicks == other.FloorTravelTicks &&
            DoorCycleTicks == other.DoorCycleTicks &&
            DwellTicks == other.DwellTicks;

        public override bool Equals(object obj) => obj is ElevatorTimingConfig other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(FloorTravelTicks, DoorCycleTicks, DwellTicks);
    }
}
