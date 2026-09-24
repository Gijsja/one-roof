using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;

namespace OneRoof.Application.Tests.EditMode
{
    [TestFixture]
    public sealed class BusinessHealthOverlayTests
    {
        [Test]
        public void BusinessHealth_GroupsTenantsByFloorAndRefreshesAfterTick()
        {
            var session = new TowerSimulationSession();
            for (var tick = 0; tick < 10; tick++) session.AdvanceOneTick();
            var overlays = new TowerDataOverlays(session);

            var first = overlays.BusinessHealth;
            Assert.That(first.Tenants.Count, Is.GreaterThan(0));
            Assert.That(overlays.BusinessHealth, Is.SameAs(first));

            var totalTenants = 0;
            foreach (var floor in first.Floors)
            {
                Assert.That(floor.Tenants, Is.GreaterThan(0));
                totalTenants += floor.Tenants;
            }
            Assert.That(totalTenants, Is.EqualTo(first.Tenants.Count));

            session.AdvanceOneTick();
            Assert.That(overlays.BusinessHealth, Is.Not.SameAs(first));
        }
    }
}
