using System.Collections.Generic;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Trips
{
    /// <summary>
    /// Convenience factory for trip generation tests.
    /// Assembles the full first-playable domain chain in one call so test fixtures
    /// stay concise and don't repeat boilerplate.
    /// </summary>
    public sealed class TripDemandFixture
    {
        public TripDemandFixture(uint seed = 1)
        {
            Topology    = FiveFloorTopologyFixture.Create();
            Population  = FiftyResidentFixture.Create(Topology, new DeterministicRandomStream(seed));
            Graph       = HierarchicalTransitGraph.FromBuildingTopology(Topology);
            Planner     = new TransitRoutePlanner(Graph);
            Generator   = new ScheduleTripGenerator(Topology, Graph, Planner, firstTripId: 10001);
        }

        public BuildingTopology           Topology   { get; }
        public PopulationState            Population { get; }
        public HierarchicalTransitGraph   Graph      { get; }
        public TransitRoutePlanner        Planner    { get; }
        public ScheduleTripGenerator      Generator  { get; }

        /// <summary>
        /// Returns the first tick at which at least one person in the population
        /// transitions from Sleep to Work (i.e. the earliest Sleep block end tick
        /// across all persons).
        /// </summary>
        public Tick FirstWorkTransitionTick()
        {
            long earliest = long.MaxValue;
            foreach (var person in Population.Persons)
            {
                // Block 0 is Sleep, Block 1 is Work — SleepEndTick == WorkStartTick.
                var workStart = person.Schedule.Blocks[1].StartTick.Value;
                if (workStart < earliest)
                {
                    earliest = workStart;
                }
            }

            return new Tick(earliest);
        }

        /// <summary>
        /// Sweeps ticks [1, <see cref="DailySchedule.TicksPerDay"/>] and returns all
        /// generated Work-purpose trips across the full day cycle for the population.
        /// </summary>
        public IReadOnlyList<TripRecord> CollectAllWorkTrips()
        {
            var result = new List<TripRecord>();
            for (long t = 1; t <= DailySchedule.TicksPerDay; t++)
            {
                var prev    = new Tick(t - 1);
                var current = new Tick(t);
                var trips   = Generator.GenerateTripsForTick(prev, current, Population);
                foreach (var trip in trips)
                {
                    if (trip.Purpose == TripPurpose.Work)
                    {
                        result.Add(trip);
                    }
                }
            }

            return result;
        }
    }
}
