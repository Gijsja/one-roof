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
        public ScheduleBlock(string label, Tick startTick, Tick endTick)
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
        }

        /// <summary>Human-readable label, e.g. "Sleep", "Work", "Eat", "Leisure".</summary>
        public string Label { get; }

        /// <summary>Inclusive start of this block (ticks within the day cycle).</summary>
        public Tick StartTick { get; }

        /// <summary>Exclusive end of this block (ticks within the day cycle).</summary>
        public Tick EndTick { get; }

        /// <summary>Duration of this block in ticks.</summary>
        public long DurationTicks => EndTick.Value - StartTick.Value;

        /// <summary>Returns true when <paramref name="tick"/> falls within [StartTick, EndTick).</summary>
        public bool Contains(Tick tick) => tick.Value >= StartTick.Value && tick.Value < EndTick.Value;

        public bool Equals(ScheduleBlock other) =>
            Label == other.Label &&
            StartTick.Equals(other.StartTick) &&
            EndTick.Equals(other.EndTick);

        public override bool Equals(object obj) => obj is ScheduleBlock other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Label?.GetHashCode() ?? 0;
                hash = (hash * 397) ^ StartTick.GetHashCode();
                hash = (hash * 397) ^ EndTick.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"{Label} [{StartTick.Value}–{EndTick.Value})";
    }
}
