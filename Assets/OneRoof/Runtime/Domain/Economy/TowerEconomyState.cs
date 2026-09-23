using System;
using System.Collections.Generic;
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
        public const int CostPerUtilityCell = 400;
        public const int ElevatorCarCost = 2500;

        public const int RentPerResidentPerDay = 12;

        [Obsolete("Use RentPerResidentPerDay.")]
        public const int RentPerResidentCycle = RentPerResidentPerDay;

        [Obsolete("Commercial rent is settled by the business ledger.")]
        public const int RentPerCommercialRoomCycle = 200;

        private readonly List<HouseholdRecord> _settlementHouseholds = new List<HouseholdRecord>();
        private PolicyDecreeState _policy = PolicyDecreeState.Default;
        private long _pendingRent;
        private long _pendingTax;
        private long _pendingUpkeep;
        private long _pendingSubsidy;
        private long _pendingConstruction;
        private long _pendingConstructionSalvage;

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

        public PolicyDecreeState Policy => _policy;
        public long LastSettlementTick { get; private set; }
        public long LastDailyRent { get; private set; }
        public long LastDailyTax { get; private set; }
        public long LastDailyUpkeep { get; private set; }
        public long LastDailySubsidy { get; private set; }
        public long LastDailyConstruction { get; private set; }
        public long LastDailyConstructionSalvage { get; private set; }

        public void SetPolicy(PolicyDecreeState policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public long CalculateResidentialRentDue(HouseholdRecord household)
        {
            if (household == null) return 0;
            var residentUnits = household.MemberIds.Count * (long)RentPerResidentPerDay;
            return (long)Math.Round(residentUnits * (double)_policy.RentCapMultiplier, MidpointRounding.AwayFromZero);
        }

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
            _pendingConstruction += amount;
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

        public void RecordConstructionSalvage(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Construction salvage cannot be negative.");
            }

            AddRevenue(amount);
            _pendingConstructionSalvage += amount;
        }

        /// <summary>
        /// Reverses a previous <see cref="TryDeduct"/> (e.g. command validation passed but
        /// topology execution failed). Restores the ledger without polluting revenue.
        /// </summary>
        public void RefundExpense(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Refund amount cannot be negative.");
            }

            if (amount == 0) return;
            if (!SandboxMode)
            {
                CashBalance += amount;
            }

            TotalExpenses = Math.Max(0, TotalExpenses - amount);
            _pendingConstruction = Math.Max(0, _pendingConstruction - amount);
        }

        public int CalculateFloorSlabCost(CellBounds bounds)
        {
            return bounds.Width * CostPerSlabCell;
        }

        public int CalculateGroundSlabExpansionCost(CellBounds existingBounds, CellBounds expandedBounds)
        {
            return Math.Max(0, expandedBounds.Width - existingBounds.Width) * CostPerSlabCell;
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
            else if (value.StartsWith("utility:"))
            {
                rate = CostPerUtilityCell;
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
            _settlementHouseholds.Clear();
            for (var i = 0; i < population.Households.Count; i++)
            {
                _settlementHouseholds.Add(population.Households[i]);
            }
            _settlementHouseholds.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));

            for (var i = 0; i < _settlementHouseholds.Count; i++)
            {
                var household = _settlementHouseholds[i];
                var rentDue = CalculateResidentialRentDue(household);
                household.AdjustCashBalance(-rentDue);
                household.UpdateArrearsDays();
                totalRent += rentDue;
            }

            AddRevenue(totalRent);
            _pendingRent += totalRent;
            return totalRent;
        }

        public void RecordBusinessRentReceipt(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            AddRevenue(amount);
            _pendingRent += amount;
        }

        public void RecordTaxReceipt(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            AddRevenue(amount);
            _pendingTax += amount;
        }

        public void ChargeDailyExpense(long amount, bool subsidy)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            if (!SandboxMode) CashBalance -= amount;
            TotalExpenses += amount;
            if (subsidy) _pendingSubsidy += amount;
            else _pendingUpkeep += amount;
        }

        public void CompleteDailySettlement(long tick)
        {
            LastSettlementTick = Math.Max(0, tick);
            LastDailyRent = _pendingRent;
            LastDailyTax = _pendingTax;
            LastDailyUpkeep = _pendingUpkeep;
            LastDailySubsidy = _pendingSubsidy;
            LastDailyConstruction = _pendingConstruction;
            LastDailyConstructionSalvage = _pendingConstructionSalvage;
            _pendingRent = 0;
            _pendingTax = 0;
            _pendingUpkeep = 0;
            _pendingSubsidy = 0;
            _pendingConstruction = 0;
            _pendingConstructionSalvage = 0;
        }

        // ── Serialization ──────────────────────────────────────────────────────

        public OneRoof.Domain.Persistence.EconomySaveData ToSaveData()
        {
            return new OneRoof.Domain.Persistence.EconomySaveData
            {
                cashBalance = CashBalance,
                sandboxMode = SandboxMode,
                totalRevenue = TotalRevenue,
                totalExpenses = TotalExpenses,
                policyVersion = 1,
                rentCapMultiplier = _policy.RentCapMultiplier,
                commercialTaxRate = _policy.CommercialTaxRate,
                transitSubsidyEnabled = _policy.TransitSubsidyEnabled,
                quietHoursEnabled = _policy.QuietHoursEnabled,
                lastSettlementTick = LastSettlementTick,
                pendingRent = _pendingRent,
                pendingTax = _pendingTax,
                pendingUpkeep = _pendingUpkeep,
                pendingSubsidy = _pendingSubsidy,
                pendingConstruction = _pendingConstruction,
                pendingConstructionSalvage = _pendingConstructionSalvage,
                lastDailyRent = LastDailyRent,
                lastDailyTax = LastDailyTax,
                lastDailyUpkeep = LastDailyUpkeep,
                lastDailySubsidy = LastDailySubsidy,
                lastDailyConstruction = LastDailyConstruction,
                lastDailyConstructionSalvage = LastDailyConstructionSalvage
            };
        }

        public static TowerEconomyState FromSaveData(OneRoof.Domain.Persistence.EconomySaveData data)
        {
            if (data == null) return new TowerEconomyState();
            var state = new TowerEconomyState(data.cashBalance, data.sandboxMode, data.totalRevenue, data.totalExpenses);
            if (data.policyVersion > 0)
            {
                state.SetPolicy(new PolicyDecreeState(data.rentCapMultiplier, data.commercialTaxRate,
                    data.transitSubsidyEnabled, data.quietHoursEnabled));
            }
            state.LastSettlementTick = Math.Max(0, data.lastSettlementTick);
            state._pendingRent = Math.Max(0, data.pendingRent);
            state._pendingTax = Math.Max(0, data.pendingTax);
            state._pendingUpkeep = Math.Max(0, data.pendingUpkeep);
            state._pendingSubsidy = Math.Max(0, data.pendingSubsidy);
            state._pendingConstruction = Math.Max(0, data.pendingConstruction);
            state._pendingConstructionSalvage = Math.Max(0, data.pendingConstructionSalvage);
            state.LastDailyRent = Math.Max(0, data.lastDailyRent);
            state.LastDailyTax = Math.Max(0, data.lastDailyTax);
            state.LastDailyUpkeep = Math.Max(0, data.lastDailyUpkeep);
            state.LastDailySubsidy = Math.Max(0, data.lastDailySubsidy);
            state.LastDailyConstruction = Math.Max(0, data.lastDailyConstruction);
            state.LastDailyConstructionSalvage = Math.Max(0, data.lastDailyConstructionSalvage);
            return state;
        }
    }
}
