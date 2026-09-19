using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Application.Overlays
{
    /// <summary>Immutable, non-colour-dependent satisfaction overlay data.</summary>
    public sealed class SatisfactionOverlayProjection
    {
        public SatisfactionOverlayProjection(float towerSatisfaction, IReadOnlyList<SatisfactionFloorProjection> floors)
        {
            TowerSatisfaction = towerSatisfaction;
            Floors = new ReadOnlyCollection<SatisfactionFloorProjection>(new List<SatisfactionFloorProjection>(floors ?? Array.Empty<SatisfactionFloorProjection>()));
        }
        public float TowerSatisfaction { get; }
        public IReadOnlyList<SatisfactionFloorProjection> Floors { get; }
    }

    public readonly struct SatisfactionFloorProjection
    {
        public SatisfactionFloorProjection(int floor, float satisfaction, int residentCount, int grievanceCount)
        { Floor = floor; Satisfaction = satisfaction; ResidentCount = residentCount; GrievanceCount = grievanceCount; }
        public int Floor { get; }
        public float Satisfaction { get; }
        public int ResidentCount { get; }
        public int GrievanceCount { get; }
        public string AccessibilityLabel => $"Floor {Floor}: {Satisfaction:P0} satisfaction, {GrievanceCount} active grievances";
    }
}
