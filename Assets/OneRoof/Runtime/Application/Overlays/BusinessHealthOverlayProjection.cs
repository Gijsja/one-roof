using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Overlays
{
    /// <summary>Projects immutable tenant health data for the Data overlay.</summary>


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
