using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class ElectricalGridStateTests
    {
        private static readonly Tick TestTick = new Tick(1);

        [Test]
        public void Evaluate_ContinuousRiserAndTransformers_PowersFloorsWithHeightLoss()
        {
            var topology = CreateTwoFloorElectricalTopology(substationCapacity: 500);
            var snapshot = new ElectricalGridState(riserLossPerFloor: .1f).Evaluate(topology);

            Assert.That(snapshot.Floors[0].Voltage, Is.EqualTo(1f));
            Assert.That(snapshot.Floors[1].Voltage, Is.EqualTo(.9f));
            Assert.That(snapshot.Floors[1].IsBrownout, Is.False);
            Assert.That(snapshot.Floors[1].RiserColumn, Is.EqualTo(10));
        }

        [Test]
        public void Evaluate_GapInRiser_OnlyDisconnectsFloorsAboveGap()
        {
            var topology = CreateTwoFloorElectricalTopology(substationCapacity: 500, includeUpperRiser: false);
            var snapshot = new ElectricalGridState().Evaluate(topology);

            Assert.That(snapshot.Floors[0].IsBrownout, Is.False);
            Assert.That(snapshot.Floors[1].BrownoutReason, Is.EqualTo(ElectricalBrownoutReason.DisconnectedRiser));
            Assert.That(snapshot.Floors[1].Voltage, Is.Zero);
        }

        [Test]
        public void Evaluate_OverloadedSubstation_ReportsVoltageBrownoutForConnectedFloor()
        {
            var topology = CreateTwoFloorElectricalTopology(substationCapacity: 1);
            var snapshot = new ElectricalGridState(riserLossPerFloor: 0f).Evaluate(topology);

            Assert.That(snapshot.IsSubstationOverloaded, Is.True);
            Assert.That(snapshot.Floors[0].BrownoutReason, Is.EqualTo(ElectricalBrownoutReason.InsufficientVoltage));
            Assert.That(snapshot.Floors[1].IsBrownout, Is.True);
        }

        [Test]
        public void Evaluate_MissingTransformer_IdentifiesTheRepairableCause()
        {
            var topology = CreateTwoFloorElectricalTopology(substationCapacity: 500, includeUpperTransformer: false);
            var snapshot = new ElectricalGridState().Evaluate(topology);

            Assert.That(snapshot.Floors[1].BrownoutReason, Is.EqualTo(ElectricalBrownoutReason.MissingTransformer));
        }

        [Test]
        public void Evaluate_OtherSystemUtilityRooms_AddNoElectricalDemand()
        {
            var topology = new BuildingTopologyState();
            topology.Execute(new BuildFloorSlabCommand(0, 0, 30), TestTick);
            Build(topology, 0, 0, 3, ElectricalGridState.SubstationContentId, 120);
            Build(topology, 0, 4, 7, WaterWasteNetworkState.WaterPumpContentId, 120);
            Build(topology, 0, 8, 11, WaterWasteNetworkState.WasteCollectionContentId, 120);
            Build(topology, 0, 20, 25, new ContentId("residential:apartment"), 5);
            var snapshot = new ElectricalGridState().Evaluate(topology);

            Assert.That(snapshot.TotalDemand, Is.EqualTo(2.5f));
            Assert.That(snapshot.IsSubstationOverloaded, Is.False);
        }

        private static BuildingTopologyState CreateTwoFloorElectricalTopology(int substationCapacity, bool includeUpperRiser = true, bool includeUpperTransformer = true)
        {
            var topology = new BuildingTopologyState();
            topology.Execute(new BuildFloorSlabCommand(0, 0, 30), TestTick);
            topology.Execute(new BuildFloorSlabCommand(1, 0, 30), TestTick);
            Build(topology, 0, 0, 3, ElectricalGridState.SubstationContentId, substationCapacity);
            Build(topology, 0, 10, 11, ElectricalGridState.RiserContentId, 0);
            Build(topology, 0, 14, 15, ElectricalGridState.TransformerContentId, 0);
            Build(topology, 0, 20, 25, new ContentId("residential:apartment"), 5);
            if (includeUpperRiser) Build(topology, 1, 10, 11, ElectricalGridState.RiserContentId, 0);
            if (includeUpperTransformer) Build(topology, 1, 14, 15, ElectricalGridState.TransformerContentId, 0);
            Build(topology, 1, 20, 25, new ContentId("residential:apartment"), 5);
            return topology;
        }

        private static void Build(BuildingTopologyState topology, int floor, int minX, int maxX, ContentId type, int capacity)
        {
            var result = topology.Execute(new BuildRoomCommand(floor, minX, maxX, type, capacity), TestTick);
            Assert.That(result.Accepted, Is.True, type.Value);
        }
    }
}
