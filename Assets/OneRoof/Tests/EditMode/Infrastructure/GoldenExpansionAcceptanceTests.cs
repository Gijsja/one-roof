using System;
using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Infrastructure.Persistence;

namespace OneRoof.Infrastructure.Tests.EditMode
{
    /// <summary>
    /// Golden Milestone 5.1 Acceptance Test (Docs/01_GAME_VISION.md & Docs/06_TEST_STRATEGY.md).
    /// Proves the complete vertical city loop:
    ///   1. Five floors and 50 residents running with 1 elevator car.
    ///   2. Player constructs Floor 5 slab, 4 apartments, and extends elevator shaft (costs deducted from treasury).
    ///   3. Autonomous leasing fills the new apartments, expanding population past 50 residents.
    ///   4. Expanded population creates morning commute congestion and elevated wait times.
    ///   5. Player intervenes by adding a 2nd elevator car, reducing average wait time by >40%.
    ///   6. Full simulation state (topology, population, economy, elevator bank, commute) round-trips
    ///      through SaveEnvelope<TowerSaveData> and JsonSaveSerializer with bit-exact replay.
    /// </summary>
    [TestFixture]
    public sealed class GoldenExpansionAcceptanceTests
    {
        private JsonSaveSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _serializer = new JsonSaveSerializer();
        }

        [Test]
        public void GoldenMilestone5_1_FullExpansion_InterventionReducesWaitOver40Percent_AndSaveStateRoundTrips()
        {
            // ── Step 1: Baseline Simulation (5 Floors, 50 Residents, 1 Elevator) ─
            var economy = new TowerEconomyState(initialTreasury: 25000);
            var sim = TowerSimulation.CreateStandardFiveFloor(economy, new DeterministicRandomStream(42));

            Assert.That(sim.Topology.FloorCount, Is.EqualTo(5));
            Assert.That(sim.ResidentCount, Is.EqualTo(50));
            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(1));
            Assert.That(sim.Economy.CashBalance, Is.EqualTo(25000));

            // ── Step 2: Interactive Construction (Expand to Floor 5) ─────────────
            // 2a. Build Floor 5 slab [-14..16] (cost: 31 cells * $100 = $3,100)
            var slabCmd = new BuildFloorSlabCommand(5, -14, 16);
            var slabResult = sim.BuildFloorSlab(slabCmd);
            Assert.That(slabResult.Accepted, Is.True);
            Assert.That(sim.Topology.FloorCount, Is.EqualTo(6));

            // 2b. Build 4 studio apartments on Floor 5 (cost: 4 * 6 cells * $250 = $6,000)
            var apt1Cmd = new BuildRoomCommand(5, -12, -7, new ContentId("residential:studio"), capacity: 5);
            var apt2Cmd = new BuildRoomCommand(5, -6, -1, new ContentId("residential:studio"), capacity: 5);
            var apt3Cmd = new BuildRoomCommand(5, 2, 7, new ContentId("residential:studio"), capacity: 5);
            var apt4Cmd = new BuildRoomCommand(5, 8, 13, new ContentId("residential:studio"), capacity: 5);

            Assert.That(sim.BuildRoom(apt1Cmd).Accepted, Is.True);
            Assert.That(sim.BuildRoom(apt2Cmd).Accepted, Is.True);
            Assert.That(sim.BuildRoom(apt3Cmd).Accepted, Is.True);
            Assert.That(sim.BuildRoom(apt4Cmd).Accepted, Is.True);

            // 2c. Extend elevator shaft to Floor 5 (cost: 6 floors * 2 cells * $500 = $6,000)
            var shaftCmd = new AddElevatorShaftCommand(0, 1, 0, 5);
            Assert.That(sim.AddElevatorShaft(shaftCmd).Accepted, Is.True);

            Assert.That(sim.Economy.CashBalance, Is.LessThan(25000), "Construction costs must be deducted from treasury.");
            Assert.That(sim.Economy.TotalExpenses, Is.GreaterThan(0));

            // ── Step 3: Autonomous Leasing & Demand Inflow ───────────────────────
            // Advance 40 ticks so periodic leasing demand (every 10 ticks) populates Floor 5
            for (var tick = 0; tick < 40; tick++)
            {
                sim.AdvanceOneTick();
            }

            Assert.That(sim.ResidentCount, Is.GreaterThan(50),
                $"Autonomous leasing must move new residents into Floor 5 apartments (found {sim.ResidentCount}).");
            Assert.That(sim.Population.Households.Count, Is.GreaterThan(16),
                "New households must be registered in population state.");

