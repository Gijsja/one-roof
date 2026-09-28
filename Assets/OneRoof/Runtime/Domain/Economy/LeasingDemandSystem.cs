using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Economy
{
    /// <summary>
    /// Autonomous domain service that evaluates vacant rooms and transit congestion,
    /// generating and moving in new households and working residents to fulfill tower demand.
    /// </summary>
    public sealed class LeasingDemandSystem
    {
        public const int MaxLeasesPerCycle = 2;
        public const int CongestionWaitThreshold = 50;
        public const int CongestionQueueThreshold = 40;

        public IReadOnlyList<PersonRecord> EvaluateLeasingDemand(
            BuildingTopologyState topology,
            PopulationState population,
            ElevatorBank elevatorBank,
            IRandomStream random,
            Tick tick,
            ref int nextEntityId,
            BusinessState businesses = null,
            float occupancyFactor = 1f)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (population == null) throw new ArgumentNullException(nameof(population));

            // Severe elevator congestion suppresses move-in demand
            if (elevatorBank != null &&
                elevatorBank.TotalQueuedCount > CongestionQueueThreshold &&
                elevatorBank.AverageWaitTicks > CongestionWaitThreshold)
            {
                return Array.Empty<PersonRecord>();
            }

            // 1. Identify occupied homes
            var occupiedHomes = new HashSet<EntityId>();
            foreach (var household in population.Households)
            {
                occupiedHomes.Add(household.HomeRoomId);
            }

            // 2. Locate vacant residential apartments
            var vacantApartments = new List<Room>();
            var potentialWorkplaces = new List<Room>();

            foreach (var room in topology.Rooms.Values)
            {
                var content = room.ContentType.Value;
                if (content.StartsWith("residential:") && !occupiedHomes.Contains(room.Id))
                {
                    vacantApartments.Add(room);
                }
                else if (businesses != null && (content.StartsWith("commercial:") ||
                         content.StartsWith("service:")))
                {
                    for (var businessIndex = 0; businessIndex < businesses.Businesses.Count; businessIndex++)
                    {
                        var tenant = businesses.Businesses[businessIndex];
                        if (tenant.RoomId.Equals(room.Id) &&
                            tenant.GetProductiveStaffCapacity(room, occupancyFactor) > 0)
                        {
                            potentialWorkplaces.Add(room);
                            break;
                        }
                    }
                }
            }

            if (vacantApartments.Count == 0)
            {
                return Array.Empty<PersonRecord>();
            }

            vacantApartments.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            potentialWorkplaces.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            var employeesPerWorkplace = new Dictionary<EntityId, int>();
            foreach (var person in population.Persons)
            {
                if (!person.WorkplaceLocation.IsOutside && person.WorkplaceRoomId.IsValid)
                {
                    employeesPerWorkplace.TryGetValue(person.WorkplaceRoomId, out var count);
                    employeesPerWorkplace[person.WorkplaceRoomId] = count + 1;
                }
            }

            var newResidents = new List<PersonRecord>();
            var leaseCount = Math.Min(vacantApartments.Count, MaxLeasesPerCycle);

            for (var i = 0; i < leaseCount; i++)
            {
                var apartment = vacantApartments[i];
                var householdId = new EntityId(nextEntityId++);
                var personId = new EntityId(nextEntityId++);

                var workplaceId = FindFundedWorkplace(potentialWorkplaces, businesses,
                    employeesPerWorkplace, occupancyFactor);
                if (workplaceId.IsValid)
                {
                    employeesPerWorkplace.TryGetValue(workplaceId, out var assigned);
                    employeesPerWorkplace[workplaceId] = assigned + 1;
                }

                var rng = random ?? new DeterministicRandomStream((ulong)nextEntityId);
                var traitKind = (i % 2 == 0) ? PersonTraitKind.EarlyBird : PersonTraitKind.Frugal;
                var trait = new PersonTrait(traitKind);
                var schedule = DailySchedule.Standard(trait, rng);
                var needs = new[]
                {
                    new NeedState(NeedKind.Hunger, 0.8f),
                    new NeedState(NeedKind.Rest, 0.9f),
                    new NeedState(NeedKind.Social, 0.7f)
                };

                var person = new PersonRecord(
                    personId,
                    householdId,
                    apartment.Id,
                    workplaceId,
                    schedule,
                    needs,
                    new[] { trait }, worksOutside: !workplaceId.IsValid);
                person.UpdateLocation(WorldLocation.Outside);

                var household = new HouseholdRecord(
                    householdId,
                    new[] { personId },
                    apartment.Id,
                    budget: 0.75f,
                    satisfaction: 0.85f,
                    cashBalance: HouseholdRecord.DefaultStartingCash);

                population.AddHousehold(household);
                population.AddPerson(person);
                newResidents.Add(person);
            }

            return newResidents;
        }

        /// <summary>
        /// Fills vacant, funded business positions from the existing Outside labor pool.
        /// Room and resident IDs break ties so re-leasing produces the same matches
        /// regardless of collection insertion order. Existing tower jobs are untouched.
        /// </summary>
        public int MatchExistingOutsideWorkers(
            BuildingTopologyState topology,
            PopulationState population,
            BusinessState businesses,
            float occupancyFactor = 1f,
            Func<PersonRecord, bool> canReassign = null)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (population == null) throw new ArgumentNullException(nameof(population));
            if (businesses == null) throw new ArgumentNullException(nameof(businesses));

            var rooms = new List<Room>();
            foreach (var room in topology.Rooms.Values)
            {
                var content = room.ContentType.Value ?? string.Empty;
                if (content.StartsWith("commercial:", StringComparison.Ordinal) ||
                    content.StartsWith("service:", StringComparison.Ordinal))
                    rooms.Add(room);
            }
            rooms.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));

            var assignedByRoom = new Dictionary<EntityId, int>();
            var candidates = new List<PersonRecord>();
            foreach (var person in population.Persons)
            {
                if (person.WorkplaceLocation.IsOutside)
                    candidates.Add(person);
                else if (person.WorkplaceRoomId.IsValid)
                {
                    assignedByRoom.TryGetValue(person.WorkplaceRoomId, out var count);
                    assignedByRoom[person.WorkplaceRoomId] = count + 1;
                }
            }
            candidates.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));

            var matched = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                var person = candidates[i];
                if (canReassign != null && !canReassign(person)) continue;
                var roomId = FindFundedWorkplace(rooms, businesses, assignedByRoom, occupancyFactor);
                if (!roomId.IsValid) break;
                if (!person.ReassignToRoomWork(roomId)) continue;
                assignedByRoom.TryGetValue(roomId, out var count);
                assignedByRoom[roomId] = count + 1;
                matched++;
            }
            return matched;
        }

        private static EntityId FindFundedWorkplace(
            List<Room> rooms,
            BusinessState businesses,
            Dictionary<EntityId, int> assignedByRoom,
            float occupancyFactor)
        {
            EntityId selected = default;
            var fewestAssigned = int.MaxValue;
            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                BusinessRecord tenant = null;
                for (var businessIndex = 0; businessIndex < businesses.Businesses.Count; businessIndex++)
                    if (businesses.Businesses[businessIndex].RoomId.Equals(room.Id))
                    {
                        tenant = businesses.Businesses[businessIndex];
                        break;
                    }
                if (tenant == null || tenant.IsVacantForReLease) continue;
                assignedByRoom.TryGetValue(room.Id, out var assigned);
                if (assigned >= tenant.GetProductiveStaffCapacity(room, occupancyFactor)) continue;

                // Forty-five is the highest daily specialist wage. Reserve enough for
                // every assigned worker, including this arrival, above the insolvency floor.
                if (tenant.CashBalance - (long)(assigned + 1) * 45 < BusinessRecord.InsolvencyThreshold) continue;
                if (assigned < fewestAssigned)
                {
                    fewestAssigned = assigned;
                    selected = room.Id;
                }
            }
            return selected;
        }
    }
}
