using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Domain.Scrutiny;
using OneRoof.Presentation.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ScrutinyOverlayPresenterTests
    {
        [Test]
        public void UpdateOverlay_StoresExplanationReadyProjection()
        {
            var holder = new GameObject("ScrutinyOverlayPresenterTestHolder");
            try
            {
                var presenter = holder.AddComponent<ScrutinyOverlayPresenter>();
                var overlay = new ScrutinyOverlayProjection(.7f, ScrutinyTrend.Rising, .73f, false, new[] { "Recent construction is drawing external attention." });
                presenter.UpdateOverlay(overlay);
                presenter.SetVisible(true);
                Assert.That(presenter.CurrentOverlay, Is.SameAs(overlay));
                Assert.That(presenter.CurrentOverlay.AccessibilityLabel, Does.Contain("RISING"));
                Assert.That(presenter.IsVisible, Is.True);
            }
            finally { Object.DestroyImmediate(holder); }
        }
    }
}
