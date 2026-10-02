using System;
using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TransitDemandIntegrationTests
    {
        private static PersonRecord Resident(TowerSimulation sim, bool atWork)
        {
            var sample = sim.Population.Persons[0];
            var schedule = DailySchedule.FromBlocks(new[]
            {
                new ScheduleBlock(DailySchedule.LabelSleep, new Tick(0), new Tick(10)),
                new ScheduleBlock(DailySchedule.LabelWork, new Tick(10), new Tick(1440))
            });
            var resident = new PersonRecord(new EntityId(90001), new EntityId(90002), sample.HomeRoomId,
                sample.WorkplaceRoomId, schedule, sample.Needs, sample.Traits);
            if (atWork) resident.UpdateLocation(resident.WorkplaceLocation);
            return resident;
        }

        [Test]
        public void CorruptUnreachableCounterCannotManufactureHealthyNoDemand()
        {
            var observer = new TransitDemandObserver(32);
            var sequence = observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 0, 1);
            observer.RejectUnreachable(sequence, 1);
            var saved = observer.ToSaveData();
            saved.current.unreachable = 0;
            Assert.Throws<ArgumentException>(() => TransitDemandObserver.FromSaveData(saved));
        }

        [Test]
        public void UnreachableDemandCannotBecomeHealthyFromMidnightInactivity()
        {
            var observer = new TransitDemandObserver(32);
            var sequence = observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 0, 1);
            observer.RejectUnreachable(sequence, 1);
            observer.BeginTick(1440);
            observer.Advance(1440);
            observer.MarkObservationComplete();
            Assert.That(observer.Current.attempted, Is.Zero);
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Failed));
            var restored = TransitDemandObserver.FromSaveData(observer.ToSaveData());
            Assert.That(restored.Current.unreachable, Is.EqualTo(1));
            restored.MarkReachable(sequence);
            Assert.That(restored.Current.Status, Is.EqualTo(DemandGateStatus.Passed));
        }

        [Test]
        public void TerminalWaitPercentileIncludesZeroWaitLocalDemand()
        {
            var observer = new TransitDemandObserver(32);
            for (var i = 1; i <= 20; i++)
            {
                var sequence = observer.ObserveAttempt(new EntityId(i), TripPurpose.Home, 0, 0);
                if (i == 20) observer.ObserveQueueWait(sequence, 100);
                observer.Complete(sequence, 100);
            }
            observer.MarkObservationComplete();
            Assert.That(observer.Current.P95Wait, Is.Zero);
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Passed));
        }

        [Test]
        public void LocalDemandCountsWithoutAnActiveTrip()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, true);
            var trips = sim.TripGenerator.GenerateTripsForTick(new Tick(9), new Tick(10),
                new PopulationState(new[] { resident }, null));
            Assert.That(trips.Count, Is.Zero);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.succeeded, Is.EqualTo(1));
        }

        [Test]
        public void SuppressedRequiredTransitionFailsOnceAcrossRepeatedChecks()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var resident = Resident(sim, false);
            var population = new PopulationState(new[] { resident }, null);
            for (var i = 0; i < 3; i++)
                sim.TripGenerator.GenerateTripsForTick(new Tick(9), new Tick(10), population, _ => false);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.failed, Is.EqualTo(1));
        }

        [Test]
        public void MissingRouteDemandIsNotDroppedBeforeSubmission()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var sample = sim.Population.Persons[0];
            var resident = new PersonRecord(new EntityId(90001), new EntityId(90002), sample.HomeRoomId,
                new EntityId(999999), DailySchedule.FromBlocks(new[] {
                    new ScheduleBlock(DailySchedule.LabelSleep, new Tick(0), new Tick(10)),
                    new ScheduleBlock(DailySchedule.LabelWork, new Tick(10), new Tick(1440)) }), sample.Needs, sample.Traits);
            var trips = sim.TripGenerator.GenerateTripsForTick(new Tick(9), new Tick(10),
                new PopulationState(new[] { resident }, null));
            Assert.That(trips.Count, Is.Zero);
            Assert.That(sim.TransitDemand.Current.attempted, Is.EqualTo(1));
            Assert.That(sim.TransitDemand.Current.failed, Is.EqualTo(1));
        }

        [Test]
        public void ReferencePartialDayCannotPassAndCompleteDayPersists()
        {
            var sim = GoldStandardCityFixture.Create();
            for (var i = 0; i < 3300; i++) sim.AdvanceOneTick();
            Assert.That(sim.TransitDemand.LastFinalizedDay, Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.LastTransitDemandDay.attempted, Is.GreaterThan(300));
            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            Assert.That(restored.LastTransitDemandDay.attempted, Is.EqualTo(sim.LastTransitDemandDay.attempted));
            Assert.That(restored.TransitDemand.LastFinalizedDay, Is.EqualTo(sim.TransitDemand.LastFinalizedDay));
        }
    }
}
