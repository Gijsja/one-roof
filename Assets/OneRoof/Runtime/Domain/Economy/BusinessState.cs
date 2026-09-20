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

        public void ProcessBusinessCycle(PopulationState population)
        {
            if (population == null) return;
            for (var i = 0; i < _businesses.Count; i++)
            {
                var business = _businesses[i];
                if (business.IsInsolvent) continue;
                business.ProcessCycle(population);
            }
        }

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
        private const long RevenuePerEmployee = 40;
        private const long WagePerEmployee = 15;
        private const long OperatingCostPerCycle = 35;
        private const long InsolvencyThreshold = -100;
        private readonly List<EntityId> _employeeIds = new List<EntityId>();

        public BusinessRecord(EntityId id, EntityId roomId, ContentId contentType, long cashBalance = 0, bool isInsolvent = false)
        {
            id.EnsureValid(); roomId.EnsureValid();
            Id = id; RoomId = roomId; ContentType = contentType; CashBalance = cashBalance; IsInsolvent = isInsolvent;
            EmployeeIds = new ReadOnlyCollection<EntityId>(_employeeIds);
        }

        public EntityId Id { get; }
        public EntityId RoomId { get; }
        public ContentId ContentType { get; }
        public IReadOnlyList<EntityId> EmployeeIds { get; }
        public long CashBalance { get; private set; }
        public long LastCustomerRevenue { get; private set; }
        public long LastWages { get; private set; }
        public bool IsInsolvent { get; private set; }

        public void ReconcileEmployees(PopulationState population, int capacity)
        {
            _employeeIds.Clear();
            if (IsInsolvent || capacity <= 0) return;
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

        public void ProcessCycle(PopulationState population)
        {
            LastCustomerRevenue = _employeeIds.Count * RevenuePerEmployee;
            LastWages = _employeeIds.Count * WagePerEmployee;
            CashBalance += LastCustomerRevenue - LastWages - OperatingCostPerCycle;
            for (var i = 0; i < _employeeIds.Count; i++)
            {
                if (!population.TryGetPerson(_employeeIds[i], out var person)) continue;
                if (!population.TryGetHousehold(person.HouseholdId, out var household)) continue;
                household.AdjustBudget(.01f);
            }
            if (CashBalance < InsolvencyThreshold) IsInsolvent = true;
        }

        public BusinessSaveData ToSaveData() => new BusinessSaveData
        {
            id = Id.Value, roomId = RoomId.Value, contentType = ContentType.Value,
            employeeIds = EmployeeIdsToArray(), cashBalance = CashBalance,
            lastCustomerRevenue = LastCustomerRevenue, lastWages = LastWages, isInsolvent = IsInsolvent
        };

        public static BusinessRecord FromSaveData(BusinessSaveData data)
        {
            var record = new BusinessRecord(new EntityId(data.id), new EntityId(data.roomId), new ContentId(data.contentType), data.cashBalance, data.isInsolvent)
            {
                LastCustomerRevenue = data.lastCustomerRevenue,
                LastWages = data.lastWages
            };
            if (data.employeeIds != null)
                for (var i = 0; i < data.employeeIds.Length; i++) if (data.employeeIds[i] > 0) record._employeeIds.Add(new EntityId(data.employeeIds[i]));
            return record;
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
    }
}
