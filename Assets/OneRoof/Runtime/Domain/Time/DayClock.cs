using System;
using OneRoof.Domain.Population;

namespace OneRoof.Domain.Time
{
    /// <summary>
    /// Pure tick → calendar mapping for the day/night clock. Day 1 starts at 00:00,
    /// matching <see cref="DailySchedule"/> (60 ticks per hour, 1440 ticks per day).
    /// Night is 20:00–05:59; presentation owns all visual blending.
    /// </summary>
    public readonly struct DayPhase : IEquatable<DayPhase>
    {
        public DayPhase(int dayNumber, int hour, int minute, bool isNight)
        {
            if (dayNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(dayNumber), dayNumber, "Day numbers start at 1.");
            }

            if (hour < 0 || hour > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(hour), hour, "Hour must be 0–23.");
            }

            if (minute < 0 || minute > 59)
            {
                throw new ArgumentOutOfRangeException(nameof(minute), minute, "Minute must be 0–59.");
            }

            DayNumber = dayNumber;
            Hour = hour;
            Minute = minute;
            IsNight = isNight;
        }

        public int DayNumber { get; }

        public int Hour { get; }

        public int Minute { get; }

        public bool IsNight { get; }

        public string ClockLabel => $"{Hour:D2}:{Minute:D2}";

        public bool Equals(DayPhase other) =>
            DayNumber == other.DayNumber && Hour == other.Hour && Minute == other.Minute && IsNight == other.IsNight;

        public override bool Equals(object obj) => obj is DayPhase other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = DayNumber;
                hash = hash * 31 + Hour;
                hash = hash * 31 + Minute;
                hash = hash * 31 + (IsNight ? 1 : 0);
                return hash;
            }
        }

        public override string ToString() => $"Day {DayNumber} {ClockLabel}";

        public static bool operator ==(DayPhase left, DayPhase right) => left.Equals(right);

        public static bool operator !=(DayPhase left, DayPhase right) => !left.Equals(right);
    }

    /// <summary>Pure factory deriving a <see cref="DayPhase"/> from a simulation tick.</summary>
    public static class DayClock
    {
        public const long TicksPerHour = 60;

        public static DayPhase FromTick(long tick)
        {
            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tick), tick, "Ticks cannot be negative.");
            }

            var dayTick = tick % DailySchedule.TicksPerDay;
            var hour = (int)(dayTick / TicksPerHour);
            var minute = (int)(dayTick % TicksPerHour);
            return new DayPhase((int)(tick / DailySchedule.TicksPerDay) + 1, hour, minute, hour >= 20 || hour < 6);
        }
    }
}
