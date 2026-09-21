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
    }
}
