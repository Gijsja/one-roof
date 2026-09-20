using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Overlays
{
    /// <summary>Projects immutable tenant health data for the Data overlay.</summary>
    public sealed class BusinessHealthOverlayService
    {
        public BusinessHealthOverlayProjection CreateOverlay(TowerSimulationSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var floors = new SortedDictionary<int, BusinessHealthAccumulator>();
            foreach (var business in session.Simulation.Businesses.Businesses)
            {
                if (!session.Topology.TryGetRoom(business.RoomId, out var room)) continue;
                if (!floors.TryGetValue(room.Floor, out var value)) floors.Add(room.Floor, value = new BusinessHealthAccumulator());
                value.Tenants++;
                if (business.IsInsolvent) value.Insolvent++;
                value.NetCash += business.CashBalance;
            }
            var result = new List<BusinessHealthFloorProjection>();
            foreach (var floor in floors) result.Add(new BusinessHealthFloorProjection(floor.Key, floor.Value.Tenants, floor.Value.Insolvent, floor.Value.NetCash));
            return new BusinessHealthOverlayProjection(result);
        }

        private sealed class BusinessHealthAccumulator { public int Tenants; public int Insolvent; public long NetCash; }
    }

    public sealed class BusinessHealthOverlayProjection
    {
        public BusinessHealthOverlayProjection(IReadOnlyList<BusinessHealthFloorProjection> floors) { Floors = floors ?? Array.Empty<BusinessHealthFloorProjection>(); }
        public IReadOnlyList<BusinessHealthFloorProjection> Floors { get; }
    }

    public readonly struct BusinessHealthFloorProjection
    {
        public BusinessHealthFloorProjection(int floor, int tenants, int insolventTenants, long netCash) { Floor = floor; Tenants = tenants; InsolventTenants = insolventTenants; NetCash = netCash; }
        public int Floor { get; } public int Tenants { get; } public int InsolventTenants { get; } public long NetCash { get; }
        public string AccessibilityLabel => $"Floor {Floor}: {Tenants} tenants, {InsolventTenants} insolvent, tenant cash ${NetCash:N0}";
    }
}
