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

            UsesSeconds = false;
            FloorTravelSeconds = checked(floorTravelTicks * 60);
            DoorCycleSeconds = checked(doorCycleTicks * 60);
            DwellSeconds = checked(dwellTicks * 60);
            FloorTravelTicks = floorTravelTicks;
            DoorCycleTicks = doorCycleTicks;
            DwellTicks = dwellTicks;
        }

        public int FloorTravelTicks { get; }

        public int DoorCycleTicks { get; }

        public int DwellTicks { get; }

        /// <summary>The tick constructor retains authored minute timings and their phase boundaries.</summary>
        public bool UsesSeconds { get; }
        public int FloorTravelSeconds { get; }
        public int DoorCycleSeconds { get; }
        public int DwellSeconds { get; }

        private ElevatorTimingConfig(int travelSeconds, int doorSeconds, int dwellSeconds, bool usesSeconds)
        {
            if (travelSeconds <= 0 || doorSeconds < 0 || dwellSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(travelSeconds));
            UsesSeconds = usesSeconds;
            FloorTravelSeconds = travelSeconds;
            DoorCycleSeconds = doorSeconds;
            DwellSeconds = dwellSeconds;
            FloorTravelTicks = (travelSeconds + 59) / 60;
            DoorCycleTicks = (doorSeconds + 59) / 60;
            DwellTicks = (dwellSeconds + 59) / 60;
        }

        public static ElevatorTimingConfig FromSeconds(int floorTravelSeconds, int doorCycleSeconds, int dwellSeconds) =>
            new ElevatorTimingConfig(floorTravelSeconds, doorCycleSeconds, dwellSeconds, true);

        public static ElevatorTimingConfig Default => FromSeconds(10, 5, 10);

        public bool Equals(ElevatorTimingConfig other) =>
            UsesSeconds == other.UsesSeconds && FloorTravelSeconds == other.FloorTravelSeconds &&
            DoorCycleSeconds == other.DoorCycleSeconds && DwellSeconds == other.DwellSeconds;

        public override bool Equals(object obj) => obj is ElevatorTimingConfig other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(UsesSeconds, FloorTravelSeconds, DoorCycleSeconds, DwellSeconds);
    }
}
