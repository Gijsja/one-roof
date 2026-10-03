using System;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// A named time window within a daily tick cycle.
    /// StartTick is inclusive; EndTick is exclusive.
    /// Both values are modulo the day length — callers are responsible for day-cycle wrapping.
    /// </summary>
    [Serializable]
    public readonly struct ScheduleBlock : IEquatable<ScheduleBlock>
    {
        public ScheduleBlock(string label, Tick startTick, Tick endTick,
            long? episodeStartDayTick = null, long? appointmentStartDayTick = null, long? appointmentEndDayTick = null)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Schedule block label must not be null or empty.", nameof(label));
            }

            if (endTick.Value <= startTick.Value)
            {
                throw new ArgumentException(
                    $"ScheduleBlock '{label}': EndTick ({endTick.Value}) must be strictly greater than StartTick ({startTick.Value}).",
                    nameof(endTick));
            }

            Label = label;
            StartTick = startTick;
            EndTick = endTick;
            EpisodeStartDayTick = episodeStartDayTick ?? startTick.Value;
            AppointmentStartDayTick = appointmentStartDayTick ?? EpisodeStartDayTick;
            AppointmentEndDayTick = appointmentEndDayTick ?? endTick.Value;
            if (episodeStartDayTick.HasValue || appointmentStartDayTick.HasValue || appointmentEndDayTick.HasValue)
            {
                if (startTick.Value < 0 || endTick.Value > DailySchedule.TicksPerDay ||
                    EpisodeStartDayTick < -DailySchedule.TicksPerDay || EpisodeStartDayTick > startTick.Value ||
                    AppointmentStartDayTick < -DailySchedule.TicksPerDay || AppointmentStartDayTick > EpisodeStartDayTick ||
                    AppointmentEndDayTick < endTick.Value || AppointmentEndDayTick > 2 * DailySchedule.TicksPerDay ||
                    AppointmentEndDayTick <= AppointmentStartDayTick ||
                    AppointmentEndDayTick - AppointmentStartDayTick > DailySchedule.TicksPerDay)
                    throw new ArgumentException("Invalid logical schedule boundaries.");
            }
        }

        /// <summary>Human-readable label, e.g. "Sleep", "Work", "Eat", "Leisure".</summary>
        public string Label { get; }

        /// <summary>Inclusive start of this block (ticks within the day cycle).</summary>
        public Tick StartTick { get; }

        /// <summary>Exclusive end of this block (ticks within the day cycle).</summary>
        public Tick EndTick { get; }
        /// <summary>Logical boundary, possibly before midnight for a split continuation.</summary>
        public long EpisodeStartDayTick { get; }
        public long AppointmentStartDayTick { get; }
        public long AppointmentEndDayTick { get; }

        /// <summary>Duration of this block in ticks.</summary>
        public long DurationTicks => EndTick.Value - StartTick.Value;

        /// <summary>Returns true when <paramref name="tick"/> falls within [StartTick, EndTick).</summary>
        public bool Contains(Tick tick) => tick.Value >= StartTick.Value && tick.Value < EndTick.Value;

        public bool Equals(ScheduleBlock other) =>
            Label == other.Label &&
            StartTick.Equals(other.StartTick) &&
            EndTick.Equals(other.EndTick) && EpisodeStartDayTick == other.EpisodeStartDayTick &&
            AppointmentStartDayTick == other.AppointmentStartDayTick && AppointmentEndDayTick == other.AppointmentEndDayTick;

        public override bool Equals(object obj) => obj is ScheduleBlock other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Label?.GetHashCode() ?? 0;
                hash = (hash * 397) ^ StartTick.GetHashCode();
                hash = (hash * 397) ^ EndTick.GetHashCode();
                hash = (hash * 397) ^ EpisodeStartDayTick.GetHashCode();
                hash = (hash * 397) ^ AppointmentStartDayTick.GetHashCode();
                hash = (hash * 397) ^ AppointmentEndDayTick.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"{Label} [{StartTick.Value}–{EndTick.Value})";
    }
}
