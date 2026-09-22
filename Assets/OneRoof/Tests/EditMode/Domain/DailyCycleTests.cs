using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    /// <summary>
    /// 24/7 daily-cycle acceptance: overnight recovery, mid-block meal exits,
    /// stranded-resident catch-up across midnight, multi-diner demand spread,
    /// and multi-day simulation stability.
    /// </summary>
    [TestFixture]
    public sealed class DailyCycleTests
    {
        private static EntityId FindDinerId(BuildingTopology topology)
        {
            topology.TryGetFloor(0, out var groundFloor);
            foreach (var room in groundFloor.Rooms)
            {
                if (room.ContentType == FiveFloorTopologyFixture.CommercialContentId)
                {
                    return room.Id;
                }
            }

            throw new System.InvalidOperationException("No diner found on floor 0.");
        }

        private static Tick WorkTick(PersonRecord person) =>
            new Tick(person.Schedule.Blocks[1].StartTick.Value + 1);

        [Test]
        public void SleepingAtHome_RecoversHygieneAcrossNight()
        {
            var topology = FiveFloorTopologyFixture.Create();
            var pop = FiftyResidentFixture.Create(topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];
            var needs = new ResidentNeedsSystem();

            person.UpdateLocation(person.HomeRoomId);
            person.UpdateNeed(NeedKind.Hygiene, 0.4f);
            person.UpdateActivity(ActivityKind.Sleeping);

            for (var i = 0; i < 50; i++)
            {
                needs.AdvancePerson(person);
            }

            Assert.That(person.GetNeedSatisfaction(NeedKind.Hygiene), Is.GreaterThan(0.4f));
        }

        [Test]
        public void SleepingAwayFromHome_DecaysHygiene()
        {
            var topology = FiveFloorTopologyFixture.Create();
            var pop = FiftyResidentFixture.Create(topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];
            var needs = new ResidentNeedsSystem();

            person.UpdateLocation(person.WorkplaceRoomId);
            person.UpdateNeed(NeedKind.Hygiene, 0.8f);
            person.UpdateActivity(ActivityKind.Sleeping);

            needs.AdvancePerson(person);

            Assert.That(person.GetNeedSatisfaction(NeedKind.Hygiene), Is.LessThan(0.8f));
        }

        [Test]
        public void MealComplete_ResumesWorkInPlaceMidBlock()
        {
            var fixture = new TripDemandFixture(seed: 11);
            var person = fixture.Population.Persons[0];
            var arbitrator = new DynamicScheduleArbitrator();

            // The diner doubles as the workplace in the five-floor fixture, so a finished
            // meal during the Work block ends in place with no trip.
            person.UpdateLocation(FindDinerId(fixture.Topology));
            person.UpdateActivity(ActivityKind.Eating);

            var purpose = arbitrator.ArbitrateDestination(person, WorkTick(person), isBlockTransition: false);

            Assert.That(purpose, Is.Null);
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Working));
        }

        [Test]
        public void StrandedWorker_ReturnsHomeMidSleepBlock()
        {
            var fixture = new TripDemandFixture(seed: 12);
            var person = fixture.Population.Persons[0];
            var arbitrator = new DynamicScheduleArbitrator();

            // Late elevator queue: still Working at the diner/office during Sleep block.
            person.UpdateLocation(person.WorkplaceRoomId);
            person.UpdateActivity(ActivityKind.Working);

            var purpose = arbitrator.ArbitrateDestination(person, new Tick(0), isBlockTransition: false);

            Assert.That(person.Schedule.ActiveLabelAt(new Tick(0)), Is.EqualTo(DailySchedule.LabelSleep));
            Assert.That(purpose, Is.EqualTo(TripPurpose.Home));
        }

        [Test]
        public void StrandedSleeper_ReturnsToWorkMidWorkBlock()
        {
            var fixture = new TripDemandFixture(seed: 13);
            var person = fixture.Population.Persons[0];
            var arbitrator = new DynamicScheduleArbitrator();

            // Overslept at home after a late-night arrival; energy comfortable so no urgent override.
            person.UpdateLocation(person.HomeRoomId);
            person.UpdateNeed(NeedKind.Energy, 0.9f);
            person.UpdateActivity(ActivityKind.Sleeping);

            var purpose = arbitrator.ArbitrateDestination(person, WorkTick(person), isBlockTransition: false);

            Assert.That(purpose, Is.EqualTo(TripPurpose.Work));
        }

        [Test]
        public void IdleAtHome_PreservesMidBlockSilence()
        {
            var fixture = new TripDemandFixture(seed: 14);

            var trips = fixture.Generator.GenerateTripsForTick(new Tick(60), new Tick(61), fixture.Population);

            Assert.That(trips.Count, Is.EqualTo(0));
        }

        [Test]
        public void MealComplete_DuringLeisure_RelaxesInPlaceWithoutTrip()
        {
            var fixture = new TripDemandFixture(seed: 16);
            var person = fixture.Population.Persons[0];
            var arbitrator = new DynamicScheduleArbitrator();

            person.UpdateLocation(FindDinerId(fixture.Topology));
            person.UpdateActivity(ActivityKind.Eating);

            var leisureTick = new Tick(person.Schedule.Blocks[3].StartTick.Value + 1);
            Assert.That(person.Schedule.ActiveLabelAt(leisureTick), Is.EqualTo(DailySchedule.LabelLeisure));
            var purpose = arbitrator.ArbitrateDestination(person, leisureTick, isBlockTransition: false);

            Assert.That(purpose, Is.Null);
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Leisure));
        }

        [Test]
        public void HygieneArrivalAtHome_DuringSleepBlock_RestsImmediately()
        {
            var topology = FiveFloorTopologyFixture.Create();
            var pop = FiftyResidentFixture.Create(topology, new DeterministicRandomStream(15));
            var person = pop.Persons[0];

            person.UpdateLocation(person.HomeRoomId);
            person.UpdateActivity(ActivityKind.Idle);

            TransitExecutionSystem.ReconcileArrivalActivity(person, new Tick(0));

            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Sleeping));
        }

        [Test]
        public void SecondDiner_SplitsFoodDemand()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor(new TowerEconomyState(50000));
            var buildCmd = new OneRoof.Domain.Commands.BuildRoomCommand(
                0, -14, -11, new ContentId("commercial:diner"), capacity: 10);
            Assert.That(sim.BuildRoom(buildCmd).Accepted, Is.True);

            foreach (var person in sim.Population.Persons)
            {
                person.UpdateLocation(person.HomeRoomId);
                person.UpdateActivity(ActivityKind.Idle);
                person.UpdateNeed(NeedKind.Hunger, 0.2f);
            }

            var trips = sim.TripGenerator.GenerateTripsForTick(new Tick(60), new Tick(61), sim.Population);
            var destinations = new HashSet<int>();
            foreach (var trip in trips)
            {
                Assert.That(trip.Purpose, Is.EqualTo(TripPurpose.Food));
                destinations.Add(trip.DestinationRoomId.Value);
            }

            Assert.That(trips.Count, Is.EqualTo(50));
            Assert.That(destinations.Count, Is.EqualTo(2), "Food demand should spread across both diners.");
        }

        [Test]
        public void TwoDayCycle_StaysStable()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // End of day 1: baseline delivery volume.
            for (var t = 0; t < DailySchedule.TicksPerDay; t++)
            {
                sim.AdvanceOneTick();
            }
            var dayOneDelivered = sim.ElevatorBank.DeliveredCount;
            Assert.That(dayOneDelivered, Is.GreaterThan(0));

            // Run deep into day 3 (through midnight wrapped twice). A single car cannot
            // drain synchronized demand, so assert liveness and bounded needs instead.
            const int endTick = 3100;
            for (var t = DailySchedule.TicksPerDay; t < endTick; t++)
            {
                sim.AdvanceOneTick();
                if (sim.CurrentTick == 2900)
                {
                    dayOneDelivered = sim.ElevatorBank.DeliveredCount;
                }
            }

            Assert.That(sim.CurrentTick, Is.EqualTo(endTick));
            Assert.That(sim.ElevatorBank.DeliveredCount, Is.GreaterThan(dayOneDelivered),
                "Day 3 must keep completing trips across wrapped midnights.");

            foreach (var person in sim.Population.Persons)
            {
                foreach (var need in person.Needs)
                {
                    Assert.That(need.Satisfaction, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(need.Satisfaction, Is.LessThanOrEqualTo(1f));
                }
            }

            Assert.That(sim.ActiveTripCount, Is.LessThanOrEqualTo(sim.ResidentCount));
        }
    }
}
