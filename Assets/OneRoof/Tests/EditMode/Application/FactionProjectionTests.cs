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

        [TestCase(-.40f, -.50f, "straining")]
        [TestCase(-.50f, -.40f, "easing toward neutral")]
        [TestCase(.40f, .50f, "strengthening")]
        [TestCase(.50f, .40f, "easing toward neutral")]
        [TestCase(0f, -.04f, "straining")]
        [TestCase(0f, .04f, "strengthening")]
        [TestCase(.30f, .30f, "steady")]
        public void TieTrend_DescribesMovementRelativeToNeutral(float previous, float current, string expected)
        {
            var tie = new ResidentTieProjection(1, 2, current, previous, 10, 10, "test");

            Assert.That(tie.Trend, Is.EqualTo(expected));
        }
    }
}
