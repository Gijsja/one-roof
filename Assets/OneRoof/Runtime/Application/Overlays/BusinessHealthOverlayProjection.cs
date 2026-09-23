using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Overlays
{
    /// <summary>Projects immutable tenant health data for the Data overlay.</summary>


    public sealed class BusinessHealthOverlayProjection
    {
        public BusinessHealthOverlayProjection(IReadOnlyList<BusinessHealthFloorProjection> floors, IReadOnlyList<BusinessTenantProjection> tenants = null)
        {
            Floors = floors ?? Array.Empty<BusinessHealthFloorProjection>();
            Tenants = tenants ?? Array.Empty<BusinessTenantProjection>();
        }
        public IReadOnlyList<BusinessHealthFloorProjection> Floors { get; }
        public IReadOnlyList<BusinessTenantProjection> Tenants { get; }
    }

    public readonly struct BusinessHealthFloorProjection
    {
        public BusinessHealthFloorProjection(int floor, int tenants, int insolventTenants, long netCash) { Floor = floor; Tenants = tenants; InsolventTenants = insolventTenants; NetCash = netCash; }
        public int Floor { get; } public int Tenants { get; } public int InsolventTenants { get; } public long NetCash { get; }
        public string AccessibilityLabel => $"Floor {Floor}: {Tenants} tenants, {InsolventTenants} insolvent, tenant cash ${NetCash:N0}";
    }

    public readonly struct BusinessTenantProjection
    {
        public BusinessTenantProjection(int businessId, int roomId, int floor, string contentType, long cashBalance, int employeeCount,
            long customerRevenue, long contractRevenue, long wages, long operatingCost, long rentPaid, long taxPaid, int arrearsDays, bool wageArrears, bool insolvent, bool vacantForReLease)
        {
            BusinessId = businessId;
            RoomId = roomId;
            Floor = floor;
            ContentType = contentType ?? string.Empty;
            CashBalance = cashBalance;
            EmployeeCount = employeeCount;
            CustomerRevenue = customerRevenue;
            ContractRevenue = contractRevenue;
            Wages = wages;
            OperatingCost = operatingCost;
            RentPaid = rentPaid;
            TaxPaid = taxPaid;
            ArrearsDays = arrearsDays;
            WageArrears = wageArrears;
            IsInsolvent = insolvent;
            IsVacantForReLease = vacantForReLease;
        }

        public int BusinessId { get; }
        public int RoomId { get; }
        public int Floor { get; }
        public string ContentType { get; }
        public long CashBalance { get; }
        public int EmployeeCount { get; }
        public long CustomerRevenue { get; }
        public long ContractRevenue { get; }
        public long Wages { get; }
        public long OperatingCost { get; }
        public long RentPaid { get; }
        public long TaxPaid { get; }
        public int ArrearsDays { get; }
        public bool WageArrears { get; }
        public bool IsInsolvent { get; }
        public bool IsVacantForReLease { get; }
        public long Margin => CustomerRevenue + ContractRevenue - Wages - OperatingCost - RentPaid - TaxPaid;
        public string AccessibilityLabel => $"Floor {Floor}, tenant {BusinessId} {ContentType}: {EmployeeCount} workers, margin ${Margin:+#,0;-#,0;0}, cash ${CashBalance:N0}, rent ${RentPaid:N0}, tax ${TaxPaid:N0}, arrears {ArrearsDays} day(s){(WageArrears ? ", wages in arrears" : string.Empty)}{(IsInsolvent ? ", insolvent" : string.Empty)}{(IsVacantForReLease ? ", vacant for re-lease" : string.Empty)}";
    }
}
