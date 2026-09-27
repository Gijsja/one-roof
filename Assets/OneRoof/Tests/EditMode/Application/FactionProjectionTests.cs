using NUnit.Framework;
using OneRoof.Application.Social;
using OneRoof.Domain;
using OneRoof.Domain.Social;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class FactionProjectionTests
    {
        [Test]
        public void CapturedProjectionRetainsNamedDriversAndStableResidentIds()
        {
            var simulation = TowerSimulation.CreateStandardFiveFloor(settlementPeriod: 1);
            simulation.AdvanceOneTick();
            var captured = FactionProjectionService.Capture(simulation);
            Assert.That(captured.Factions.Count, Is.EqualTo(4));
            Assert.That(captured.Factions[0].Id, Is.EqualTo(FactionIds.TenantUnion));
            Assert.That(captured.Factions[0].Name, Is.EqualTo("Tenant Union"));
            Assert.That(captured.Factions[0].RepresentativeResidentIds.Count, Is.GreaterThan(0));
            var support = captured.ResidentSupports[0];
            Assert.That(support.ResidentId, Is.GreaterThan(0));
            Assert.That(support.Driver, Is.Not.Empty);
        }
    }
}
