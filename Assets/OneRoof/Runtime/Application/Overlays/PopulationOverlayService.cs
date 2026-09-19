using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Overlays
{
    /// <summary>
    /// Projects population concentration and demographics without changing simulation state.
    /// Age bands are a stable roster profile derived from resident IDs; resource bands reflect
    /// the household's current normalized budget, not an invented wage value.
    /// </summary>
    public sealed class PopulationOverlayService
    {
        public PopulationOverlayProjection CreateOverlay(TowerSimulationSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var accumulators = new SortedDictionary<int, PopulationAccumulator>();
            for (var floor = 0; floor < session.FloorCount; floor++)
            {
                var capacity = 0;
                foreach (var room in session.Topology.GetRoomsOnFloor(floor)) capacity += room.Capacity;
                accumulators.Add(floor, new PopulationAccumulator(capacity));
            }

            var locations = new Dictionary<int, int>();
            foreach (var resident in session.TransitProjection().Residents) locations[resident.ResidentId] = resident.Floor;
            foreach (var person in session.Population.Persons)
            {
                if (!locations.TryGetValue(person.Id.Value, out var floor) || !accumulators.TryGetValue(floor, out var values)) continue;
                values.ResidentCount++;
                var age = DeriveAge(person.Id.Value);
                if (age < 30) values.YoungAdultCount++;
                else if (age < 50) values.AdultCount++;
                else values.OlderAdultCount++;

                var budget = session.Population.GetHousehold(person.HouseholdId).Budget;
                if (budget < .55f) values.LimitedResourceCount++;
                else if (budget < .8f) values.StableResourceCount++;
                else values.ComfortableResourceCount++;
            }

            var floors = new List<PopulationFloorProjection>(accumulators.Count);
            foreach (var item in accumulators)
            {
                var value = item.Value;
                floors.Add(new PopulationFloorProjection(item.Key, value.ResidentCount, value.Capacity, value.YoungAdultCount, value.AdultCount, value.OlderAdultCount, value.LimitedResourceCount, value.StableResourceCount, value.ComfortableResourceCount));
            }

            return new PopulationOverlayProjection(session.ResidentCount, floors);
        }

        private static int DeriveAge(int residentId) => 18 + (int)Math.Abs(((long)residentId * 17) % 53);

        private sealed class PopulationAccumulator
        {
            public PopulationAccumulator(int capacity) { Capacity = capacity; }
            public int Capacity { get; }
            public int ResidentCount { get; set; }
            public int YoungAdultCount { get; set; }
            public int AdultCount { get; set; }
            public int OlderAdultCount { get; set; }
            public int LimitedResourceCount { get; set; }
            public int StableResourceCount { get; set; }
            public int ComfortableResourceCount { get; set; }
        }
    }
}