            // ── Step 4 & 5: Commute Congestion & Elevator Capacity Intervention ──
            // Clone/checkpoint state via save data to run parallel 1-car vs 2-car comparison
            var checkpointData = sim.ExportSaveData();

            // Run Baseline (1 car) through commute rush (300 ticks to deliver morning rush)
            var baselineSim = TowerSimulation.RestoreFromSaveData(checkpointData, new DeterministicRandomStream(99));
            for (var tick = 0; tick < 300; tick++)
            {
                baselineSim.AdvanceOneTick();
            }

            var baselineAvgWait = baselineSim.AverageElevatorWaitTicks;
            Assert.That(baselineAvgWait, Is.GreaterThan(0f), "Commute traffic must register non-zero elevator wait time.");

            // Run Intervention (Add 2nd elevator car) through identical commute rush
            var interventionSim = TowerSimulation.RestoreFromSaveData(checkpointData, new DeterministicRandomStream(99));
            interventionSim.AddElevatorCar(capacity: 10, startingFloor: 0);
            Assert.That(interventionSim.ElevatorBank.Cars.Count, Is.EqualTo(2));

            for (var tick = 0; tick < 300; tick++)
            {
                interventionSim.AdvanceOneTick();
            }

            var interventionAvgWait = interventionSim.AverageElevatorWaitTicks;
            var improvement = (baselineAvgWait - interventionAvgWait) / baselineAvgWait;

            Assert.That(improvement, Is.GreaterThanOrEqualTo(0.40f),
                $"Adding a 2nd elevator car must reduce wait time by at least 40% (Baseline: {baselineAvgWait:F2}, Intervention: {interventionAvgWait:F2}, Improvement: {improvement * 100:F1}%).");

            // ── Step 6: Full Save/Load Round-Trip Persistence ───────────────────
            var metadata = new SaveEnvelopeMetadata(
                new SchemaVersion(1),
                new Tick(interventionSim.CurrentTick),
                new RandomStreamState(1337, 42, 100),
                "2026-09-16T12:00:00Z",
                "0.5.1");
            var envelope = new SaveEnvelope<TowerSaveData>(metadata, interventionSim.ExportSaveData());

            var json = _serializer.Serialize(envelope);
            Assert.That(json, Is.Not.Null.And.Not.Empty);

            var loadResult = _serializer.Deserialize<TowerSaveData>(json, new SchemaVersion(1));
            Assert.That(loadResult.IsSuccess, Is.True);
            Assert.That(loadResult.Value, Is.Not.Null);

            var restoredSim = TowerSimulation.RestoreFromSaveData(loadResult.Value.StatePayload, new DeterministicRandomStream(1337));

            // Verify bit-exact fidelity of restored tower
            Assert.That(restoredSim.CurrentTick, Is.EqualTo(interventionSim.CurrentTick));
            Assert.That(restoredSim.Topology.FloorCount, Is.EqualTo(interventionSim.Topology.FloorCount));
            Assert.That(restoredSim.Topology.Rooms.Count, Is.EqualTo(interventionSim.Topology.Rooms.Count));
            Assert.That(restoredSim.ResidentCount, Is.EqualTo(interventionSim.ResidentCount));
            Assert.That(restoredSim.Population.Households.Count, Is.EqualTo(interventionSim.Population.Households.Count));
            Assert.That(restoredSim.ElevatorBank.Cars.Count, Is.EqualTo(interventionSim.ElevatorBank.Cars.Count));
            Assert.That(restoredSim.Economy.CashBalance, Is.EqualTo(interventionSim.Economy.CashBalance));
            Assert.That(restoredSim.Economy.TotalExpenses, Is.EqualTo(interventionSim.Economy.TotalExpenses));

            // Advance both restored and intervention simulations: must behave identically
            var initialDelivered = interventionSim.ElevatorBank.DeliveredCount;
            for (var tick = 0; tick < 20; tick++)
            {
                interventionSim.AdvanceOneTick();
                restoredSim.AdvanceOneTick();
            }

            Assert.That(restoredSim.CurrentTick, Is.EqualTo(interventionSim.CurrentTick));
            Assert.That(restoredSim.ElevatorBank.DeliveredCount, Is.EqualTo(interventionSim.ElevatorBank.DeliveredCount));
            Assert.That(restoredSim.ElevatorBank.DeliveredCount, Is.GreaterThanOrEqualTo(initialDelivered));
        }
    }
}
