using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ResidentPurposeTests
    {
        [Test]
        public void StandardScheduleHasExactlyEightHourWorkBlock()
        {
            var schedule = DailySchedule.Standard(new PersonTrait(PersonTraitKind.NightOwl),
                new DeterministicRandomStream(44));
            Assert.That(schedule.Blocks[1].DurationTicks, Is.EqualTo(480));
            var sample = FiftyResidentFixture.Create().Persons[0];
            var person = new PersonRecord(new EntityId(99100), sample.HouseholdId, sample.HomeRoomId,
                sample.WorkplaceRoomId, schedule, sample.Needs, sample.Traits);
            var restored = PopulationState.FromSaveData(new PopulationState(new[] { person }, null).ToSaveData()).Persons[0];
            Assert.That(restored.Schedule.Blocks[1], Is.EqualTo(schedule.Blocks[1]));
        }

        [Test]
        public void OutsideWorkerRemainsAwayForEightHoursAfterArrivalAcrossSaveLoad()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(99001), new EntityId(99002), sample.HomeRoomId,
                default, sample.Schedule, sample.Needs, sample.Traits, worksOutside: true);
            worker.UpdateLocation(WorldLocation.Outside);
            worker.UpdateActivity(ActivityKind.Working);
            worker.CommitPurpose(ResidentPurposeKind.WorkingOutside, new Tick(100), 480);
            worker.UpdateNeed(NeedKind.Hunger, 0f);
            var population = new PopulationState(new[] { worker }, null);
            var generator = NewGenerator(topology);

            for (var tick = 101; tick <= 300; tick++)
                Assert.That(generator.GenerateTripsForTick(new Tick(tick - 1), new Tick(tick), population), Is.Empty);

            population = PopulationState.FromSaveData(population.ToSaveData());
            worker = population.Persons[0];
            generator = NewGenerator(topology);
            Assert.That(worker.PurposeEndsAtTick, Is.EqualTo(580));
            for (var tick = 301; tick < 580; tick++)
                Assert.That(generator.GenerateTripsForTick(new Tick(tick - 1), new Tick(tick), population), Is.Empty);

            var trips = generator.GenerateTripsForTick(new Tick(579), new Tick(580), population);
            Assert.That(trips.Count, Is.EqualTo(1));
            Assert.That(trips[0].Purpose, Is.EqualTo(TripPurpose.Home));
            Assert.That(trips[0].Origin.IsOutside, Is.True);
        }

        [Test]
        public void MealAndHomePurposeHoldForMinimumTimeAfterArrival()
        {
            var sample = FiftyResidentFixture.Create().Persons[0];
            var schedule = DailySchedule.Standard(sample.Traits[0], new DeterministicRandomStream(42));
            var person = new PersonRecord(new EntityId(99011), sample.HouseholdId, sample.HomeRoomId,
                sample.WorkplaceRoomId, schedule, sample.Needs, sample.Traits);
            person.UpdateActivity(ActivityKind.Eating);
            ResidentPurposeSystem.AdvancePerson(person, new Tick(100));
            Assert.That(person.CurrentPurpose, Is.EqualTo(ResidentPurposeKind.EatingAtDiner));
            Assert.That(person.PurposeEndsAtTick, Is.EqualTo(130));
            person.UpdateNeed(NeedKind.Hunger, 1f);
            var arbitrator = new DynamicScheduleArbitrator();
            Assert.That(arbitrator.ArbitrateDestination(person, new Tick(129), false), Is.Null);

            person.UpdateActivity(ActivityKind.Leisure);
            ResidentPurposeSystem.AdvancePerson(person, new Tick(200));
            Assert.That(person.CurrentPurpose == ResidentPurposeKind.Sitting ||
                        person.CurrentPurpose == ResidentPurposeKind.Reading ||
                        person.CurrentPurpose == ResidentPurposeKind.Learning ||
                        person.CurrentPurpose == ResidentPurposeKind.Chilling, Is.True);
            Assert.That(person.PurposeEndsAtTick, Is.EqualTo(260));
            var restored = PopulationState.FromSaveData(new PopulationState(new[] { person }, null).ToSaveData()).Persons[0];
            Assert.That(restored.CurrentPurpose, Is.EqualTo(person.CurrentPurpose));
            Assert.That(restored.PurposeEndsAtTick, Is.EqualTo(260));
        }

        private static ScheduleTripGenerator NewGenerator(BuildingTopologyState topology) =>
            new ScheduleTripGenerator(topology.ToSnapshot(), topology.TransitGraph,
                new TransitRoutePlanner(topology.TransitGraph));
    }
}
