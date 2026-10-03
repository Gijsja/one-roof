using System;
using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class EmploymentRoutineTests
    {
        [Test]
        public void ReferenceResidents_HavePreparationMealsAndShiftRelativeSleep()
        {
            var sim = GoldStandardCityFixture.Create();
            foreach (var resident in sim.Population.Persons)
            {
                var blocks = resident.Schedule.Blocks;
                Assert.That(blocks.Any(b => b.Label == "Preparation"), Is.True);
                Assert.That(blocks.Count(b => b.Label == DailySchedule.LabelEat), Is.GreaterThanOrEqualTo(3));
                Assert.That(blocks.Where(b => b.Label == DailySchedule.LabelWork).Sum(b => b.DurationTicks), Is.EqualTo(480));
                Assert.That(blocks.Where(b => b.Label == DailySchedule.LabelSleep).Sum(b => b.DurationTicks), Is.EqualTo(480));
                Assert.That(blocks[0].StartTick.Value, Is.Zero);
                Assert.That(blocks[blocks.Count - 1].EndTick.Value, Is.EqualTo(1440));
                for (var i = 1; i < blocks.Count; i++)
                    Assert.That(blocks[i].StartTick.Value, Is.EqualTo(blocks[i - 1].EndTick.Value));
            }
            Assert.That(sim.Population.Persons.Count(p => p.Schedule.ActiveLabelAt(new Tick(0)) != DailySchedule.LabelSleep), Is.GreaterThan(0));
        }

        [Test]
        public void ReferenceNeeds_AreDerivedFromRoutinePhase()
        {
            var sim = GoldStandardCityFixture.Create();
            Assert.That(sim.Population.Persons.Select(p => p.GetNeedSatisfaction(NeedKind.Hunger)).Distinct().Count(), Is.GreaterThan(3));
            Assert.That(sim.Population.Persons.Select(p => p.GetNeedSatisfaction(NeedKind.Energy)).Distinct().Count(), Is.GreaterThan(3));
        }

        [Test]
        public void RoutineSave_PreservesCohortAndLogicalOvernightBlocks()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2003), new PersonTrait(PersonTraitKind.Frugal), "service:security_station", 123);
            var person = Resident(schedule);
            var restored = PopulationState.FromSaveData(new PopulationState(new[] { person }, null).ToSaveData()).Persons[0];
            Assert.That(restored.Schedule.Cohort, Is.EqualTo(EmploymentCohort.Overnight));
            Assert.That(restored.Schedule.RoutineSeed, Is.EqualTo(schedule.RoutineSeed));
            Assert.That(restored.Schedule.Blocks, Is.EqualTo(schedule.Blocks));
            var before = new DynamicScheduleArbitrator().Decide(person, new Tick(1439), true);
            var after = new DynamicScheduleArbitrator().Decide(person, new Tick(1440), true);
            Assert.That(after.EpisodeStartTick, Is.EqualTo(before.EpisodeStartTick));
        }

        [Test]
        public void LateOvernightArrival_KeepsScheduledEndAcrossMidnight()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2003), new PersonTrait(PersonTraitKind.Frugal), "service:security_station", 123);
            var person = Resident(schedule, true);
            var block = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork && b.StartTick.Value > 1000);
            person.UpdateLocation(WorldLocation.Outside);
            person.UpdateActivity(ActivityKind.Working);
            ResidentPurposeSystem.AdvancePerson(person, new Tick(block.StartTick.Value + 45));
            Assert.That(person.PurposeEndsAtTick, Is.EqualTo(block.AppointmentEndDayTick));
        }

        [Test]
        public void WorkDeparture_AnticipatesPhysicalRouteBeforeAppointment()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, sample: sim.Population.Persons[0]);
            var block = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork);
            var population = new PopulationState(new[] { person }, null);
            var trips = sim.TripGenerator.GenerateTripsForTick(new Tick(block.StartTick.Value - 2), new Tick(block.StartTick.Value - 1), population);
            Assert.That(trips.Count, Is.EqualTo(1));
            Assert.That(trips[0].Purpose, Is.EqualTo(TripPurpose.Work));
            Assert.That(sim.TransitDemand.ToSaveData().episodes.Single().submissionTick, Is.EqualTo(block.StartTick.Value - 1));
        }

        [Test]
        public void OptionalOutings_HaveBoundedDailyVariationWithoutRequiredDemand()
        {
            var decisions = 0;
            for (var id = 2000; id < 2020; id++)
            {
                var schedule = EmploymentRoutineFactory.Create(new EntityId(id), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
                var person = Resident(schedule);
                person.UpdateNeed(NeedKind.Social, .7f);
                var block = schedule.Blocks.First(b => b.Label == DailySchedule.LabelLeisure);
                for (var day = 0; day < 4; day++)
                {
                    var decision = new DynamicScheduleArbitrator().Decide(person, new Tick(day * 1440 + block.StartTick.Value), true);
                    if (decision.Kind != RoutineDecisionKind.Travel) continue;
                    Assert.That(decision.Purpose, Is.EqualTo(TripPurpose.Leisure));
                    Assert.That(decision.IsRequired, Is.False);
                    decisions++;
                }
            }
            Assert.That(decisions, Is.GreaterThan(0));
            Assert.That(decisions, Is.LessThan(80));
        }

        [Test]
        public void LunchReturn_HasNewRequiredDemandInsideOriginalAppointment()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, sample: sim.Population.Persons[0]);
            var work = schedule.Blocks.Where(b => b.Label == DailySchedule.LabelWork).ToArray();
            var pop = new PopulationState(new[] { person }, null);
            person.UpdateLocation(person.WorkplaceLocation);
            sim.TripGenerator.GenerateTripsForTick(new Tick(work[0].StartTick.Value - 1), new Tick(work[0].StartTick.Value), pop);
            Assert.That(sim.TransitDemand.Current.succeeded, Is.EqualTo(1));
            person.UpdateLocation(WorldLocation.Outside);
            person.UpdateActivity(ActivityKind.Commuting);
            sim.TripGenerator.GenerateTripsForTick(new Tick(work[1].StartTick.Value - 1), new Tick(work[1].StartTick.Value), pop);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(2));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.EqualTo(1));
            var episode = sim.TransitDemand.ToSaveData().episodes.Single(e => e.purpose == TripPurpose.Work);
            Assert.That(episode.episodeStartTick, Is.EqualTo(work[1].StartTick.Value));
            Assert.That(episode.terminal, Is.False);
            Assert.That(person.WorkAppointmentEndTick, Is.EqualTo(work[0].AppointmentEndDayTick));
            sim.TransitDemand.Advance(work[1].StartTick.Value + 360);
            Assert.That(sim.TransitDemand.Current.failed, Is.EqualTo(1));
        }

        [Test]
        public void ExpiredArrival_DoesNotCreateFreshWorkObligation()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, true);
            var work = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork);
            person.PrepareWorkAppointment(work, 0, new Tick(work.StartTick.Value));
            person.UpdateLocation(WorldLocation.Outside);
            person.UpdateActivity(ActivityKind.Working);
            ResidentPurposeSystem.AdvancePerson(person, new Tick(work.AppointmentEndDayTick + 1));
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Idle));
            Assert.That(person.CurrentPurpose, Is.EqualTo(ResidentPurposeKind.None));
            Assert.That(person.WorkArrivalLatenessTicks, Is.EqualTo(511));
        }

        [Test]
        public void SleepSplit_PreservesEndAndEpisodeAcrossMidnight()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule);
            person.UpdateActivity(ActivityKind.Sleeping);
            ResidentPurposeSystem.AdvancePerson(person, new Tick(1439));
            var before = new DynamicScheduleArbitrator().Decide(person, new Tick(1439), true);
            var end = person.PurposeEndsAtTick;
            person = PopulationState.FromSaveData(new PopulationState(new[] { person }, null).ToSaveData()).Persons[0];
            ResidentPurposeSystem.AdvancePerson(person, new Tick(1440));
            var after = new DynamicScheduleArbitrator().Decide(person, new Tick(1440), true);
            Assert.That(person.PurposeEndsAtTick, Is.EqualTo(end));
            Assert.That(after.EpisodeStartTick, Is.EqualTo(before.EpisodeStartTick));
        }

        [Test]
        public void LateLunchReturn_KeepsScheduledEndAndReportsLateness()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, true);
            var work = schedule.Blocks.Last(b => b.Label == DailySchedule.LabelWork);
            person.UpdateLocation(WorldLocation.Outside);
            person.UpdateActivity(ActivityKind.Working);
            ResidentPurposeSystem.AdvancePerson(person, new Tick(work.StartTick.Value + 40));
            Assert.That(person.PurposeEndsAtTick, Is.EqualTo(work.AppointmentEndDayTick));
            Assert.That(person.WorkArrivalLatenessTicks, Is.EqualTo(40));
            var decision = new DynamicScheduleArbitrator().Decide(person, new Tick(work.StartTick.Value + 40), true);
            Assert.That(decision.AppointmentStartTick, Is.EqualTo(work.AppointmentStartDayTick));
        }

        [Test]
        public void SatisfiedLunch_StopsWorkUntilReturnAppointment()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, true);
            var work = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork);
            person.UpdateLocation(WorldLocation.Outside);
            person.UpdateActivity(ActivityKind.Working);
            ResidentPurposeSystem.AdvancePerson(person, new Tick(work.StartTick.Value));
            var lunch = schedule.Blocks.First(b => b.Label == DailySchedule.LabelEat && b.StartTick.Value == work.EndTick.Value);
            new DynamicScheduleArbitrator().Decide(person, new Tick(lunch.StartTick.Value), true);
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Idle));
        }

        [Test]
        public void PredepartureAttendance_KeepsSubmissionAndDeadlineAtWorkBoundary()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, sample: sim.Population.Persons[0]);
            var work = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork);
            var pop = new PopulationState(new[] { person }, null);
            sim.TripGenerator.GenerateTripsForTick(new Tick(work.StartTick.Value - 2), new Tick(work.StartTick.Value - 1), pop);
            var departure = person.WorkDepartureTick;
            person.UpdateLocation(person.WorkplaceLocation);
            person.UpdateActivity(ActivityKind.Idle);
            sim.TripGenerator.GenerateTripsForTick(new Tick(work.StartTick.Value - 1), new Tick(work.StartTick.Value), pop);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.EqualTo(1));
            var demand = sim.TransitDemand.ToSaveData().episodes.Single();
            Assert.That(demand.submissionTick, Is.EqualTo(departure));
            Assert.That(demand.episodeStartTick, Is.EqualTo(departure));
            Assert.That(person.PurposeEndsAtTick, Is.EqualTo(work.AppointmentEndDayTick));
        }

        [Test]
        public void PredepartureCheckpoint_PreservesOriginalEpisodeAndFutureAppointment()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, sample: sim.Population.Persons[0]);
            var work = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork);
            var pop = new PopulationState(new[] { person }, null);
            var departure = work.StartTick.Value - 1;
            sim.TripGenerator.GenerateTripsForTick(new Tick(departure - 1), new Tick(departure), pop);
            var restored = PopulationState.FromSaveData(pop.ToSaveData()).Persons[0];
            var before = new DynamicScheduleArbitrator().Decide(person, new Tick(work.StartTick.Value), true);
            var after = new DynamicScheduleArbitrator().Decide(restored, new Tick(work.StartTick.Value), true);
            Assert.That(after.EpisodeStartTick, Is.EqualTo(departure));
            Assert.That(after.AppointmentStartTick, Is.EqualTo(work.StartTick.Value));
            Assert.That(after.AppointmentEndTick, Is.EqualTo(work.AppointmentEndDayTick));
            Assert.That(after.EpisodeStartTick, Is.EqualTo(before.EpisodeStartTick));
            Assert.That(restored.WorkDepartureTick, Is.EqualTo(departure));
        }

        [Test]
        public void FundedLeasing_UsesActualOfficeCohortAndRoutinePhaseNeeds()
        {
            var sim = GoldStandardCityFixture.Create();
            var population = new PopulationState(null, null);
            var next = 95000;
            var residents = new LeasingDemandSystem().EvaluateLeasingDemand(sim.Topology, population, sim.ElevatorBank,
                new DeterministicRandomStream(77), new Tick(450), ref next, sim.Businesses);
            Assert.That(residents.Count, Is.GreaterThan(0));
            foreach (var person in residents)
            {
                Assert.That(person.Schedule.IsEmploymentRoutine, Is.True);
                var content = person.WorkplaceLocation.IsOutside ? null : sim.Topology.Rooms[person.WorkplaceRoomId].ContentType.Value;
                var expected = EmploymentRoutineFactory.Create(person.Id, person.Traits[0], content, person.Schedule.RoutineSeed);
                Assert.That(person.Schedule.Cohort, Is.EqualTo(expected.Cohort));
                Assert.That(person.Schedule.Blocks, Is.EqualTo(expected.Blocks));
                foreach (var need in EmploymentRoutineFactory.InitialNeeds(person.Schedule, new Tick(450)))
                    Assert.That(person.GetNeedSatisfaction(need.Kind), Is.EqualTo(need.Satisfaction));
            }
        }

        [Test]
        public void AuthoredLegacySchedule_IsPreservedWithoutEmploymentRegeneration()
        {
            var schedule = DailySchedule.FromBlocks(new[] { new ScheduleBlock("Custom", new Tick(0), new Tick(1440)) });
            var person = Resident(schedule);
            var save = new PopulationState(new[] { person }, null).ToSaveData();
            save.persons[0].routineVersion = 0;
            var restored = PopulationState.FromSaveData(save).Persons[0];
            Assert.That(restored.Schedule.Blocks, Is.EqualTo(schedule.Blocks));
            Assert.That(restored.Schedule.IsEmploymentRoutine, Is.False);
        }

        [Test]
        public void EmploymentReassignment_UsesSupportedJobCohortsWithoutChangingLegacySchedules()
        {
            var topology = new BuildingTopologyState();
            var office = new Room(new EntityId(131), new ContentId("commercial:office"), new CellBounds(0, 0, 3), Array.Empty<EntityId>(), 2);
            topology.RestoreFromData(new[] { new CellBounds(0, 0, 3) }, new[] { office }, Array.Empty<Portal>());
            var businesses = new BusinessState(new[] { new BusinessRecord(new EntityId(132), office.Id, office.ContentType, cashBalance: 1000) });
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2003), new PersonTrait(PersonTraitKind.Frugal), "service:security_station", 123);
            var person = Resident(schedule, true);
            Assert.That(person.Schedule.Cohort, Is.EqualTo(EmploymentCohort.Overnight));
            var population = new PopulationState(new[] { person }, null);
            Assert.That(new LeasingDemandSystem().MatchExistingOutsideWorkers(topology, population, businesses), Is.EqualTo(1));
            Assert.That(person.Schedule.Cohort, Is.EqualTo(EmploymentCohort.Day));
            Assert.That(person.ReassignToOutsideWork(), Is.True);
            Assert.That(person.Schedule.Cohort, Is.Not.EqualTo(EmploymentCohort.Overnight));
            var legacy = Resident(DailySchedule.Standard(new PersonTrait(PersonTraitKind.Frugal), new DeterministicRandomStream(55)), true);
            var original = legacy.Schedule;
            Assert.That(legacy.ReassignToRoomWork(office.Id), Is.True);
            Assert.That(legacy.Schedule, Is.SameAs(original));
        }

        [Test]
        public void EmploymentChange_PreservesNeedsLocationAndAccruedAppointmentEvidence()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2003), new PersonTrait(PersonTraitKind.Frugal), "service:security_station", 123);
            var person = Resident(schedule);
            var work = schedule.Blocks.Last(b => b.Label == DailySchedule.LabelWork);
            person.PrepareWorkAppointment(work, 0, new Tick(work.StartTick.Value));
            person.RecordWorkDeparture(new Tick(work.StartTick.Value));
            person.RecordWorkArrival(new Tick(work.StartTick.Value + 40));
            person.UpdateNeed(NeedKind.Hunger, .2f);
            var location = person.CurrentLocation;
            var start = person.WorkAppointmentStartTick;
            var end = person.WorkAppointmentEndTick;
            Assert.That(person.ReassignToOutsideWork(), Is.True);
            Assert.That(person.CurrentLocation, Is.EqualTo(location));
            Assert.That(person.GetNeedSatisfaction(NeedKind.Hunger), Is.EqualTo(.2f));
            Assert.That(person.WorkAppointmentStartTick, Is.EqualTo(start));
            Assert.That(person.WorkAppointmentEndTick, Is.EqualTo(end));
            Assert.That(person.WorkArrivalLatenessTicks, Is.EqualTo(40));
            person.UpdateActivity(ActivityKind.Commuting);
            Assert.That(person.ReassignToRoomWork(new EntityId(999)), Is.False);
            Assert.That(person.CurrentActivity, Is.EqualTo(ActivityKind.Commuting));
        }

        [TestCase(1439, true, false)]
        [TestCase(1441, true, false)]
        [TestCase(1400, true, false)]
        [TestCase(900, true, false)]
        [TestCase(1200, false, false)]
        [TestCase(539, false, false)]
        [TestCase(590, false, false)]
        [TestCase(600, false, true)]
        public void RoutineCheckpoint_ReplaysExactFutureDemandActivitiesAndDecisions(long checkpoint, bool overnight, bool urgent)
        {
            var topology = BuildingTopologyState.CreateWithFixture();
            var sample = TowerSimulation.CreateStandardFiveFloor().Population.Persons[0];
            var schedule = EmploymentRoutineFactory.Create(new EntityId(overnight ? 2003 : 2001),
                new PersonTrait(PersonTraitKind.Frugal), overnight ? "service:security_station" : "commercial:office", 123);
            var person = Resident(schedule, true, sample);
            var population = new PopulationState(new[] { person }, new[] {
                new HouseholdRecord(person.HouseholdId, new[] { person.Id }, person.HomeRoomId, .75f, 1f) });
            var sim = new TowerSimulation(new SimulationClock(new Tick(checkpoint - 20)), topology, population,
                new OneRoof.Domain.Transit.ElevatorBank(0, 4, new[] { new OneRoof.Domain.Transit.ElevatorCar(new EntityId(80001), 0, 20) }), randomStream: new DeterministicRandomStream(77));
            for (var i = 0; i < 20; i++) sim.AdvanceOneTick();
            if (urgent) person.UpdateNeed(NeedKind.Hunger, .2f);
            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            for (var i = 0; i < 700; i++)
            {
                sim.AdvanceOneTick(); restored.AdvanceOneTick();
                var original = sim.Population.GetPerson(person.Id);
                var replay = restored.Population.GetPerson(person.Id);
                Assert.That(replay.CurrentActivity, Is.EqualTo(original.CurrentActivity), "activity at " + sim.Clock.CurrentTick.Value);
                Assert.That(replay.CurrentPurpose, Is.EqualTo(original.CurrentPurpose));
                Assert.That(replay.PurposeEndsAtTick, Is.EqualTo(original.PurposeEndsAtTick));
                Assert.That(replay.CurrentLocation, Is.EqualTo(original.CurrentLocation));
                Assert.That(replay.WorkAppointmentEndTick, Is.EqualTo(original.WorkAppointmentEndTick));
                Assert.That(replay.WorkDemandEpisodeStartTick, Is.EqualTo(original.WorkDemandEpisodeStartTick));
                Assert.That(replay.WorkArrivalTick, Is.EqualTo(original.WorkArrivalTick));
                Assert.That(replay.RoutineChoiceDay, Is.EqualTo(original.RoutineChoiceDay));
                Assert.That(replay.RoutineDayChoice, Is.EqualTo(original.RoutineDayChoice));
                foreach (var need in original.Needs) Assert.That(replay.GetNeedSatisfaction(need.Kind), Is.EqualTo(need.Satisfaction));
                var a = new DynamicScheduleArbitrator().Decide(original, sim.Clock.CurrentTick, false);
                var b = new DynamicScheduleArbitrator().Decide(replay, restored.Clock.CurrentTick, false);
                Assert.That(b.Kind, Is.EqualTo(a.Kind)); Assert.That(b.Purpose, Is.EqualTo(a.Purpose));
                Assert.That(b.EpisodeStartTick, Is.EqualTo(a.EpisodeStartTick));
                var x = sim.TransitDemand.ToSaveData(); var y = restored.TransitDemand.ToSaveData();
                Assert.That(y.current.attempted, Is.EqualTo(x.current.attempted));
                Assert.That(y.current.succeeded, Is.EqualTo(x.current.succeeded));
                Assert.That(y.current.failed, Is.EqualTo(x.current.failed));
                Assert.That(y.episodes.Select(e => (e.purpose, e.episodeStartTick, e.submissionTick, e.tripId, e.terminal)),
                    Is.EqualTo(x.episodes.Select(e => (e.purpose, e.episodeStartTick, e.submissionTick, e.tripId, e.terminal))));
                Assert.That(restored.TripGenerator.ToSaveData().nextTripId, Is.EqualTo(sim.TripGenerator.ToSaveData().nextTripId));
            }
        }

        private static PersonRecord Resident(DailySchedule schedule, bool outside = false, PersonRecord sample = null)
        {
            return new PersonRecord(new EntityId(90001), new EntityId(90002), sample?.HomeRoomId ?? new EntityId(100),
                outside ? default : sample?.WorkplaceRoomId ?? new EntityId(101), schedule,
                new[] { new NeedState(NeedKind.Hunger, 1), new NeedState(NeedKind.Energy, 1),
                    new NeedState(NeedKind.Hygiene, 1), new NeedState(NeedKind.Social, 1) },
                new[] { new PersonTrait(PersonTraitKind.Frugal) }, worksOutside: outside);
        }

        [Test]
        public void NoElevatorBank_AnticipationUsesAvailableStairsRatherThanInfinitePickup()
        {
            var sim = GoldStandardCityFixture.Create();
            var sample = sim.Population.Persons.First(p => !p.WorkplaceLocation.IsOutside &&
                Math.Abs(sim.Topology.Rooms[p.HomeRoomId].Floor - sim.Topology.Rooms[p.WorkplaceRoomId].Floor) > 5);
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule, sample: sample);
            var bank = new OneRoof.Domain.Transit.ElevatorBank(0, 29, Array.Empty<OneRoof.Domain.Transit.ElevatorCar>());
            var generator = new ScheduleTripGenerator(sim.Topology.ToSnapshot(), sim.Topology.TransitGraph,
                new OneRoof.Domain.Transit.TransitRoutePlanner(sim.Topology.TransitGraph), elevatorBank: bank);
            var work = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork);
            var pop = new PopulationState(new[] { person }, null);
            var trips = generator.GenerateTripsForTick(new Tick(work.StartTick.Value - 101), new Tick(work.StartTick.Value - 100), pop);
            Assert.That(trips, Is.Empty);
            Assert.That(person.WorkDepartureTick, Is.EqualTo(-1));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)]
        [TestCase(12)][TestCase(13)][TestCase(14)][TestCase(15)][TestCase(16)][TestCase(17)][TestCase(18)]
        public void CorruptRoutinePayload_IsRejectedBeforePopulationRestoration(int corruption)
        {
            var save = GoldStandardCityFixture.Create().ExportSaveData();
            var p = save.persons[0];
            switch (corruption)
            {
                case 0: p.scheduleEpisodeStartTicks = p.scheduleAppointmentStartTicks = p.scheduleAppointmentEndTicks = null; break;
                case 1: p.employmentCohort = 999; break;
                case 2: p.routineVersion = 2; p.scheduleLabels = null; break;
                case 3: p.workAppointmentStartTick = -100; p.workAppointmentEndTick = -50; p.workSegmentStartTick = -500; p.workDepartureTick = 1000; p.workArrivalTick = 2; break;
                case 4: p.routineChoiceDay = 20; break;
                case 5: p.workAppointmentStartTick = 100; p.workAppointmentEndTick = 610; p.workSegmentStartTick = 100; p.workDemandEpisodeStartTick = 100; p.workDepartureTick = 3000; p.workArrivalTick = 4000; break;
                case 6: p.scheduleAppointmentEndTicks = p.scheduleAppointmentEndTicks.Select(_ => -10000L).ToArray(); break;
                case 7: p.scheduleEpisodeStartTicks = new long[1]; break;
                case 8: p.scheduleLabels = null; break;
                case 9: p.scheduleStartTicks[1]++; break;
                case 10: p.scheduleEndTicks[p.scheduleEndTicks.Length - 1] = 1500; break;
                case 11: p.scheduleEpisodeStartTicks[0] = p.scheduleStartTicks[0] + 1; break;
                case 12: p.scheduleAppointmentStartTicks[0] = p.scheduleEpisodeStartTicks[0] + 1; break;
                case 13: p.workDepartureTick = 100; break;
                case 14: p.workAppointmentStartTick = 100; p.workAppointmentEndTick = 610; p.workSegmentStartTick = 100; p.workDemandEpisodeStartTick = 100; p.workDepartureTick = 150; p.workArrivalTick = 149; break;
                case 15: p.routineChoiceDay = -1; p.routineDayChoice = 2; break;
                case 16: p.scheduleLabels[0] = "UnknownRoutine"; break;
                case 17: p.workAppointmentStartTick = -100; p.workAppointmentEndTick = long.MaxValue; p.workSegmentStartTick = 0; p.workDemandEpisodeStartTick = 0; break;
                case 18: p.workAppointmentStartTick = 100000; p.workAppointmentEndTick = 100510; p.workSegmentStartTick = 100000; p.workDemandEpisodeStartTick = 100; break;
            }
            Assert.Throws<ArgumentException>(() => TowerSimulation.RestoreFromSaveData(save));
        }

        [TestCase(ActivityKind.Eating, 30)]
        [TestCase(ActivityKind.Leisure, 60)]
        public void GeneratedLateMidnightFragment_UsesProductionActivityClock(ActivityKind activity, long duration)
        {
            DailySchedule schedule = null;
            for (var seed = 0U; seed < 200 && schedule == null; seed++)
            {
                var candidate = EmploymentRoutineFactory.Create(new EntityId(2000), new PersonTrait(PersonTraitKind.Frugal), null, seed);
                if (candidate.Cohort == EmploymentCohort.Late && candidate.Blocks[0].Label == DailySchedule.LabelEat && candidate.Blocks[0].DurationTicks < 30)
                    schedule = candidate;
            }
            Assert.That(schedule, Is.Not.Null);
            var resident = Resident(schedule);
            resident.UpdateActivity(activity);
            ResidentPurposeSystem.AdvancePerson(resident, new Tick(1));
            Assert.That(resident.PurposeEndsAtTick - resident.PurposeStartedAtTick, Is.EqualTo(duration));
            var legacy = Resident(DailySchedule.Standard(new PersonTrait(PersonTraitKind.Frugal), new DeterministicRandomStream(44), 15));
            legacy.UpdateActivity(activity);
            ResidentPurposeSystem.AdvancePerson(legacy, new Tick(1));
            Assert.That(legacy.PurposeEndsAtTick - legacy.PurposeStartedAtTick, Is.EqualTo(5));
        }

        [TestCase(-100, -100, 0, 0, 10)]
        [TestCase(535, 535, 440, 440, 450)]
        [TestCase(100, 370, 365, 365, 400)]
        public void ValidRoutineObservations_PreserveMidnightPredepartureAndPriorJobEvidence(
            long start, long segment, long demand, long departure, long arrival)
        {
            var save = GoldStandardCityFixture.Create().ExportSaveData();
            var p = save.persons[0];
            p.routineChoiceDay = 0; p.routineDayChoice = 2;
            p.workAppointmentStartTick = start; p.workAppointmentEndTick = start + 510;
            p.workSegmentStartTick = segment; p.workDemandEpisodeStartTick = demand;
            p.workDepartureTick = departure; p.workArrivalTick = arrival;
            var restored = TowerSimulation.RestoreFromSaveData(save).Population.GetPerson(new EntityId(p.id));
            Assert.That(restored.WorkAppointmentStartTick, Is.EqualTo(start));
            Assert.That(restored.WorkAppointmentEndTick, Is.EqualTo(start + 510));
            Assert.That(restored.WorkSegmentStartTick, Is.EqualTo(segment));
            Assert.That(restored.WorkDemandEpisodeStartTick, Is.EqualTo(demand));
            Assert.That(restored.WorkArrivalTick, Is.EqualTo(arrival));
            Assert.That(restored.WorkArrivalLatenessTicks, Is.EqualTo(Math.Max(0, arrival - segment)));
        }

        [Test]
        public void DirectPopulationRestore_ValidatesStructureWithoutInventingWorldClock()
        {
            var save = GoldStandardCityFixture.Create().ExportSaveData().GetPopulationSaveData();
            var p = save.persons[0];
            p.workAppointmentStartTick = 100; p.workAppointmentEndTick = 610; p.workSegmentStartTick = 100;
            p.workDemandEpisodeStartTick = 100; p.workDepartureTick = 3000; p.workArrivalTick = 4000;
            Assert.That(PopulationState.FromSaveData(save).Persons[0].WorkArrivalTick, Is.EqualTo(4000));
            Assert.Throws<ArgumentException>(() => PopulationState.FromSaveData(save, restoredTick: 450));
        }

        [Test]
        public void LocalAttendance_DoesNotAcquireLaterDepartureForCompletedEpisode()
        {
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), "commercial:office", 123);
            var person = Resident(schedule);
            var work = schedule.Blocks.First(b => b.Label == DailySchedule.LabelWork);
            person.PrepareWorkAppointment(work, 0, new Tick(work.StartTick.Value));
            person.RecordWorkArrival(new Tick(work.StartTick.Value));
            person.RecordWorkDeparture(new Tick(work.StartTick.Value + 120));
            Assert.That(person.WorkDepartureTick, Is.EqualTo(-1));
            Assert.That(person.WorkArrivalTick, Is.EqualTo(work.StartTick.Value));
        }

        [Test]
        public void OutsideRoutineCheckpoint_PreservesLastIndoorRoomAlongsideWorldLocation()
        {
            var sample = TowerSimulation.CreateStandardFiveFloor().Population.Persons[0];
            var schedule = EmploymentRoutineFactory.Create(new EntityId(2001), new PersonTrait(PersonTraitKind.Frugal), null, 123);
            var person = Resident(schedule, true, sample);
            person.UpdateLocation(sample.WorkplaceRoomId);
            person.UpdateLocation(WorldLocation.Outside);
            person.UpdateActivity(ActivityKind.Working);
            var saved = new PopulationState(new[] { person }, null).ToSaveData();
            var restored = PopulationState.FromSaveData(saved);
            Assert.That(restored.Persons[0].CurrentLocation, Is.EqualTo(WorldLocation.Outside));
            Assert.That(restored.ToSaveData().persons[0].currentRoomId, Is.EqualTo(saved.persons[0].currentRoomId));
            Assert.That(restored.Persons[0].CurrentActivity, Is.EqualTo(ActivityKind.Working));
        }

        [Test]
        public void GoldStandardCheckpoint_After90Ticks_RestoresExactSaveFields()
        {
            var first = GoldStandardCityFixture.Create();
            var second = GoldStandardCityFixture.Create();
            for (var tick = 0; tick < 90; tick++) { first.AdvanceOneTick(); second.AdvanceOneTick(); }
            var save = first.ExportSaveData();
            AssertSaveFieldsEqual(save, second.ExportSaveData(), "deterministic seed");
            AssertSaveFieldsEqual(save, TowerSimulation.RestoreFromSaveData(save).ExportSaveData(), "checkpoint restore");
        }

        // Portable equivalent of comparing the complete JsonUtility snapshot: every public saved field,
        // including nested arrays/DTOs, is checked without introducing a Unity serializer dependency.
        private static void AssertSaveFieldsEqual(object expected, object actual, string path)
        {
            if (expected == null) { Assert.That(actual, Is.Null, path); return; }
            Assert.That(actual, Is.Not.Null, path);
            if (expected.GetType().IsValueType || expected is string) { Assert.That(actual, Is.EqualTo(expected), path); return; }
            if (expected is Array expectedArray)
            {
                var actualArray = (Array)actual;
                Assert.That(actualArray.Length, Is.EqualTo(expectedArray.Length), path);
                for (var i = 0; i < expectedArray.Length; i++)
                    AssertSaveFieldsEqual(expectedArray.GetValue(i), actualArray.GetValue(i), path + "[" + i + "]");
                return;
            }
            foreach (var field in expected.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                AssertSaveFieldsEqual(field.GetValue(expected), field.GetValue(actual), path + "." + field.Name);
        }
    }
}
