using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Domain.Infrastructure;
using OneRoof.Presentation.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class UtilitiesOverlayPresenterTests
    {
        [Test]
        public void UpdateOverlay_StoresProjectionAndVisibility()
        {
            var holder = new GameObject("UtilitiesOverlayPresenterTestHolder");
            try
            {
                var presenter = holder.AddComponent<UtilitiesOverlayPresenter>();
                var overlay = new UtilitiesOverlayProjection(new[] { new UtilitiesFloorProjection(0, 1f, "None", 1f, "None", "None", 0) }, null);
                presenter.UpdateOverlay(overlay); presenter.SetVisible(true);
                Assert.That(presenter.CurrentOverlay, Is.SameAs(overlay));
                Assert.That(presenter.IsVisible, Is.True);
            }
            finally { Object.DestroyImmediate(holder); }
        }

        [Test]
        public void FloorSummary_ShowsOnlySelectedNetworkAndFullLabelRemainsAvailable()
        {
            var floor = new UtilitiesFloorProjection(2, 0.75f, "Overload", 0.5f, "Low pressure", "Blocked", 1);
            var summary = UtilitiesOverlayPresenter.FormatFloorSummary(floor, UtilitiesNetworkLayerPresenter.NetworkKind.Power);
            Assert.That(summary, Does.Contain("Power 75%"));
            Assert.That(summary, Does.Not.Contain("Low pressure"));
            Assert.That(floor.AccessibilityLabel, Does.Contain("Low pressure"));
        }

        [Test]
        public void UndergroundSummary_ReportsFlowForSelectedNetwork()
        {
            var paths = new UndergroundUtilityPathSnapshot(null, null, new[]
            {
                new UndergroundRoomUtilityStatus(1, UndergroundUtilityKind.Power, true, true, UndergroundUtilityCause.None),
                new UndergroundRoomUtilityStatus(2, UndergroundUtilityKind.Power, true, false, UndergroundUtilityCause.SurfaceServiceUnavailable),
                new UndergroundRoomUtilityStatus(1, UndergroundUtilityKind.Water, false, false, UndergroundUtilityCause.DisconnectedPath)
            });
            var overlay = new UtilitiesOverlayProjection(null, null, paths);
            Assert.That(UtilitiesOverlayPresenter.FormatUndergroundSummary(overlay,
                UtilitiesNetworkLayerPresenter.NetworkKind.Power), Does.Contain("1/2 rooms flowing"));
            Assert.That(UtilitiesOverlayPresenter.FormatUndergroundSummary(overlay,
                UtilitiesNetworkLayerPresenter.NetworkKind.Water), Does.Contain("No connected path"));
        }
    }
}
