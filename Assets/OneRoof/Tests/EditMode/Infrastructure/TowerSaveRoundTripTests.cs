using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Infrastructure.Persistence;

namespace OneRoof.Infrastructure.Tests.EditMode
{
    [TestFixture]
    public sealed class TowerSaveRoundTripTests
    {
        private JsonSaveSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _serializer = new JsonSaveSerializer();
        }

        [Test]
        public void ExportAndRestore_FreshTower_StateMatchesExactly()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            var saveData = sim.ExportSaveData();
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                new Tick(sim.CurrentTick),
                new RandomStreamState(1337, 1337, 0),
                "2026-09-16T15:00:00Z",
                "0.5.1");

            var envelope = new SaveEnvelope<TowerSaveData>(metadata, saveData);
            var json = _serializer.Serialize(envelope);

            Assert.That(string.IsNullOrEmpty(json), Is.False);

            var loadResult = _serializer.Deserialize<TowerSaveData>(json, new SchemaVersion(1));
            Assert.That(loadResult.IsSuccess, Is.True);

            var restoredSim = TowerSimulation.RestoreFromSaveData(loadResult.Value.StatePayload);

            Assert.That(restoredSim.CurrentTick, Is.EqualTo(0));
            Assert.That(restoredSim.ResidentCount, Is.EqualTo(50));
            Assert.That(restoredSim.Topology.FloorCount, Is.EqualTo(5));
            Assert.That(restoredSim.ElevatorBank.Cars.Count, Is.EqualTo(1));
            Assert.That(restoredSim.Economy.CashBalance, Is.EqualTo(50000));
        }

        [Test]
        public void ExportAndRestore_MidCommuteCongestion_PreservesActiveTripsAndQueues()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            // Run through morning commute to build elevator queues and active trips
            for (var tick = 0; tick < 35; tick++)
            {
                sim.AdvanceOneTick();
            }

            var originalActiveTrips = sim.ActiveTripCount;
            var originalQueued = sim.TotalQueuedElevatorPassengers;

            var saveData = sim.ExportSaveData();
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                new Tick(sim.CurrentTick),
                new RandomStreamState(1337, 1337, 35),
                "2026-09-16T15:00:00Z",
                "0.5.1");

            var envelope = new SaveEnvelope<TowerSaveData>(metadata, saveData);
            var json = _serializer.Serialize(envelope);

            var loadResult = _serializer.Deserialize<TowerSaveData>(json, new SchemaVersion(1));
            Assert.That(loadResult.IsSuccess, Is.True);

            var restoredSim = TowerSimulation.RestoreFromSaveData(loadResult.Value.StatePayload);

            Assert.That(restoredSim.CurrentTick, Is.EqualTo(35));
            Assert.That(restoredSim.ActiveTripCount, Is.EqualTo(originalActiveTrips));
            Assert.That(restoredSim.TotalQueuedElevatorPassengers, Is.EqualTo(originalQueued));
            Assert.That(restoredSim.ElevatorBank.Cars[0].CurrentFloor, Is.EqualTo(sim.ElevatorBank.Cars[0].CurrentFloor));
            Assert.That(restoredSim.ElevatorBank.Cars[0].Phase, Is.EqualTo(sim.ElevatorBank.Cars[0].Phase));

            // Both advance identically for another 15 ticks
            for (var tick = 0; tick < 15; tick++)
            {
                sim.AdvanceOneTick();
                restoredSim.AdvanceOneTick();
            }

            Assert.That(restoredSim.CurrentTick, Is.EqualTo(sim.CurrentTick));
            Assert.That(restoredSim.ElevatorBank.DeliveredCount, Is.EqualTo(sim.ElevatorBank.DeliveredCount));
        }

        [Test]
        public void ExportAndRestore_ExpandedFloorsAndRooms_PreservesNewArchitecture()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();

            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -30, 30));
            sim.BuildRoom(new BuildRoomCommand(5, -10, -5, new ContentId("residential:apartment"), 4));
            sim.BuildRoom(new BuildRoomCommand(5, 0, 5, new ContentId("commercial:office"), 8));

            var saveData = sim.ExportSaveData();
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                new Tick(sim.CurrentTick),
                new RandomStreamState(1337, 1337, 0),
                "2026-09-16T15:00:00Z",
                "0.5.1");

            var envelope = new SaveEnvelope<TowerSaveData>(metadata, saveData);
            var json = _serializer.Serialize(envelope);

            var loadResult = _serializer.Deserialize<TowerSaveData>(json, new SchemaVersion(1));
            Assert.That(loadResult.IsSuccess, Is.True);

            var restoredSim = TowerSimulation.RestoreFromSaveData(loadResult.Value.StatePayload);

            Assert.That(restoredSim.Topology.FloorCount, Is.EqualTo(6));
            Assert.That(restoredSim.Topology.HasFloor(5), Is.True);
            Assert.That(restoredSim.Topology.GetRoomsOnFloor(5).Count, Is.EqualTo(2));
        }
    }
}
