using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Social;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class FactionTensionOverlayTests
    {
        [Test]
        public void ReliefIsWeightedByFactionSupportersAndRetainsRawAttribution()
        {
            var factions = new List<FactionSummaryProjection> {
                new FactionSummaryProjection("tenant_union", "Tenant Union", .5f, .8f, "rent", 1, 1,
                    new Dictionary<int, int> { { 1, 1 } }, new List<int> { 10 }, .3f, .8f),
                new FactionSummaryProjection("merchant_guild", "Merchant Guild", .8f, .8f, "business", 1, 1,
                    new Dictionary<int, int> { { 1, 1 } }, new List<int> { 11 })
            };
            var social = new FactionProjection(factions, new List<ResidentFactionProjection> {
                new ResidentFactionProjection(10, "tenant_union", .8f, true, "rent", 1),
                new ResidentFactionProjection(11, "merchant_guild", .8f, true, "business", 1)
            });
            var floor = FactionTensionOverlayProjector.Project(1, social, 2).Floors[1];
            Assert.That(floor.AveragePressure, Is.EqualTo(.65f).Within(.001f));
            Assert.That(floor.EarnedRespectRelief, Is.EqualTo(.15f).Within(.001f));
            Assert.That(floor.RawAveragePressure, Is.EqualTo(.8f).Within(.001f));
        }

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
