using NUnit.Framework;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class TowerInspectionServiceTests
    {
        [Test]
        public void InspectResident_ProjectsLocationNeedsAndWaitState()
        {
            var session = new TowerSimulationSession();
            session.SeedMorningRush();
            var residentId = session.TransitProjection().Residents[0].ResidentId;
            var projection = new TowerInspectionService(session).InspectResident(residentId);

            Assert.That(projection, Is.Not.Null);
            Assert.That(projection.Title, Is.EqualTo($"Resident #{residentId}"));
            Assert.That(projection.Details, Has.Some.Contains("Household:"));
            Assert.That(projection.Details, Has.Some.Contains("Purpose:"));
            Assert.That(projection.Details, Has.Some.Contains("Hunger:"));
            Assert.That(projection.Details, Has.Some.Contains("Specialist role:"));
        }

        [Test]
        public void InspectRoom_ProjectsTopologyAndCurrentOccupancy()
        {
            var session = new TowerSimulationSession();
            var roomId = session.TransitProjection().Residents[0].RoomId.Value;
            var projection = new TowerInspectionService(session).InspectRoom(roomId);

            Assert.That(projection, Is.Not.Null);
            Assert.That(projection.Details, Has.Some.Contains("Occupancy:"));
            Assert.That(projection.Details, Has.Some.Contains("Footprint:"));
        }

        [Test]
        public void ResidentInspectionSnapshot_RemainsStableAcrossSimulationTicks()
        {
            var session = new TowerSimulationSession();
            var id = new EntityId(session.TransitProjection().Residents[0].ResidentId);
            Assert.That(session.TryGetResidentInspection(id, out var before), Is.True);
            var hunger = before.Needs[0].Satisfaction;

            for (var i = 0; i < 10; i++) session.AdvanceOneTick();
            Assert.That(session.TryGetResidentInspection(id, out var after), Is.True);

            Assert.That(before.Needs[0].Satisfaction, Is.EqualTo(hunger));
            Assert.That(after.Needs[0].Satisfaction, Is.LessThan(hunger));
        }

        [Test]
        public void InspectElevatorBank_UsesSnapshotForQueuesAndCars()
        {
            var session = new TowerSimulationSession();
            session.SeedMorningRush();
            var projection = new TowerInspectionService(session).InspectElevatorBank();

            Assert.That(projection.Title, Is.EqualTo("Elevator Bank"));
            Assert.That(projection.Details, Has.Some.Contains("Cars in service:"));
            Assert.That(projection.Details, Has.Some.Contains("Floor 0 queue:"));
        }

        [Test]
        public void InspectResident_ProjectsSocialTraits_ActiveThoughts_AndTrueRelationshipStage()
        {
            var session = new TowerSimulationSession();
            session.SeedMorningRush();
            var residentId = session.TransitProjection().Residents[0].ResidentId;
            var id = new EntityId(residentId);
            Assert.That(session.Population.TryGetPerson(id, out var person), Is.True);

            person.Wellbeing.AddThought(new Domain.Population.ThoughtMemory("Had delightful conversation", 5f, 0, 1000));

            var otherId = session.TransitProjection().Residents[1].ResidentId;
            session.Simulation.Factions.GetOrCreateEdge(id, new EntityId(otherId), 100, "romantic spark", Domain.Social.RelationshipStage.Crush, 0.55f);

            var projection = new TowerInspectionService(session).InspectResident(residentId);

            Assert.That(projection, Is.Not.Null);
            Assert.That(projection.Details, Has.Some.Contains("Social trait:"));
            Assert.That(projection.Details, Has.Some.Contains("Thought: \"Had delightful conversation\" (+5.0 mood"));
            Assert.That(projection.Details, Has.Some.Contains("Crush"));
        }
    }
}
