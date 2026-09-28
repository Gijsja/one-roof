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
            worker.UpdateNeed(NeedKind.Hunger, 1f);
            worker.UpdateNeed(NeedKind.Energy, 1f);
            worker.UpdateNeed(NeedKind.Hygiene, 1f);
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
        public void CriticalHungerInterruptsCommittedOutsideShiftForFood()
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(99003), new EntityId(99004), sample.HomeRoomId,
                default, sample.Schedule, sample.Needs, sample.Traits, worksOutside: true);
            worker.UpdateLocation(WorldLocation.Outside);
            worker.UpdateActivity(ActivityKind.Working);
            worker.CommitPurpose(ResidentPurposeKind.WorkingOutside, new Tick(100), 480);
            worker.UpdateNeed(NeedKind.Hunger, 0.2f);
            worker.UpdateNeed(NeedKind.Energy, 1f);
            worker.UpdateNeed(NeedKind.Hygiene, 1f);
            var generator = NewGenerator(topology);

            var trips = generator.GenerateTripsForTick(new Tick(100), new Tick(101),
                new PopulationState(new[] { worker }, null));

            Assert.That(trips.Count, Is.EqualTo(1));
            Assert.That(trips[0].Purpose, Is.EqualTo(TripPurpose.Food));
            Assert.That(trips[0].Destination.IsOutside, Is.True);
        }

        [Test]
        public void OutsideFoodRetryBlockSurvivesPopulationSaveLoad()
        {
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(99005), new EntityId(99006), sample.HomeRoomId,
                default, sample.Schedule, sample.Needs, sample.Traits, worksOutside: true);
            worker.BlockOutsideFoodUntil(500);

            var restored = PopulationState.FromSaveData(new PopulationState(new[] { worker }, null).ToSaveData()).Persons[0];

            Assert.That(restored.OutsideFoodRetryAfterTick, Is.EqualTo(500));
            Assert.That(restored.IsOutsideFoodRetryBlocked(new Tick(499)), Is.True);
            Assert.That(restored.IsOutsideFoodRetryBlocked(new Tick(500)), Is.False);
        }

        [Test]
        public void ReassignToOutsideWork_PreservesIdentityAndHomeAcrossSaveLoad()
        {
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(99007), new EntityId(99008), sample.HomeRoomId,
                sample.WorkplaceRoomId, sample.Schedule, sample.Needs, sample.Traits);
            worker.UpdateLocation(sample.WorkplaceRoomId);
            worker.UpdateActivity(ActivityKind.Working);
            worker.CommitPurpose(ResidentPurposeKind.WorkingInside, new Tick(100), 480);

            Assert.That(worker.ReassignToOutsideWork(), Is.True);
            Assert.That(worker.Id, Is.EqualTo(new EntityId(99007)));
            Assert.That(worker.HouseholdId, Is.EqualTo(new EntityId(99008)));
            Assert.That(worker.HomeRoomId, Is.EqualTo(sample.HomeRoomId));
            Assert.That(worker.WorkplaceRoomId.IsValid, Is.False);
            Assert.That(worker.WorkplaceLocation.IsOutside, Is.True);
            Assert.That(worker.CurrentRoomId, Is.EqualTo(sample.WorkplaceRoomId),
                "Reassignment changes employment, not the resident's physical location.");
            Assert.That(worker.CurrentActivity, Is.EqualTo(ActivityKind.Idle));
            Assert.That(worker.CurrentPurpose, Is.EqualTo(ResidentPurposeKind.None));

            var restored = PopulationState.FromSaveData(new PopulationState(new[] { worker }, null).ToSaveData()).Persons[0];
            Assert.That(restored.WorkplaceLocation.IsOutside, Is.True);
            Assert.That(restored.WorkplaceRoomId.IsValid, Is.False);
            Assert.That(restored.HomeRoomId, Is.EqualTo(sample.HomeRoomId));
        }

        [Test]
        public void ReassignToOutsideWork_WaitsForActiveCommute()
        {
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(99009), new EntityId(99010), sample.HomeRoomId,
                sample.WorkplaceRoomId, sample.Schedule, sample.Needs, sample.Traits);
            worker.UpdateActivity(ActivityKind.Commuting);

            Assert.That(worker.ReassignToOutsideWork(), Is.False);
            Assert.That(worker.WorkplaceRoomId, Is.EqualTo(sample.WorkplaceRoomId));
            Assert.That(worker.WorkplaceLocation.IsOutside, Is.False);
        }

        [Test]
        public void ReassignToRoomWork_PreservesHomeAndPhysicalLocationAcrossSaveLoad()
        {
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(99012), new EntityId(99013), sample.HomeRoomId,
                default, sample.Schedule, sample.Needs, sample.Traits, worksOutside: true);
            worker.UpdateLocation(WorldLocation.Outside);
            worker.UpdateActivity(ActivityKind.Working);
            worker.CommitPurpose(ResidentPurposeKind.WorkingOutside, new Tick(100), 480);

            Assert.That(worker.ReassignToRoomWork(sample.WorkplaceRoomId), Is.True);
            Assert.That(worker.WorkplaceRoomId, Is.EqualTo(sample.WorkplaceRoomId));
            Assert.That(worker.WorkplaceLocation, Is.EqualTo(WorldLocation.InRoom(sample.WorkplaceRoomId)));
            Assert.That(worker.CurrentLocation.IsOutside, Is.True);
            Assert.That(worker.HomeRoomId, Is.EqualTo(sample.HomeRoomId));
            Assert.That(worker.CurrentPurpose, Is.EqualTo(ResidentPurposeKind.None));
            Assert.That(worker.CurrentActivity, Is.EqualTo(ActivityKind.Idle));

            var restored = PopulationState.FromSaveData(new PopulationState(new[] { worker }, null).ToSaveData()).Persons[0];
            Assert.That(restored.WorkplaceRoomId, Is.EqualTo(sample.WorkplaceRoomId));
            Assert.That(restored.WorkplaceLocation.IsOutside, Is.False);
            Assert.That(restored.CurrentLocation.IsOutside, Is.True);
        }

        [Test]
        public void ReassignToRoomWork_DoesNotRedirectActiveCommute()
        {
            var sample = FiftyResidentFixture.Create().Persons[0];
            var worker = new PersonRecord(new EntityId(99014), new EntityId(99015), sample.HomeRoomId,
                default, sample.Schedule, sample.Needs, sample.Traits, worksOutside: true);
            worker.UpdateActivity(ActivityKind.Commuting);

            Assert.That(worker.ReassignToRoomWork(sample.WorkplaceRoomId), Is.False);
            Assert.That(worker.WorkplaceRoomId.IsValid, Is.False);
            Assert.That(worker.WorkplaceLocation.IsOutside, Is.True);
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
