using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
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

        [Test]
        public void ExportAndRestore_ExpandedGroundSlab_PreservesWidenedBounds()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            Assert.That(sim.Topology.TryGetFloorSlab(0, out var original), Is.True);

            var expandResult = sim.ExpandGroundSlab(
                new ExpandGroundSlabCommand(original.MinX - 6, original.MaxX));
            Assert.That(expandResult.Accepted, Is.True);

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

            Assert.That(restoredSim.Topology.TryGetFloorSlab(0, out var restored), Is.True);
            Assert.That(restored.MinX, Is.EqualTo(original.MinX - 6));
            Assert.That(restored.MaxX, Is.EqualTo(original.MaxX));
            Assert.That(restoredSim.Economy.CashBalance, Is.EqualTo(sim.Economy.CashBalance));
        }

        [Test]
        public void ExportAndRestore_WornUtilityEquipment_PreservesCondition()
        {
            var topology = new BuildingTopologyState();
            topology.Execute(new BuildFloorSlabCommand(0, 0, 30), new Tick(0));
            topology.Execute(new BuildFloorSlabCommand(1, 0, 30), new Tick(0));
            var sim = new TowerSimulation(
                new SimulationClock(new Tick(0)),
                topology,
                new PopulationState(null, null),
                new ElevatorBank(0, 1, null),
                new TowerEconomyState(sandboxMode: true));
            Assert.That(sim.BuildRoom(new BuildRoomCommand(0, 0, 3, ElectricalGridState.SubstationContentId, 120)).Accepted, Is.True);

            for (var tick = 0; tick < 50; tick++) sim.AdvanceOneTick();
            var original = sim.UtilityOperationsSnapshot().Equipment[0];

            var saveData = sim.ExportSaveData();
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                new Tick(sim.CurrentTick),
                new RandomStreamState(1337, 1337, 50),
                "2026-09-21T00:00:00Z",
                "0.8.2");
            var json = _serializer.Serialize(new SaveEnvelope<TowerSaveData>(metadata, saveData));
            var loadResult = _serializer.Deserialize<TowerSaveData>(json, new SchemaVersion(1));
            Assert.That(loadResult.IsSuccess, Is.True);

            var restoredSim = TowerSimulation.RestoreFromSaveData(loadResult.Value.StatePayload);
            var roundTripped = restoredSim.UtilityOperationsSnapshot().Equipment[0];

            Assert.That(roundTripped.RoomId, Is.EqualTo(original.RoomId));
            Assert.That(roundTripped.Condition, Is.EqualTo(original.Condition).Within(1e-5f));
            Assert.That(roundTripped.IsFailed, Is.EqualTo(original.IsFailed));

            sim.AdvanceOneTick();
            restoredSim.AdvanceOneTick();
            Assert.That(restoredSim.UtilityOperationsSnapshot().Equipment[0].Condition,
                Is.EqualTo(sim.UtilityOperationsSnapshot().Equipment[0].Condition).Within(1e-5f));
        }
    }
}
