using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    [TestFixture]
    public sealed class ResidentNeedsSystemTests
    {
        private BuildingTopology _topology;
        private ResidentNeedsSystem _needsSystem;
        private DynamicScheduleArbitrator _arbitrator;

        [SetUp]
        public void SetUp()
        {
            _topology = FiveFloorTopologyFixture.Create();
            _needsSystem = new ResidentNeedsSystem();
            _arbitrator = new DynamicScheduleArbitrator();
        }

        [Test]
        public void FiftyResidentFixture_InitializesAllFiveCoreNeeds()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(42));
            var person = pop.Persons[0];

            Assert.That(person.HasNeed(NeedKind.Hunger), Is.True);
            Assert.That(person.HasNeed(NeedKind.Energy), Is.True);
            Assert.That(person.HasNeed(NeedKind.Social), Is.True);
            Assert.That(person.HasNeed(NeedKind.Hygiene), Is.True);
            Assert.That(person.HasNeed(NeedKind.Purpose), Is.True);

            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Energy), Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Social), Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Hygiene), Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Purpose), Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void Sleeping_RestoresEnergy_AndDecaysHunger()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            person.UpdateNeed(NeedKind.Energy, 0.4f);
            person.UpdateNeed(NeedKind.Hunger, 0.8f);
            person.UpdateActivity(ActivityKind.Sleeping);

            _needsSystem.AdvancePerson(person);

            Assert.That(person.GetNeedSatisfaction(NeedKind.Energy), Is.GreaterThan(0.4f));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.LessThan(0.8f));
        }

        [Test]
        public void Eating_RestoresHunger()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            person.UpdateNeed(NeedKind.Hunger, 0.3f);
            person.UpdateActivity(ActivityKind.Eating);

            _needsSystem.AdvancePerson(person);

            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.GreaterThan(0.3f));
        }

        [Test]
        public void Working_RestoresPurpose_AndDepletesEnergyAndHunger()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            person.UpdateNeed(NeedKind.Purpose, 0.5f);
            person.UpdateNeed(NeedKind.Energy, 0.9f);
            person.UpdateNeed(NeedKind.Hunger, 0.9f);
            person.UpdateActivity(ActivityKind.Working);

            _needsSystem.AdvancePerson(person);

            Assert.That(person.GetNeedSatisfaction(NeedKind.Purpose), Is.GreaterThan(0.5f));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Energy), Is.LessThan(0.9f));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.LessThan(0.9f));
        }

        [Test]
        public void Leisure_RestoresSocial()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            person.UpdateNeed(NeedKind.Social, 0.3f);
            person.UpdateActivity(ActivityKind.Leisure);

            _needsSystem.AdvancePerson(person);

            Assert.That(person.GetNeedSatisfaction(NeedKind.Social), Is.GreaterThan(0.3f));
        }

        [Test]
        public void IdleAtHome_RestoresHygieneIfLow()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            person.UpdateLocation(person.HomeRoomId);
            person.UpdateNeed(NeedKind.Hygiene, 0.4f);
            person.UpdateActivity(ActivityKind.Idle);

            _needsSystem.AdvancePerson(person);

            Assert.That(person.GetNeedSatisfaction(NeedKind.Hygiene), Is.GreaterThan(0.4f));
        }

        [Test]
        public void IntrovertDecaysSocialMoreSlowlyThanExtrovert()
        {
            var schedule = DailySchedule.FromBlocks(new[]
            {
                new ScheduleBlock("Work", new Tick(0), new Tick(1000))
            });

            var introvertPerson = new PersonRecord(
                new EntityId(901),
                new EntityId(101),
                new EntityId(201),
                new EntityId(301),
                schedule,
                new[] { new NeedState(NeedKind.Social, 1.0f) },
                new[] { new PersonTrait(PersonTraitKind.Introvert) });

            var extrovertPerson = new PersonRecord(
                new EntityId(902),
                new EntityId(101),
                new EntityId(201),
                new EntityId(301),
                schedule,
                new[] { new NeedState(NeedKind.Social, 1.0f) },
                new[] { new PersonTrait(PersonTraitKind.Extrovert) });

            introvertPerson.UpdateActivity(ActivityKind.Working);
            extrovertPerson.UpdateActivity(ActivityKind.Working);

            for (var i = 0; i < 20; i++)
            {
                _needsSystem.AdvancePerson(introvertPerson);
                _needsSystem.AdvancePerson(extrovertPerson);
            }

            Assert.That(introvertPerson.GetNeedSatisfaction(NeedKind.Social),
                Is.GreaterThan(extrovertPerson.GetNeedSatisfaction(NeedKind.Social)));
        }

        [Test]
        public void Arbitrator_CriticalExhaustion_OverridesScheduleWithHomeTrip()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            // Person is at workplace during work hours
            person.UpdateLocation(person.WorkplaceRoomId);
            person.UpdateActivity(ActivityKind.Working);

            // Severely exhausted
            person.UpdateNeed(NeedKind.Energy, 0.15f);

            // Tick during work hours
            var workTick = new Tick(500);
            var purpose = _arbitrator.ArbitrateDestination(person, workTick);

            Assert.That(purpose, Is.EqualTo(TripPurpose.Home));
        }

        [Test]
        public void Arbitrator_CriticalHunger_OverridesScheduleWithFoodTrip()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            // Person is at home during leisure hours
            person.UpdateLocation(person.HomeRoomId);
            person.UpdateActivity(ActivityKind.Idle);

            // Severely hungry
            person.UpdateNeed(NeedKind.Hunger, 0.20f);

            var leisureTick = new Tick(1200);
            var purpose = _arbitrator.ArbitrateDestination(person, leisureTick);

            Assert.That(purpose, Is.EqualTo(TripPurpose.Food));
        }

        [Test]
        public void Arbitrator_CriticalHygiene_ReturnsHomeToFreshenUp()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            // Person is away from home
            person.UpdateLocation(person.WorkplaceRoomId);
            person.UpdateActivity(ActivityKind.Idle);

            // Severely low hygiene
            person.UpdateNeed(NeedKind.Hygiene, 0.15f);

            var leisureTick = new Tick(1200);
            var purpose = _arbitrator.ArbitrateDestination(person, leisureTick);

            Assert.That(purpose, Is.EqualTo(TripPurpose.Hygiene));
        }

        [Test]
        public void Arbitrator_ComfortableNeeds_FollowsSchedule()
        {
            var pop = FiftyResidentFixture.Create(_topology, new DeterministicRandomStream(1));
            var person = pop.Persons[0];

            person.UpdateLocation(person.HomeRoomId);
            person.UpdateActivity(ActivityKind.Idle);

            // All needs comfortable (1.0f)
            // Schedule block is Work at tick 30 (work block runs ~15 to ~95)
            var workTick = new Tick(30);
            var purpose = _arbitrator.ArbitrateDestination(person, workTick);

            Assert.That(purpose, Is.EqualTo(TripPurpose.Work));
        }
    }
}
