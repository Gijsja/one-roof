using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;

namespace OneRoof.Application.Overlays
{
    /// <summary>Projects active pedestrian movement by source floor without presentation-side simulation queries.</summary>
    public sealed class FootTrafficOverlayService
    {
        public FootTrafficOverlayProjection CreateOverlay(TowerSimulationSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var counts = new SortedDictionary<int, int>();
            foreach (var resident in session.TransitProjection().Residents)
                if (resident.Status == TransitResidentStatus.Walking || resident.Status == TransitResidentStatus.Queued || resident.Status == TransitResidentStatus.Riding)
                    counts[resident.Floor] = counts.TryGetValue(resident.Floor, out var count) ? count + 1 : 1;
            var flows = new List<FootTrafficFloorProjection>();
            foreach (var entry in counts) flows.Add(new FootTrafficFloorProjection(entry.Key, entry.Value));
            return new FootTrafficOverlayProjection(flows);
        }
    }

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
