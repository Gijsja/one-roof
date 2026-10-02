using System;
using NUnit.Framework;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TransitGeneratorPersistenceTests
    {
        [Test]
        public void GeneratorCheckpointPreservesNextIdAndRetryDeadline()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            sim.TripGenerator.RestoreFromSaveData(new TripGeneratorSaveData
            {
                version = 1, nextTripId = 9000,
                retries = new[] { new TripRetrySaveData { personId = sim.Population.Persons[0].Id.Value, retryAfterTick = 9999 } }
            }, 1);
            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            var saved = restored.TripGenerator.ToSaveData();
            Assert.That(saved.nextTripId, Is.EqualTo(9000));
            Assert.That(saved.retries.Length, Is.EqualTo(1));
            Assert.That(saved.retries[0].retryAfterTick, Is.EqualTo(9999));
        }

        [Test]
        public void RandomCheckpointContinuesExactSequence()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            for (var i = 0; i < 111; i++) sim.RandomStream.NextUInt32();
            var restored = TowerSimulation.RestoreFromSaveData(sim.ExportSaveData());
            for (var i = 0; i < 200; i++)
                Assert.That(restored.RandomStream.NextUInt32(), Is.EqualTo(sim.RandomStream.NextUInt32()));
        }

        [Test]
        public void InvalidRandomCheckpointIsRejected()
        {
            var save = TowerSimulation.CreateStandardFiveFloor().ExportSaveData();
            save.simulationRandom.state = 0;
            Assert.That(TowerSimulation.TryRestoreFromSaveData(save, out _, out _), Is.False);
        }

        [Test]
        public void LegacyGeneratorStartsAboveActiveTripIds()
        {
            var sim = GoldStandardCityFixture.Create();
            sim.AdvanceOneTick();
            var save = sim.ExportSaveData();
            save.tripGenerator = null;
            var restored = TowerSimulation.RestoreFromSaveData(save);
            foreach (var trip in save.activeTrips)
                Assert.That(restored.TripGenerator.ToSaveData().nextTripId, Is.GreaterThan(trip.tripId));
        }
    }
}
