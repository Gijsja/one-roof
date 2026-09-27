using NUnit.Framework;
using OneRoof.Application.Overlays;
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
    }
}
