using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;

namespace OneRoof.Application.Overlays
{
    /// <summary>Projects active pedestrian movement by source floor without presentation-side simulation queries.</summary>


    public sealed class FootTrafficOverlayProjection
    {
        public FootTrafficOverlayProjection(IReadOnlyList<FootTrafficFloorProjection> floors) { Floors = floors ?? Array.Empty<FootTrafficFloorProjection>(); }
        public IReadOnlyList<FootTrafficFloorProjection> Floors { get; }
    }

    public readonly struct FootTrafficFloorProjection
    {
        public FootTrafficFloorProjection(int floor, int movingResidents) { Floor = floor; MovingResidents = movingResidents; }
        public int Floor { get; } public int MovingResidents { get; }
        public string AccessibilityLabel => $"Floor {Floor}: {MovingResidents} residents in transit";
    }
}
