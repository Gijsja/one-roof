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
            ref int nextEntityId)
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
                else if (content.StartsWith("commercial:") ||
                         content.StartsWith("service:") ||
                         content.StartsWith("workplace:"))
                {
                    potentialWorkplaces.Add(room);
                }
            }

            if (vacantApartments.Count == 0)
            {
                return Array.Empty<PersonRecord>();
            }

            if (potentialWorkplaces.Count > 1)
            {
                var employeesPerWorkplace = new Dictionary<EntityId, int>();
                foreach (var p in population.Persons)
                {
                    if (p.WorkplaceRoomId.IsValid)
                    {
                        employeesPerWorkplace.TryGetValue(p.WorkplaceRoomId, out var count);
                        employeesPerWorkplace[p.WorkplaceRoomId] = count + 1;
                    }
                }

                potentialWorkplaces.Sort((a, b) =>
                {
                    employeesPerWorkplace.TryGetValue(a.Id, out var countA);
                    employeesPerWorkplace.TryGetValue(b.Id, out var countB);
                    return countA.CompareTo(countB);
                });
            }

            var fallbackWorkplaceId = potentialWorkplaces.Count > 0
                ? potentialWorkplaces[0].Id
                : vacantApartments[0].Id;

            var newResidents = new List<PersonRecord>();
            var leaseCount = Math.Min(vacantApartments.Count, MaxLeasesPerCycle);

            for (var i = 0; i < leaseCount; i++)
            {
                var apartment = vacantApartments[i];
                var householdId = new EntityId(nextEntityId++);
                var personId = new EntityId(nextEntityId++);

                var workplaceId = potentialWorkplaces.Count > 0
                    ? potentialWorkplaces[i % potentialWorkplaces.Count].Id
                    : fallbackWorkplaceId;

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
                    new[] { trait });

                var household = new HouseholdRecord(
                    householdId,
                    new[] { personId },
                    apartment.Id,
                    budget: 0.75f,
                    satisfaction: 0.85f);

                population.AddHousehold(household);
                population.AddPerson(person);
                newResidents.Add(person);
            }

            return newResidents;
        }
    }
}
