using System;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Economy
{
    /// <summary>
    /// Mutable domain record managing tower financial treasury, construction costs,
    /// rental revenue, and sandbox/free-build toggles.
    /// </summary>
    public sealed class TowerEconomyState
    {
        public const long DefaultStartingTreasury = 50000;
        public const int CostPerSlabCell = 100;
        public const int CostPerResidentialCell = 250;
        public const int CostPerCommercialCell = 350;
        public const int CostPerAmenityCell = 150;
        public const int CostPerShaftCell = 500;
        public const int CostPerStairCell = 250;
        public const int ElevatorCarCost = 2500;

        public const int RentPerResidentCycle = 25;
        public const int RentPerCommercialRoomCycle = 200;

        public TowerEconomyState(long initialTreasury = DefaultStartingTreasury, bool sandboxMode = false, long totalRevenue = 0, long totalExpenses = 0)
        {
            CashBalance = initialTreasury;
            SandboxMode = sandboxMode;
            TotalRevenue = totalRevenue;
            TotalExpenses = totalExpenses;
        }

        public long CashBalance { get; private set; }

        public bool SandboxMode { get; set; }

        public long TotalRevenue { get; private set; }

        public long TotalExpenses { get; private set; }

        public bool CanAfford(long amount)
        {
            if (amount <= 0) return true;
            return SandboxMode || CashBalance >= amount;
        }

        public bool TryDeduct(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Expense amount cannot be negative.");
            }

            if (!CanAfford(amount))
            {
                return false;
            }

            if (!SandboxMode)
            {
                CashBalance -= amount;
            }

            TotalExpenses += amount;
            return true;
        }

        public void AddRevenue(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Revenue amount cannot be negative.");
            }

            CashBalance += amount;
            TotalRevenue += amount;
        }

        public int CalculateFloorSlabCost(CellBounds bounds)
        {
            return bounds.Width * CostPerSlabCell;
        }

        public int CalculateRoomCost(ContentId contentType, CellBounds bounds)
        {
            var value = contentType.Value;
            var rate = CostPerAmenityCell;

            if (value.StartsWith("residential:"))
            {
                rate = CostPerResidentialCell;
            }
            else if (value.StartsWith("commercial:"))
            {
                rate = CostPerCommercialCell;
            }
            else if (value.StartsWith("transit:"))
            {
                rate = CostPerShaftCell;
            }

            return bounds.Width * rate;
        }

        public int CalculateElevatorShaftCost(int floorSpan, int shaftWidth)
        {
            return floorSpan * shaftWidth * CostPerShaftCell;
        }

        public int CalculateStairwellCost(int floorSpan, int stairWidth)
        {
            return floorSpan * stairWidth * CostPerStairCell;
        }

        public long ProcessRentCycle(BuildingTopologyState topology, PopulationState population)
        {
            if (topology == null || population == null) return 0;

            long totalRent = 0;

            // Resident households pay residential rent
            totalRent += population.ResidentCount * RentPerResidentCycle;

            // Commercial rooms pay business rent
            foreach (var room in topology.Rooms.Values)
            {
                if (room.ContentType.Value.StartsWith("commercial:"))
                {
                    totalRent += RentPerCommercialRoomCycle;
                }
            }

            AddRevenue(totalRent);
            return totalRent;
        }
    }
}
