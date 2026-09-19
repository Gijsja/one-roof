using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Presentation.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class PopulationOverlayPresenterTests
    {
        [Test]
        public void UpdateOverlay_StoresImmutablePopulationProjectionAndVisibility()
        {
            var holder = new GameObject("PopulationOverlayPresenterTestHolder");
            try
            {
                var presenter = holder.AddComponent<PopulationOverlayPresenter>();
                var overlay = new PopulationOverlayProjection(2, new[] { new PopulationFloorProjection(1, 2, 4, 1, 1, 0, 0, 1, 1) });

                presenter.UpdateOverlay(overlay);
                presenter.SetVisible(true);

                Assert.That(presenter.CurrentOverlay, Is.SameAs(overlay));
                Assert.That(presenter.IsVisible, Is.True);
            }
            finally { Object.DestroyImmediate(holder); }
        }
    }
}
