using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Population;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class UtilityOperationsStateTests
    {
        [Test]
        public void Advance_UnstaffedEquipment_WearsIntoARepairableFailure()
        {
            var topology = CreateTopology();
            var operations = new UtilityOperationsState();
            for (var tick = 0; tick < 120; tick++) operations.Advance(topology, new PopulationState(null, null));

            var equipment = operations.Snapshot(topology).Equipment[0];
            Assert.That(equipment.IsFailed, Is.True);
            Assert.That(equipment.Condition, Is.LessThanOrEqualTo(UtilityOperationsState.FailureThreshold));
        }

        [Test]
        public void Advance_MaintenanceSpecialist_RepairsFailedEquipmentAutonomously()
        {
            var topology = CreateTopology();
            var operations = new UtilityOperationsState();
            var population = FiftyResidentFixture.Create();
            population.Persons[0].RestoreSpecialization(SpecialistRole.Maintenance, SpecialistRole.None, 0f);
            for (var tick = 0; tick < 120; tick++) operations.Advance(topology, new PopulationState(null, null));
            operations.Advance(topology, population);

            var equipment = operations.Snapshot(topology).Equipment[0];
            Assert.That(equipment.IsFailed, Is.False);
            Assert.That(equipment.Condition, Is.GreaterThan(UtilityOperationsState.FailureThreshold));
        }

        private static BuildingTopologyState CreateTopology()
        {
            var topology = new BuildingTopologyState();
            Assert.That(topology.Execute(new BuildFloorSlabCommand(0, 0, 10), new Tick(0)).Accepted, Is.True);
            Assert.That(topology.Execute(new BuildRoomCommand(0, 0, 3, ElectricalGridState.SubstationContentId, 100), new Tick(0)).Accepted, Is.True);
            return topology;
        }
    }
}
