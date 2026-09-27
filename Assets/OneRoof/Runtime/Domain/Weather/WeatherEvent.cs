using System;

namespace OneRoof.Domain.Weather
{
    /// <summary>
    /// Discrete weather front spanning a defined simulation tick interval.
    /// </summary>
    public readonly struct WeatherEvent : IEquatable<WeatherEvent>
    {
        public WeatherEvent(WeatherCondition condition, long startTick, long endTick, float peakIntensity, float windSpeed, string description = null)
        {
            if (endTick < startTick)
            {
                throw new ArgumentOutOfRangeException(nameof(endTick), "End tick cannot precede start tick.");
            }

            Condition = condition;
            StartTick = startTick;
            EndTick = endTick;
            PeakIntensity = Math.Max(0f, Math.Min(1f, peakIntensity));
            WindSpeed = windSpeed;
            Description = description ?? WeatherSample.DefaultDescription(condition);
        }

        public WeatherCondition Condition { get; }
        public long StartTick { get; }
        public long EndTick { get; }
        public long DurationTicks => EndTick - StartTick;
        public float PeakIntensity { get; }
        public float WindSpeed { get; }
        public string Description { get; }

        public bool Contains(long tick) => tick >= StartTick && tick < EndTick;

        public bool Equals(WeatherEvent other) =>
            Condition == other.Condition &&
            StartTick == other.StartTick &&
            EndTick == other.EndTick &&
            Math.Abs(PeakIntensity - other.PeakIntensity) < 0.001f &&
            Math.Abs(WindSpeed - other.WindSpeed) < 0.001f &&
            Description == other.Description;

        public override bool Equals(object obj) => obj is WeatherEvent other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Condition;
                hash = hash * 397 ^ (int)(StartTick & 0x7FFFFFFF);
                hash = hash * 397 ^ (int)(EndTick & 0x7FFFFFFF);
                return hash;
            }
        }

        public override string ToString() => $"[{StartTick}..{EndTick}] {Description} (Peak {PeakIntensity:P0})";

        public static bool operator ==(WeatherEvent left, WeatherEvent right) => left.Equals(right);
        public static bool operator !=(WeatherEvent left, WeatherEvent right) => !left.Equals(right);
    }
}
