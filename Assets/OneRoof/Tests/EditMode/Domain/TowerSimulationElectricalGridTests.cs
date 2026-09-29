using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TowerSimulationElectricalGridTests
    {
        [Test]
        public void ElectricalGridSnapshot_UpdatesAfterBuildCommands()
        {
            var simulation = CreateSimulation();

            Assert.That(simulation.ElectricalGridSnapshot().Floors[0].BrownoutReason, Is.EqualTo(ElectricalBrownoutReason.NoSubstation));

            Assert.That(simulation.BuildRoom(new BuildRoomCommand(0, 10, 11, ElectricalGridState.RiserContentId, 0)).Accepted, Is.True);
            Assert.That(simulation.BuildRoom(new BuildRoomCommand(0, 0, 3, ElectricalGridState.SubstationContentId, 120)).Accepted, Is.True);
            Assert.That(simulation.BuildRoom(new BuildRoomCommand(0, 14, 15, ElectricalGridState.TransformerContentId, 0)).Accepted, Is.True);

            var after = simulation.ElectricalGridSnapshot();
            Assert.That(after.Floors[0].IsBrownout, Is.False);
            Assert.That(after.Floors[1].BrownoutReason, Is.EqualTo(ElectricalBrownoutReason.DisconnectedRiser));
        }

        [Test]
        public void FailedUtilityEquipment_DisconnectsPowerWaterAndWasteService()
        {
            var topology = new BuildingTopologyState();
            topology.Execute(new BuildFloorSlabCommand(0, 0, 30), new Tick(0));
            topology.Execute(new BuildFloorSlabCommand(1, 0, 30), new Tick(0));
            Build(topology, 0, 0, 3, ElectricalGridState.SubstationContentId, 120);
            Build(topology, 0, 4, 5, ElectricalGridState.RiserContentId, 0);
            Build(topology, 1, 4, 5, ElectricalGridState.RiserContentId, 0);
            Build(topology, 0, 6, 7, ElectricalGridState.TransformerContentId, 0);
            Build(topology, 1, 6, 7, ElectricalGridState.TransformerContentId, 0);
            Build(topology, 0, 8, 11, WaterWasteNetworkState.WaterPumpContentId, 100);
            Build(topology, 0, 12, 15, WaterWasteNetworkState.WasteCollectionContentId, 100);
            Build(topology, 0, 16, 17, WaterWasteNetworkState.WaterRiserContentId, 0);
            Build(topology, 1, 16, 17, WaterWasteNetworkState.WaterRiserContentId, 0);
            Build(topology, 0, 18, 19, WaterWasteNetworkState.WasteChuteContentId, 0);
            Build(topology, 1, 18, 19, WaterWasteNetworkState.WasteChuteContentId, 0);
            Build(topology, 0, 20, 25, new ContentId("residential:apartment"), 5);
            Build(topology, 1, 20, 25, new ContentId("residential:apartment"), 5);

            Assert.That(new ElectricalGridState().Evaluate(topology, 0,
                    FailedEquipment(FindRoom(topology, 1, ElectricalGridState.RiserContentId)))
                    .Floors[1].BrownoutReason,
                Is.EqualTo(ElectricalBrownoutReason.DisconnectedRiser));
            Assert.That(new ElectricalGridState().Evaluate(topology, 0,
                    FailedEquipment(FindRoom(topology, 1, ElectricalGridState.TransformerContentId)))
                    .Floors[1].BrownoutReason,
                Is.EqualTo(ElectricalBrownoutReason.MissingTransformer));
            Assert.That(new WaterWasteNetworkState().Evaluate(topology,
                    FailedEquipment(FindRoom(topology, 1, WaterWasteNetworkState.WaterRiserContentId)))
                    .Floors[1].WaterFailure,
                Is.EqualTo(WaterFailureReason.DisconnectedRiser));
            Assert.That(new WaterWasteNetworkState().Evaluate(topology,
                    FailedEquipment(FindRoom(topology, 1, WaterWasteNetworkState.WasteChuteContentId)))
                    .Floors[1].WasteFailure,
                Is.EqualTo(WasteFailureReason.DisconnectedChute));

            var simulation = new TowerSimulation(
                new SimulationClock(new Tick(0)), topology, new PopulationState(null, null),
                new ElevatorBank(0, 1, null), new TowerEconomyState(sandboxMode: true));

            Assert.That(simulation.ElectricalGridSnapshot().Floors[1].IsBrownout, Is.False);
            Assert.That(simulation.WaterWasteNetworkSnapshot().Floors[1].HasWaterService, Is.True);
            Assert.That(simulation.WaterWasteNetworkSnapshot().Floors[1].HasWasteCollection, Is.True);

            for (var tick = 0; tick < 120; tick++) simulation.AdvanceOneTick();

            var equipment = simulation.UtilityOperationsSnapshot().Equipment;
            for (var i = 0; i < equipment.Count; i++) Assert.That(equipment[i].IsFailed, Is.True);
            Assert.That(simulation.ElectricalGridSnapshot().Floors[1].BrownoutReason,
                Is.EqualTo(ElectricalBrownoutReason.NoSubstation));
            var water = simulation.WaterWasteNetworkSnapshot().Floors[1];
            Assert.That(water.WaterFailure, Is.EqualTo(WaterFailureReason.NoGroundPump));
            Assert.That(water.WasteFailure, Is.EqualTo(WasteFailureReason.NoGroundCollection));
        }

        [Test]
        public void BuildRoom_UpperFloorSubstation_IsRejectedAtDomainBoundary()
        {
            var simulation = CreateSimulation();
            var result = simulation.BuildRoom(new BuildRoomCommand(1, 0, 3, new ContentId("utility:electrical_substation"), 120));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("utility:substation_requires_ground")));
        }

        [Test]
        public void BuildRoom_UpperFloorWaterPump_IsRejectedAtDomainBoundary()
        {
            var simulation = CreateSimulation();
            var result = simulation.BuildRoom(new BuildRoomCommand(1, 0, 3, WaterWasteNetworkState.WaterPumpContentId, 120));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("utility:ground_infrastructure_required")));
        }

        private static TowerSimulation CreateSimulation()
        {
            var topology = new BuildingTopologyState();
            topology.Execute(new BuildFloorSlabCommand(0, 0, 30), new Tick(0));
            topology.Execute(new BuildFloorSlabCommand(1, 0, 30), new Tick(0));
            topology.Execute(new BuildRoomCommand(0, 20, 25, new ContentId("residential:apartment"), 5), new Tick(0));
            topology.Execute(new BuildRoomCommand(1, 20, 25, new ContentId("residential:apartment"), 5), new Tick(0));
            return new TowerSimulation(
                new SimulationClock(new Tick(0)),
                topology,
                new PopulationState(null, null),
                new ElevatorBank(0, 1, null),
                new TowerEconomyState(sandboxMode: true));
        }

        private static void Build(BuildingTopologyState topology, int floor, int minX, int maxX, ContentId type, int capacity)
        {
            Assert.That(topology.Execute(new BuildRoomCommand(floor, minX, maxX, type, capacity), new Tick(0)).Accepted, Is.True,
                type.Value);
        }

        private static Room FindRoom(BuildingTopologyState topology, int floor, ContentId contentType)
        {
            foreach (var room in topology.GetRoomsOnFloor(floor))
                if (room.ContentType == contentType) return room;
            Assert.Fail($"Expected {contentType.Value} on floor {floor}.");
            return null;
        }

        private static UtilityOperationsSnapshot FailedEquipment(Room room) =>
            new UtilityOperationsSnapshot(new[]
            {
                new UtilityEquipmentProjection(room.Id.Value, room.Floor, room.ContentType.Value,
                    UtilityOperationsState.FailureThreshold, true)
            });
    }
}
