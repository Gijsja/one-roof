using NUnit.Framework;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class TowerInspectionServiceTests
    {
        [Test]
        public void InspectResident_ProjectsLocationNeedsAndWaitState()
        {
            var session = new TowerSimulationSession();
            session.SeedMorningRush();
            var residentId = session.Population.Persons[0].Id.Value;
            var projection = new TowerInspectionService(session).InspectResident(residentId);

            Assert.That(projection, Is.Not.Null);
            Assert.That(projection.Title, Is.EqualTo($"Resident #{residentId}"));
            Assert.That(projection.Details, Has.Some.Contains("Household:"));
            Assert.That(projection.Details, Has.Some.Contains("Hunger:"));
        }

        [Test]
        public void InspectRoom_ProjectsTopologyAndCurrentOccupancy()
        {
            var session = new TowerSimulationSession();
            var roomId = session.Population.Persons[0].HomeRoomId.Value;
            var projection = new TowerInspectionService(session).InspectRoom(roomId);

            Assert.That(projection, Is.Not.Null);
            Assert.That(projection.Details, Has.Some.Contains("Occupancy:"));
            Assert.That(projection.Details, Has.Some.Contains("Footprint:"));
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
    }
}
