using NUnit.Framework;
using OneRoof.Domain;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class SpecialistRoleSystemTests
    {
        [Test]
        public void Capacity_StandardTowerAndOffice_ExposesServiceAndKnowledgeTrainingSlots()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            sim.BuildFloorSlab(new BuildFloorSlabCommand(5, -14, 17));
            var result = sim.BuildRoom(new BuildRoomCommand(5, 5, 12, new ContentId("commercial:office"), 8));
            Assert.That(result.Accepted, Is.True);

            var capacity = SpecialistTrainingCapacity.FromTopology(sim.Topology);

            Assert.That(capacity.ServiceSlots, Is.GreaterThan(0), "The existing diner should teach service roles.");
            Assert.That(capacity.KnowledgeSlots, Is.GreaterThan(0), "Office capacity should teach knowledge roles.");
            Assert.That(capacity.MaintenanceSlots, Is.EqualTo(0), "Future maintenance space must be built before it can train residents.");
        }

        [Test]
        public void Advance_DinerCapacity_AutonomouslyTrainsOnlyAvailableServiceSlots()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var system = sim.Specialists;

            system.Advance(sim.Population, sim.Topology, new Tick(1));
            system.Advance(sim.Population, sim.Topology, new Tick(2));

            var assigned = 0;
            for (var i = 0; i < sim.Population.Persons.Count; i++)
                if (sim.Population.Persons[i].Specialization.Role == SpecialistRole.Service) assigned++;

            Assert.That(assigned, Is.EqualTo(system.Capacity.ServiceSlots));
            Assert.That(system.ServiceEfficiencyMultiplier, Is.GreaterThan(1f));
            Assert.That(system.CrisisResponseMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void PopulationSave_RoundTripsCompletedAndInProgressSpecialistTraining()
        {
            var sim = TowerSimulation.CreateStandardFiveFloor();
            var complete = sim.Population.Persons[0];
            var inProgress = sim.Population.Persons[1];
            complete.Specialization.AdvanceTowards(SpecialistRole.Service, 1f);
            inProgress.Specialization.AdvanceTowards(SpecialistRole.Knowledge, .34f);

            var restored = PopulationState.FromSaveData(sim.Population.ToSaveData(), new DeterministicRandomStream(42));

            Assert.That(restored.GetPerson(complete.Id).Specialization.Role, Is.EqualTo(SpecialistRole.Service));
            Assert.That(restored.GetPerson(inProgress.Id).Specialization.TrainingRole, Is.EqualTo(SpecialistRole.Knowledge));
            Assert.That(restored.GetPerson(inProgress.Id).Specialization.TrainingProgress, Is.EqualTo(.34f).Within(.001f));
        }
    }
}
