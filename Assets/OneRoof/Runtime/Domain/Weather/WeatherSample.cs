using System;

namespace OneRoof.Domain.Weather
{
    /// <summary>
    /// Continuous sample of atmospheric weather parameters at a specific simulation instant.
    /// </summary>
    public readonly struct WeatherSample : IEquatable<WeatherSample>
    {
        public WeatherSample(WeatherCondition condition, float intensity, float windSpeed, string description = null)
        {
            Condition = condition;
            Intensity = Math.Max(0f, Math.Min(1f, intensity));
            WindSpeed = windSpeed;
            Description = description ?? DefaultDescription(condition);
        }

        public WeatherCondition Condition { get; }
        public float Intensity { get; }
        public float WindSpeed { get; }
        public string Description { get; }
        public bool IsPrecipitating => Condition != WeatherCondition.Clear && Intensity > 0.01f;

        /// <summary>Returns true when this weather state significantly reduces visibility.</summary>
        public bool IsReducedVisibility =>
            Condition == WeatherCondition.Fog ||
            Condition == WeatherCondition.Snow ||
            (Condition == WeatherCondition.Storm && Intensity > 0.7f);

        public static string DefaultDescription(WeatherCondition condition) => condition switch
        {
            WeatherCondition.Clear   => "Clear Skies",
            WeatherCondition.Drizzle => "Light Drizzle",
            WeatherCondition.Rain    => "Steady Rain",
            WeatherCondition.Storm   => "Thunderstorm",
            WeatherCondition.Fog     => "Morning Fog",
            WeatherCondition.Snow    => "Light Snowfall",
            _ => "Clear Skies"
        };


        public bool Equals(WeatherSample other) =>
            Condition == other.Condition &&
            Math.Abs(Intensity - other.Intensity) < 0.001f &&
            Math.Abs(WindSpeed - other.WindSpeed) < 0.001f &&
            Description == other.Description;

        public override bool Equals(object obj) => obj is WeatherSample other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Condition;
                hash = hash * 397 ^ (int)(Intensity * 1000f);
                hash = hash * 397 ^ (int)(WindSpeed * 100f);
                return hash;
            }
        }

        public override string ToString() => $"{Description} ({Condition}, {Intensity:P0})";

        public static bool operator ==(WeatherSample left, WeatherSample right) => left.Equals(right);
        public static bool operator !=(WeatherSample left, WeatherSample right) => !left.Equals(right);
    }
}
