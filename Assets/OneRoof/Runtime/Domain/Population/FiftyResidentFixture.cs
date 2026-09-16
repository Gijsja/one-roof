using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Deterministic factory that creates 50 residents across 16 households in the canonical
    /// five-floor building topology used by first-playable acceptance tests.
    ///
    /// Layout:
    ///   Floor 0 — Lobby, Diner (workplace for all residents), Elevator shaft.
    ///   Floors 1–4 — 4 residential apartments each (16 rooms total).
    ///   16 households, one per apartment. Residents 1–50 distributed 3–4 per household.
    ///
    /// All randomness flows through the injected <see cref="IRandomStream"/> so the fixture
    /// is exactly reproducible for any given seed.
    /// </summary>
    public static class FiftyResidentFixture
    {
        public const int TotalResidents = 50;
        public const int TotalHouseholds = 16;   // 4 floors × 4 apartments
        public const int FloorsWithApartments = 4;
        public const int ApartmentsPerFloor = 4;

        // ── Entity-ID allocation ──────────────────────────────────────────────
        // Households: IDs 1001–1016
        // Persons:    IDs 2001–2050
        private const int HouseholdIdBase = 1000;
        private const int PersonIdBase    = 2000;

        /// <summary>
        /// Creates a <see cref="PopulationState"/> with 50 persons and 16 households
        /// using rooms from <paramref name="topology"/> and randomness from <paramref name="rng"/>.
        /// </summary>
        /// <param name="topology">
        /// A building topology that must contain residential rooms on floors 1–4
        /// and a commercial diner room on floor 0, matching <see cref="FiveFloorTopologyFixture"/>.
        /// </param>
        /// <param name="rng">
        /// A seeded, deterministic random stream. The same seed produces the same population.
        /// </param>
        public static PopulationState Create()
        {
            return Create(FiveFloorTopologyFixture.Create(), new DeterministicRandomStream(1337));
        }

        public static PopulationState Create(BuildingTopology topology, IRandomStream rng)
        {
            if (topology == null) throw new ArgumentNullException(nameof(topology));
            if (rng == null)      throw new ArgumentNullException(nameof(rng));

            // Collect residential rooms: floors 1–4, 4 apartments each.
            var residentialRooms = CollectResidentialRooms(topology);
            if (residentialRooms.Count != TotalHouseholds)
            {
                throw new InvalidOperationException(
                    $"FiftyResidentFixture expects {TotalHouseholds} residential rooms but found {residentialRooms.Count}. " +
                    "Ensure the topology was created with FiveFloorTopologyFixture.Create().");
            }

            // Find the diner on Floor 0 (workplace for all residents).
            var workplaceRoomId = FindDinerRoomId(topology);

            // Build households (one per apartment).
            var households = new List<HouseholdRecord>(TotalHouseholds);
            for (var i = 0; i < TotalHouseholds; i++)
            {
                var householdId = new EntityId(HouseholdIdBase + i + 1);
                var homeRoomId  = residentialRooms[i];
                var budget      = Clamp01((rng.NextInt(40, 101)) / 100f); // 40–100 % wealth
                households.Add(new HouseholdRecord(
                    householdId,
                    memberIds:    Array.Empty<EntityId>(), // filled below after persons created
                    homeRoomId:   homeRoomId,
                    budget:       budget,
                    satisfaction: 1f));
            }

            // Build persons, distributing them across households.
            // Residents 1–50: household index = (personIndex % TotalHouseholds)
            // gives 3 persons to first 2 households and 4 to the rest for a total of 50.
            // More precisely: 50 / 16 = 3 remainder 2, so households 0 and 1 get 4 members,
            // households 2–15 get 3 members.
            // We use round-robin: person i → household (i % TotalHouseholds).
            var householdMembers = new List<List<EntityId>>(TotalHouseholds);
            for (var i = 0; i < TotalHouseholds; i++)
            {
                householdMembers.Add(new List<EntityId>());
            }

            var allTraitKinds = (PersonTraitKind[])Enum.GetValues(typeof(PersonTraitKind));
            var persons = new List<PersonRecord>(TotalResidents);

            for (var p = 0; p < TotalResidents; p++)
            {
                var personId    = new EntityId(PersonIdBase + p + 1);
                var hIndex      = p % TotalHouseholds;
                var household   = households[hIndex];
                var traitKind   = allTraitKinds[rng.NextInt(0, allTraitKinds.Length)];
                var trait       = new PersonTrait(traitKind);
                var schedule    = DailySchedule.Standard(trait, rng);

                var needs = new[]
                {
                    new NeedState(NeedKind.Hunger,  1f),
                    new NeedState(NeedKind.Rest,    1f),
                    new NeedState(NeedKind.Social,  1f),
                    new NeedState(NeedKind.Comfort, 1f),
                };

                persons.Add(new PersonRecord(
                    id:              personId,
                    householdId:     household.Id,
                    homeRoomId:      household.HomeRoomId,
                    workplaceRoomId: workplaceRoomId,
                    schedule:        schedule,
                    needs:           needs,
                    traits:          new[] { trait }));

                householdMembers[hIndex].Add(personId);
            }

            // Reconstruct households with their member lists.
            var finalHouseholds = new List<HouseholdRecord>(TotalHouseholds);
            for (var i = 0; i < TotalHouseholds; i++)
            {
                var src = households[i];
                finalHouseholds.Add(new HouseholdRecord(
                    src.Id,
                    householdMembers[i],
                    src.HomeRoomId,
                    src.Budget,
                    src.Satisfaction));
            }

            return new PopulationState(persons, finalHouseholds);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<EntityId> CollectResidentialRooms(BuildingTopology topology)
        {
            var rooms = new List<EntityId>();
            for (var floor = 1; floor <= FloorsWithApartments; floor++)
            {
                if (!topology.TryGetFloor(floor, out var floorTopology))
                {
                    continue;
                }

                foreach (var room in floorTopology.Rooms)
                {
                    if (room.ContentType == FiveFloorTopologyFixture.ResidentialContentId)
                    {
                        rooms.Add(room.Id);
                    }
                }
            }

            return rooms;
        }

        private static EntityId FindDinerRoomId(BuildingTopology topology)
        {
            if (!topology.TryGetFloor(0, out var groundFloor))
            {
                throw new InvalidOperationException("FiftyResidentFixture: Floor 0 not found in topology.");
            }

            foreach (var room in groundFloor.Rooms)
            {
                if (room.ContentType == FiveFloorTopologyFixture.CommercialContentId)
                {
                    return room.Id;
                }
            }

            throw new InvalidOperationException("FiftyResidentFixture: No commercial diner room found on Floor 0.");
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
