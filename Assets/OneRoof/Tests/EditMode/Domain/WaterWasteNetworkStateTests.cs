using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class WaterWasteNetworkStateTests
    {
        private static readonly Tick TestTick = new Tick(1);

        [Test]
        public void Evaluate_ContinuousRiserAndChute_SuppliesWaterAndCollectsWaste()
        {
            var topology = CreateTopology(floors: 2);
            var snapshot = new WaterWasteNetworkState(pressureLossPerFloor: .1f).Evaluate(topology);

            Assert.That(snapshot.Floors[0].WaterPressure, Is.EqualTo(1f));
            Assert.That(snapshot.Floors[1].WaterPressure, Is.EqualTo(.9f));
            Assert.That(snapshot.Floors[1].WaterFailure, Is.EqualTo(WaterFailureReason.None));
            Assert.That(snapshot.Floors[1].WasteFailure, Is.EqualTo(WasteFailureReason.None));
            Assert.That(snapshot.Floors[1].WaterRiserColumn, Is.EqualTo(10));
            Assert.That(snapshot.Floors[1].WasteChuteColumn, Is.EqualTo(14));
        }

        [Test]
        public void Evaluate_BoosterRestoresPressureAboveTallHead()
        {
            var topology = CreateTopology(floors: 6);
            Build(topology, 4, 4, 5, WaterWasteNetworkState.BoosterPumpContentId, 0);
            var snapshot = new WaterWasteNetworkState(pressureLossPerFloor: .12f).Evaluate(topology);

            Assert.That(snapshot.Floors[5].WaterPressure, Is.EqualTo(.88f).Within(.001f));
            Assert.That(snapshot.Floors[5].WaterFailure, Is.EqualTo(WaterFailureReason.None));
        }

        [Test]
        public void Evaluate_GapInRiserAndChute_ReportsRepairableFailuresAboveGap()
        {
            var topology = CreateTopology(floors: 3, includeMiddleWaterRiser: false, includeMiddleWasteChute: false);
            var snapshot = new WaterWasteNetworkState().Evaluate(topology);

            Assert.That(snapshot.Floors[2].WaterFailure, Is.EqualTo(WaterFailureReason.DisconnectedRiser));
            Assert.That(snapshot.Floors[2].WasteFailure, Is.EqualTo(WasteFailureReason.DisconnectedChute));
        }

        [Test]
        public void Evaluate_MissingGroundEquipment_ReportsSourceFailures()
        {
            var topology = CreateTopology(floors: 2, includePump: false, includeCollector: false);
            var snapshot = new WaterWasteNetworkState().Evaluate(topology);

            Assert.That(snapshot.Floors[1].WaterFailure, Is.EqualTo(WaterFailureReason.NoGroundPump));
            Assert.That(snapshot.Floors[1].WasteFailure, Is.EqualTo(WasteFailureReason.NoGroundCollection));
        }

        private static BuildingTopologyState CreateTopology(int floors, bool includePump = true, bool includeCollector = true, bool includeMiddleWaterRiser = true, bool includeMiddleWasteChute = true)
        {
            var topology = new BuildingTopologyState();
            for (var floor = 0; floor < floors; floor++) topology.Execute(new BuildFloorSlabCommand(floor, 0, 30), TestTick);
            if (includePump) Build(topology, 0, 0, 3, WaterWasteNetworkState.WaterPumpContentId, 500);
            if (includeCollector) Build(topology, 0, 4, 7, WaterWasteNetworkState.WasteCollectionContentId, 500);
            for (var floor = 0; floor < floors; floor++)
            {
                if (includeMiddleWaterRiser || floor != 1) Build(topology, floor, 10, 11, WaterWasteNetworkState.WaterRiserContentId, 0);
                if (includeMiddleWasteChute || floor != 1) Build(topology, floor, 14, 15, WaterWasteNetworkState.WasteChuteContentId, 0);
                Build(topology, floor, 20, 25, new ContentId("residential:apartment"), 5);
            }
            return topology;
        }

        private static void Build(BuildingTopologyState topology, int floor, int minX, int maxX, ContentId type, int capacity)
        {
            var result = topology.Execute(new BuildRoomCommand(floor, minX, maxX, type, capacity), TestTick);
            Assert.That(result.Accepted, Is.True, type.Value);
        }
    }
}
