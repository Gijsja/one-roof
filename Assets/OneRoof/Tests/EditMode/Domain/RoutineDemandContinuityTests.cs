using System.Linq;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class RoutineDemandContinuityTests
    {
        private static PersonRecord Resident(TowerSimulation sim, string nextLabel, bool outsideWork = false)
        {
            var sample = sim.Population.Persons[0];
            var firstLabel = nextLabel == DailySchedule.LabelSleep ? DailySchedule.LabelWork : DailySchedule.LabelSleep;
            var schedule = DailySchedule.FromBlocks(new[] {
                new ScheduleBlock(firstLabel, new Tick(0), new Tick(10)),
                new ScheduleBlock(nextLabel, new Tick(10), new Tick(1440)) });
            return new PersonRecord(new EntityId(90001), new EntityId(90002), sample.HomeRoomId,
                sample.WorkplaceRoomId, schedule, new[] {
                    new NeedState(NeedKind.Hunger, 1), new NeedState(NeedKind.Energy, 1),
                    new NeedState(NeedKind.Hygiene, 1), new NeedState(NeedKind.Social, 1) }, sample.Traits,
                worksOutside: outsideWork);
        }

        private static void Boundary(TowerSimulation sim, PersonRecord resident)
        {
            var population = new PopulationState(new[] { resident }, null);
            for (var i = 0; i < 3; i++)
                sim.TripGenerator.GenerateTripsForTick(new Tick(9), new Tick(10), population);
        }

        [Test]
        public void WorkBoundaryDuringCommute_IsObservedOnce()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelWork);
            resident.UpdateActivity(ActivityKind.Commuting);
            Boundary(sim, resident);
            var saved = sim.TransitDemand.ToSaveData();
            Assert.That(saved.current.attempted, Is.EqualTo(1));
            Assert.That(saved.current.succeeded, Is.Zero);
            Assert.That(saved.current.failed, Is.Zero);
            Assert.That(saved.episodes.Single().purpose, Is.EqualTo(TripPurpose.Work));
            Assert.That(saved.episodes.Single().episodeStartTick, Is.EqualTo(10));
            Assert.That(saved.episodes.Single().terminal, Is.False);
            Assert.That(sim.TripGenerator.ToSaveData().retries, Is.Empty);
            var arbitrator = new DynamicScheduleArbitrator();
            var decision = arbitrator.Decide(resident, new Tick(10), true);
            Assert.That(decision.Kind, Is.EqualTo(RoutineDecisionKind.Travel));
            Assert.That(decision.IsRequired, Is.True);
            Assert.That(decision.EpisodeStartTick, Is.EqualTo(10));
            Assert.That(arbitrator.ArbitrateDestination(resident, new Tick(10)), Is.Null);
            sim.TransitDemand.Advance(370);
            Assert.That(sim.TransitDemand.Current.failed, Is.EqualTo(1));
        }

        [Test]
        public void HomeBoundaryDuringCommute_IsObservedOnce()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelSleep);
            resident.UpdateLocation(resident.WorkplaceLocation);
            resident.UpdateActivity(ActivityKind.Commuting);
            Boundary(sim, resident);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.Zero);
            Assert.That(sim.TransitDemand.ToSaveData().episodes.Single().terminal, Is.False);
            Assert.That(sim.TripGenerator.ToSaveData().retries, Is.Empty);
        }

        [Test]
        public void NullDecisionAwayFromDestination_IsNotLocalSuccess()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelEat);
            resident.UpdateLocation(resident.WorkplaceLocation);
            resident.UpdateNeed(NeedKind.Hunger, .6f);
            resident.UpdateActivity(ActivityKind.Eating);
            Boundary(sim, resident);
            Assert.That(sim.TransitDemand.Current.succeeded, Is.Zero);
            Assert.That(sim.TransitDemand.Current.attempted, Is.Zero);
        }

        [TestCase(DailySchedule.LabelEat)]
        [TestCase(DailySchedule.LabelLeisure)]
        public void SatisfiedOptionalWindow_DoesNotCreateDemand(string label)
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, label);
            sim.TripGenerator.GenerateTripsForTick(new Tick(9), new Tick(10),
                new PopulationState(new[] { resident }, null), _ => false);
            Boundary(sim, resident);
            var decision = new DynamicScheduleArbitrator().Decide(resident, new Tick(10), true);
            Assert.That(decision.Kind, Is.EqualTo(RoutineDecisionKind.None));
            Assert.That(decision.Purpose, Is.Null);
            Assert.That(decision.IsRequired, Is.False);
            Assert.That(sim.TransitDemand.Current.attempted, Is.Zero);
            Assert.That(sim.TransitDemand.Current.succeeded, Is.Zero);
        }

        [Test]
        public void ExistingSamePurposeTrip_RetainsOriginalEpisode()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelWork);
            long sequence = sim.TransitDemand.ObserveAttempt(resident.Id, TripPurpose.Work, 7, 7);
            sim.TransitDemand.AcceptTrip(sequence, 12345);
            resident.UpdateActivity(ActivityKind.Commuting);
            Boundary(sim, resident);
            var episode = sim.TransitDemand.ToSaveData().episodes.Single();
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(episode.episodeStartTick, Is.EqualTo(7));
            Assert.That(episode.submissionTick, Is.EqualTo(7));
            Assert.That(sim.TransitDemand.FindSequenceForTrip(12345), Is.EqualTo(sequence));
            Assert.That(episode.terminal, Is.False);
            Assert.That(sim.TripGenerator.ToSaveData().retries, Is.Empty);
        }

        [Test]
        public void BlockedOutsideMeal_FailsOnceWithoutLocalSuccessOrNewIdentity()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelEat, outsideWork: true);
            resident.UpdateLocation(WorldLocation.Outside);
            resident.UpdateActivity(ActivityKind.Working);
            resident.CommitPurpose(ResidentPurposeKind.WorkingOutside, new Tick(1), 100);
            resident.UpdateNeed(NeedKind.Hunger, .2f);
            resident.BlockOutsideFoodUntil(50);
            Boundary(sim, resident);
            var saved = sim.TransitDemand.ToSaveData();
            Assert.That(saved.current.attempted, Is.EqualTo(1));
            Assert.That(saved.current.failed, Is.EqualTo(1));
            Assert.That(saved.current.succeeded, Is.Zero);
            Assert.That(saved.episodes.Single().episodeStartTick, Is.EqualTo(10));
            Assert.That(saved.episodes.Single().terminal, Is.True);
            Assert.That(sim.TripGenerator.ToSaveData().retries, Is.Empty);
        }

        [Test]
        public void MissingWorkDestination_FailsUnreachableOnceDuringCooldown()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var sample = Resident(sim, DailySchedule.LabelWork);
            var resident = new PersonRecord(sample.Id, sample.HouseholdId, sample.HomeRoomId,
                new EntityId(999999), sample.Schedule, sample.Needs, sample.Traits);
            Boundary(sim, resident);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.failed, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.unreachable, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.Zero);
            Assert.That(sim.TripGenerator.ToSaveData().retries.Length, Is.EqualTo(1));
        }

        [Test]
        public void DepartedResident_PendingObligationStillFailsAtDeadline()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelWork);
            resident.UpdateActivity(ActivityKind.Commuting);
            Boundary(sim, resident);
            sim.TripGenerator.GenerateTripsForTick(new Tick(10), new Tick(11), new PopulationState(null, null));
            sim.TransitDemand.Advance(369);
            Assert.That(sim.TransitDemand.Current.failed, Is.Zero);
            sim.TransitDemand.Advance(370);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.failed, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.Zero);
        }

        [Test]
        public void CrossedObligation_SaveLoadPreservesIdentityAndInclusive360Completion()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelWork);
            resident.UpdateActivity(ActivityKind.Commuting);
            Boundary(sim, resident);
            var restoredObserver = TransitDemandObserver.FromSaveData(sim.TransitDemand.ToSaveData());
            var generatorSave = sim.TripGenerator.ToSaveData();
            var restored = TowerSimulation.CreateStandardFiveFloor();
            restored.TripGenerator.RestoreFromSaveData(generatorSave, 1);
            restored.TripGenerator.DemandObserver = restoredObserver;
            var population = PopulationState.FromSaveData(new PopulationState(new[] { resident }, null).ToSaveData());
            restored.TripGenerator.GenerateTripsForTick(new Tick(9), new Tick(10), population);
            Assert.That(restoredObserver.Current.attempted, Is.EqualTo(1));
            var returned = population.Persons[0];
            returned.UpdateLocation(returned.WorkplaceLocation);
            returned.UpdateActivity(ActivityKind.Idle);
            var trips = restored.TripGenerator.GenerateTripsForTick(new Tick(369), new Tick(370), population);
            Assert.That(trips, Is.Empty);
            restoredObserver.Advance(370);
            Assert.That(restoredObserver.Current.succeeded, Is.EqualTo(1));
            Assert.That(restoredObserver.Current.failed, Is.Zero);
            Assert.That(restoredObserver.ToSaveData().episodes.Single().submissionTick, Is.EqualTo(10));
            Assert.That(restored.TripGenerator.ToSaveData().retries, Is.Empty);
        }

        [Test]
        public void CriticalHungerSurvivingCommute_IsObservedWhenTravelBecomesEligible()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelWork);
            resident.UpdateNeed(NeedKind.Hunger, .2f);
            resident.UpdateActivity(ActivityKind.Commuting);
            Boundary(sim, resident);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(resident.GetNeedSatisfaction(NeedKind.Hunger), Is.EqualTo(.2f));
            resident.UpdateActivity(ActivityKind.Idle);
            var trips = sim.TripGenerator.GenerateTripsForTick(new Tick(10), new Tick(11),
                new PopulationState(new[] { resident }, null));
            Assert.That(trips.Count, Is.EqualTo(1));
            Assert.That(trips[0].Purpose, Is.EqualTo(TripPurpose.Food));
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(2));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.Zero);
            Assert.That(sim.TransitDemand.Current.failed, Is.Zero);
            Assert.That(sim.TransitDemand.FindSequenceForTrip(trips[0].Id.Value), Is.GreaterThan(0));
        }

        [Test]
        public void RequiredWorkBoundaryDuringCooldown_IsObservedWithoutDuplicateRetry()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, DailySchedule.LabelWork);
            var saved = sim.TripGenerator.ToSaveData();
            saved.retries = new[] { new OneRoof.Domain.Persistence.TripRetrySaveData {
                personId = resident.Id.Value, retryAfterTick = 50 } };
            sim.TripGenerator.RestoreFromSaveData(saved, 1);
            Boundary(sim, resident);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.Zero);
            Assert.That(sim.TransitDemand.Current.failed, Is.Zero);
            Assert.That(sim.TransitDemand.ToSaveData().episodes.Single().terminal, Is.False);
            Assert.That(sim.TripGenerator.ToSaveData().retries.Length, Is.EqualTo(1));
            Assert.That(sim.TripGenerator.ToSaveData().retries[0].retryAfterTick, Is.EqualTo(50));
        }
    }
}
