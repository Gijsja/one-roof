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
        public void BuildRoom_UpperFloorSubstation_IsRejectedAtDomainBoundary()
        {
            var simulation = CreateSimulation();
            var result = simulation.BuildRoom(new BuildRoomCommand(1, 0, 3, new ContentId("utility:electrical_substation"), 120));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejections[0].Code, Is.EqualTo(new ContentId("utility:substation_requires_ground")));
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
    }
}
