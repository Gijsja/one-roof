using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Social;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class FactionTensionOverlayTests
    {
        [Test]
        public void ProjectsRegionalSupportWithNoDataAndTextCause()
        {
            var factions = new List<FactionSummaryProjection>
            {
                new FactionSummaryProjection("tenant_union", "Tenant Union", .6f, .4f, "rent burden", 2, 2,
                    new Dictionary<int, int> { { 1, 2 } }, new List<int> { 10, 11 })
            };
            var supporters = new List<ResidentFactionProjection>
            {
                new ResidentFactionProjection(10, "tenant_union", .8f, true, "rent burden", 1),
                new ResidentFactionProjection(11, "tenant_union", .6f, true, "rent burden", 1)
            };
            var social = new FactionProjection(factions, supporters);
            var overlay = FactionTensionOverlayProjector.Project(1440, social, 3);

            Assert.That(overlay.Floors[0].Tier, Is.EqualTo("NO DATA"));
            Assert.That(overlay.Floors[1].AveragePressure, Is.EqualTo(.7f).Within(.001f));
            Assert.That(overlay.Floors[1].AccessibilityLabel, Does.Contain("rent burden"));
            Assert.That(overlay.Floors[1].AccessibilityLabel, Does.Contain("!!!"));
        }
    }
}
