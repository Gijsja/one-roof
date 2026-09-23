using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Economy
{
    /// <summary>Campaign-owned commercial tenant state derived from leased commercial and service rooms.</summary>
    public sealed class BusinessState
    {
        private readonly List<BusinessRecord> _businesses;
        private readonly List<HouseholdRecord> _orderedHouseholds = new List<HouseholdRecord>();

        public BusinessState(IEnumerable<BusinessRecord> businesses = null)
        {
            _businesses = businesses == null ? new List<BusinessRecord>() : new List<BusinessRecord>(businesses);
            Businesses = new ReadOnlyCollection<BusinessRecord>(_businesses);
        }

        public IReadOnlyList<BusinessRecord> Businesses { get; }

        public void Advance(BuildingTopologyState topology, PopulationState population, ref int nextEntityId)
        {
            if (topology == null || population == null) return;
            var activeRooms = new HashSet<EntityId>();
            var rooms = new List<Room>();
            foreach (var room in topology.Rooms.Values)
                if (IsBusinessRoom(room.ContentType)) rooms.Add(room);
            rooms.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));

            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                activeRooms.Add(room.Id);
                var business = FindByRoom(room.Id);
                if (business == null)
                {
                    business = new BusinessRecord(new EntityId(nextEntityId++), room.Id, room.ContentType);
                    _businesses.Add(business);
                }
                business.ReconcileEmployees(population, room.Capacity);
            }
            _businesses.RemoveAll(business => !activeRooms.Contains(business.RoomId));
            _businesses.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));
        }

        /// <summary>
        /// Runs one deterministic daily business settlement. Business records are processed in stable ID order.
        /// </summary>
        public void ProcessBusinessCycle(
            BuildingTopologyState topology,
            PopulationState population,
            TowerEconomyState treasury,
            float occupancyFactor,
            PolicyDecreeState policy)
        {
            if (population == null) return;
            _businesses.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));
            _orderedHouseholds.Clear();
            for (var i = 0; i < population.Households.Count; i++) _orderedHouseholds.Add(population.Households[i]);
            _orderedHouseholds.Sort((left, right) => left.Id.Value.CompareTo(right.Id.Value));

            long dailyServiceBudget = 0;
            var totalWalkInStaff = 0;
            for (var i = 0; i < _orderedHouseholds.Count; i++)
                dailyServiceBudget += _orderedHouseholds[i].MemberIds.Count * 5L;
            for (var i = 0; i < _businesses.Count; i++)
                if (_businesses[i].IsWalkIn) totalWalkInStaff += _businesses[i].EmployeeIds.Count;

            for (var i = 0; i < _businesses.Count; i++)
            {
                Room room = null;
                if (topology != null) topology.TryGetRoom(_businesses[i].RoomId, out room);
                var serviceBudget = _businesses[i].IsWalkIn && totalWalkInStaff > 0
                    ? dailyServiceBudget * _businesses[i].EmployeeIds.Count / totalWalkInStaff
                    : 0;
                _businesses[i].ProcessCycle(population, room, treasury, occupancyFactor, policy ?? PolicyDecreeState.Default, _orderedHouseholds, serviceBudget);
            }
        }

        /// <summary>Compatibility entry point for callers not yet wired to daily topology and treasury settlement.</summary>
        public void ProcessBusinessCycle(PopulationState population) =>
            ProcessBusinessCycle(null, population, null, 1f, PolicyDecreeState.Default);

        public BusinessSaveData[] ToSaveData()
        {
            var result = new BusinessSaveData[_businesses.Count];
            for (var i = 0; i < result.Length; i++) result[i] = _businesses[i].ToSaveData();
            return result;
        }

        public static BusinessState FromSaveData(BusinessSaveData[] data)
        {
            if (data == null) return new BusinessState();
            var businesses = new List<BusinessRecord>();
            for (var i = 0; i < data.Length; i++)
                if (data[i] != null && data[i].id > 0 && data[i].roomId > 0)
                    businesses.Add(BusinessRecord.FromSaveData(data[i]));
            return new BusinessState(businesses);
        }

        private BusinessRecord FindByRoom(EntityId roomId)
        {
            for (var i = 0; i < _businesses.Count; i++)
                if (_businesses[i].RoomId.Equals(roomId)) return _businesses[i];
            return null;
        }

        private static bool IsBusinessRoom(ContentId contentType)
        {
            var value = contentType.Value ?? string.Empty;
            return value.StartsWith("commercial:", StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("service:", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class BusinessRecord
    {
        public const long OperatingCostPerDay = 35;
        public const long InsolvencyThreshold = -100;
        public const int ReLeaseAfterInsolventDays = 7;
        private const int WalkInServeRate = 8;
        private const int WalkInDemandPerRoomCell = 4;
        private readonly List<EntityId> _employeeIds = new List<EntityId>();

        public BusinessRecord(
            EntityId id,
            EntityId roomId,
            ContentId contentType,
            long cashBalance = 0,
            bool isInsolvent = false,
            int arrearsDays = 0)
        {
            id.EnsureValid(); roomId.EnsureValid();
            Id = id;
            RoomId = roomId;
            ContentType = contentType;
            CashBalance = cashBalance;
            IsInsolvent = isInsolvent;
            ArrearsDays = Math.Max(0, arrearsDays);
            EmployeeIds = new ReadOnlyCollection<EntityId>(_employeeIds);
        }

        public EntityId Id { get; }
        public EntityId RoomId { get; }
        public ContentId ContentType { get; }
        public IReadOnlyList<EntityId> EmployeeIds { get; }
        public long CashBalance { get; private set; }
        public long LastCustomerRevenue { get; private set; }
        public long LastContractRevenue { get; private set; }
        public long LastWages { get; private set; }
        public long LastOperatingCost { get; private set; }
        public long LastRentPaid { get; private set; }
        public long LastTaxPaid { get; private set; }
        public bool WageArrears { get; private set; }
        public int ArrearsDays { get; private set; }
        public bool IsInsolvent { get; private set; }
        public bool IsVacantForReLease => IsInsolvent && ArrearsDays >= ReLeaseAfterInsolventDays;
        internal bool IsWalkIn => IsWalkInBusiness(ContentType);

        public void ReconcileEmployees(PopulationState population, int capacity)
        {
            _employeeIds.Clear();
            if (population == null || IsInsolvent || capacity <= 0) return;
            var candidates = new List<PersonRecord>();
            for (var i = 0; i < population.Persons.Count; i++)
                if (population.Persons[i].WorkplaceRoomId.Equals(RoomId)) candidates.Add(population.Persons[i]);

            var preferredRole = PreferredRole(ContentType);
            candidates.Sort((left, right) =>
            {
                var leftPreferred = left.Specialization.Role == preferredRole;
                var rightPreferred = right.Specialization.Role == preferredRole;
                if (leftPreferred != rightPreferred) return leftPreferred ? -1 : 1;
                return left.Id.Value.CompareTo(right.Id.Value);
            });
            for (var i = 0; i < candidates.Count && _employeeIds.Count < capacity; i++) _employeeIds.Add(candidates[i].Id);
        }

        /// <summary>Legacy direct cycle retained for existing domain callers; production settlement uses the full overload.</summary>
        public void ProcessCycle(PopulationState population) =>
            ProcessCycle(population, null, null, 1f, PolicyDecreeState.Default, null, 0);

        public void ProcessCycle(
            PopulationState population,
            Room room,
            TowerEconomyState treasury,
            float occupancyFactor,
            PolicyDecreeState policy,
            IReadOnlyList<HouseholdRecord> orderedHouseholds,
            long walkInSpendBudget)
        {
            if (population == null) return;
            LastCustomerRevenue = 0;
            LastContractRevenue = 0;
            LastWages = 0;
            LastOperatingCost = 0;
            LastRentPaid = 0;
            LastTaxPaid = 0;
            WageArrears = false;

            if (IsInsolvent)
            {
                ArrearsDays = IncrementSaturated(ArrearsDays);
                return;
            }

            policy = policy ?? PolicyDecreeState.Default;
            var wages = CalculatePayroll(population);
            if (CashBalance - wages < InsolvencyThreshold)
            {
                WageArrears = wages > 0;
            }
            else
            {
                LastWages = wages;
                CashBalance -= wages;
                PayEmployees(population);
            }

            var factor = NormalizeOccupancy(occupancyFactor);
            if (room != null && IsWalkInBusiness(ContentType))
            {
                var expectedRevenue = CalculateWalkInRevenue(room, factor, policy.QuietHoursEnabled);
                LastCustomerRevenue = SpendWalkInRevenue(orderedHouseholds, Math.Min(expectedRevenue, Math.Max(0, walkInSpendBudget)));
            }
            else if (room != null && IsContractBusiness(ContentType))
            {
                LastContractRevenue = CalculateContractRevenue(factor);
            }
            CashBalance += LastCustomerRevenue + LastContractRevenue;

            if (room != null && treasury != null)
            {
                LastRentPaid = CalculateRent(room, policy.RentCapMultiplier);
                LastTaxPaid = CalculateTax(LastCustomerRevenue + LastContractRevenue, policy.CommercialTaxRate);
                CashBalance -= LastRentPaid + LastTaxPaid;
                if (LastRentPaid > 0) treasury.RecordBusinessRentReceipt(LastRentPaid);
                if (LastTaxPaid > 0) treasury.RecordTaxReceipt(LastTaxPaid);
            }

            LastOperatingCost = OperatingCostPerDay;
            CashBalance -= LastOperatingCost;
            if (CashBalance < InsolvencyThreshold)
            {
                IsInsolvent = true;
                ArrearsDays = 1;
            }
            else if (CashBalance < 0)
            {
                ArrearsDays = IncrementSaturated(ArrearsDays);
            }
            else
            {
                ArrearsDays = 0;
            }
        }

        public BusinessSaveData ToSaveData() => new BusinessSaveData
        {
            id = Id.Value,
            roomId = RoomId.Value,
            contentType = ContentType.Value,
            employeeIds = EmployeeIdsToArray(),
            cashBalance = CashBalance,
            lastCustomerRevenue = LastCustomerRevenue,
            lastContractRevenue = LastContractRevenue,
            lastWages = LastWages,
            lastOperatingCost = LastOperatingCost,
            lastRentPaid = LastRentPaid,
            lastTaxPaid = LastTaxPaid,
            arrearsDays = ArrearsDays,
            wageArrears = WageArrears,
            isInsolvent = IsInsolvent
        };

        public static BusinessRecord FromSaveData(BusinessSaveData data)
        {
            var record = new BusinessRecord(new EntityId(data.id), new EntityId(data.roomId), new ContentId(data.contentType), data.cashBalance, data.isInsolvent, data.arrearsDays)
            {
                LastCustomerRevenue = data.lastCustomerRevenue,
                LastContractRevenue = data.lastContractRevenue,
                LastWages = data.lastWages,
                LastOperatingCost = data.lastOperatingCost,
                LastRentPaid = data.lastRentPaid,
                LastTaxPaid = data.lastTaxPaid,
                WageArrears = data.wageArrears
            };
            if (data.employeeIds != null)
                for (var i = 0; i < data.employeeIds.Length; i++) if (data.employeeIds[i] > 0) record._employeeIds.Add(new EntityId(data.employeeIds[i]));
            return record;
        }

        private long CalculatePayroll(PopulationState population)
        {
            long wages = 0;
            for (var i = 0; i < _employeeIds.Count; i++)
            {
                if (!population.TryGetPerson(_employeeIds[i], out var person)) continue;
                wages += WageRate(person.Specialization.Role);
            }
            return wages;
        }

        private void PayEmployees(PopulationState population)
        {
            // Employee IDs are selected deterministically by role preference, then person ID.
            for (var i = 0; i < _employeeIds.Count; i++)
            {
                if (!population.TryGetPerson(_employeeIds[i], out var person)) continue;
                if (population.TryGetHousehold(person.HouseholdId, out var household))
                    household.RecordDailyIncome(WageRate(person.Specialization.Role));
            }
        }

        private static long SpendWalkInRevenue(IReadOnlyList<HouseholdRecord> households, long revenue)
        {
            if (households == null) return 0;
            var remaining = revenue;
            long paidTotal = 0;
            for (var i = 0; i < households.Count && remaining > 0; i++)
            {
                var spend = households[i].SpendOnService(remaining);
                if (spend <= 0) continue;
                paidTotal += spend;
                remaining -= spend;
            }
            return paidTotal;
        }

        private long CalculateWalkInRevenue(Room room, float occupancyFactor, bool quietHours)
        {
            var customersByStaff = _employeeIds.Count * WalkInServeRate;
            var demandCap = (int)Math.Floor(WalkInDemandPerRoomCell * room.Capacity * (double)occupancyFactor);
            var customers = Math.Min(customersByStaff, Math.Max(0, demandCap));
            long gross = (long)customers * WalkInTicket(ContentType);
            if (quietHours) gross = (long)Math.Floor(gross * 0.9d);
            return gross;
        }

        private long CalculateContractRevenue(float occupancyFactor) =>
            (long)Math.Floor(_employeeIds.Count * (double)ContractRate(ContentType) * occupancyFactor);

        private static long CalculateRent(Room room, float rentMultiplier)
        {
            var ratePerCell = IsReducedRentBusiness(room.ContentType) ? 5 : 8;
            return (long)Math.Round(room.Bounds.Width * ratePerCell * (double)rentMultiplier, MidpointRounding.AwayFromZero);
        }

        private static long CalculateTax(long grossRevenue, float rate) =>
            (long)Math.Round(grossRevenue * (double)rate, MidpointRounding.AwayFromZero);

        private static int WageRate(SpecialistRole role)
        {
            switch (role)
            {
                case SpecialistRole.Maintenance: return 35;
                case SpecialistRole.Security: return 35;
                case SpecialistRole.Knowledge: return 45;
                case SpecialistRole.None:
                case SpecialistRole.Service:
                default: return 30;
            }
        }

        private static int WalkInTicket(ContentId contentType)
        {
            var value = contentType.Value ?? string.Empty;
            if (value.IndexOf("clinic", StringComparison.OrdinalIgnoreCase) >= 0) return 10;
            if (value.IndexOf("retail", StringComparison.OrdinalIgnoreCase) >= 0) return 8;
            return 6;
        }

        private static int ContractRate(ContentId contentType)
        {
            var value = contentType.Value ?? string.Empty;
            if (value.IndexOf("office", StringComparison.OrdinalIgnoreCase) >= 0) return 55;
            if (value.IndexOf("security", StringComparison.OrdinalIgnoreCase) >= 0) return 38;
            return 40;
        }

        private static bool IsWalkInBusiness(ContentId contentType)
        {
            var value = contentType.Value ?? string.Empty;
            return value.IndexOf("diner", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("retail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("clinic", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsContractBusiness(ContentId contentType)
        {
            var value = contentType.Value ?? string.Empty;
            return value.IndexOf("office", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("workshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("security", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsReducedRentBusiness(ContentId contentType)
        {
            var value = contentType.Value ?? string.Empty;
            return value.IndexOf("clinic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("workshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("security", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float NormalizeOccupancy(float occupancyFactor)
        {
            if (float.IsNaN(occupancyFactor) || float.IsInfinity(occupancyFactor)) return 0f;
            return Math.Max(0f, Math.Min(1f, occupancyFactor));
        }

        private int[] EmployeeIdsToArray()
        {
            var result = new int[_employeeIds.Count];
            for (var i = 0; i < result.Length; i++) result[i] = _employeeIds[i].Value;
            return result;
        }

        private static SpecialistRole PreferredRole(ContentId contentType)
        {
            var value = contentType.Value ?? string.Empty;
            if (value.Contains("maintenance")) return SpecialistRole.Maintenance;
            if (value.Contains("security")) return SpecialistRole.Security;
            if (value.Contains("office")) return SpecialistRole.Knowledge;
            return SpecialistRole.Service;
        }

        private static int IncrementSaturated(int value) => value == int.MaxValue ? int.MaxValue : value + 1;
    }
}
