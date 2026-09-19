using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Application.Overlays
{
    /// <summary>Immutable, non-colour-dependent population and demographic data for Data mode.</summary>
    public sealed class PopulationOverlayProjection
    {
        public PopulationOverlayProjection(int residentCount, IReadOnlyList<PopulationFloorProjection> floors)
        {
            ResidentCount = residentCount;
            Floors = new ReadOnlyCollection<PopulationFloorProjection>(new List<PopulationFloorProjection>(floors ?? Array.Empty<PopulationFloorProjection>()));
        }

        public int ResidentCount { get; }
        public IReadOnlyList<PopulationFloorProjection> Floors { get; }

        public bool TryGetFloor(int floor, out PopulationFloorProjection projection)
        {
            for (var i = 0; i < Floors.Count; i++)
            {
                if (Floors[i].Floor != floor) continue;
                projection = Floors[i];
                return true;
            }

            projection = default;
            return false;
        }
    }

    public enum PopulationDensityTier { Quiet, Active, Dense }

    /// <summary>One floor's readable count, density and demographic distribution.</summary>
    public readonly struct PopulationFloorProjection
    {
        public PopulationFloorProjection(int floor, int residentCount, int capacity, int youngAdultCount, int adultCount, int olderAdultCount, int limitedResourceCount, int stableResourceCount, int comfortableResourceCount)
        {
            Floor = floor;
            ResidentCount = residentCount;
            Capacity = capacity;
            YoungAdultCount = youngAdultCount;
            AdultCount = adultCount;
            OlderAdultCount = olderAdultCount;
            LimitedResourceCount = limitedResourceCount;
            StableResourceCount = stableResourceCount;
            ComfortableResourceCount = comfortableResourceCount;
        }

        public int Floor { get; }
        public int ResidentCount { get; }
        public int Capacity { get; }
        public int YoungAdultCount { get; }
        public int AdultCount { get; }
        public int OlderAdultCount { get; }
        public int LimitedResourceCount { get; }
        public int StableResourceCount { get; }
        public int ComfortableResourceCount { get; }
        public float Density => Capacity <= 0 ? 0f : Math.Min(1f, (float)ResidentCount / Capacity);
        public PopulationDensityTier DensityTier => Density < .35f ? PopulationDensityTier.Quiet : Density < .7f ? PopulationDensityTier.Active : PopulationDensityTier.Dense;
        public string AccessibilityLabel => $"Floor {Floor}: {ResidentCount}/{Capacity} residents [{DensityTier.ToString().ToUpperInvariant()}] | Age 18–29: {YoungAdultCount}, 30–49: {AdultCount}, 50+: {OlderAdultCount} | Resources limited: {LimitedResourceCount}, stable: {StableResourceCount}, comfortable: {ComfortableResourceCount}";
    }
}
