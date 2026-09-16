using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Presentation.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ElevatorWaitOverlayPresenterTests
    {
        private GameObject _holder;
        private ElevatorWaitOverlayPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("OverlayPresenterTestHolder");
            _presenter = _holder.AddComponent<ElevatorWaitOverlayPresenter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void Presenter_VisibilityToggle_UpdatesState()
        {
            Assert.That(_presenter.IsVisible, Is.False);

            _presenter.SetVisible(true);
            Assert.That(_presenter.IsVisible, Is.True);

            _presenter.SetVisible(false);
            Assert.That(_presenter.IsVisible, Is.False);
        }

        [Test]
        public void Presenter_UpdateOverlay_StoresProjection()
        {
            var floorFlows = new[]
            {
                new FloorWaitFlowProjection(0, 50, 40, 20f, CongestionTier.Severe, true, -1f, 1f, "FL 0: 50 QUEUED [SEVERE] • BOTTLENECK"),
                new FloorWaitFlowProjection(1, 0, 0, 0f, CongestionTier.Clear, false, -1f, 0f, "FL 1: 0 QUEUED [CLEAR]")
            };

            var overlay = new ElevatorWaitOverlayProjection(
                tick: 10,
                bottleneckFloor: 0,
                overallSeverity: CongestionTier.Severe,
                floorFlows: floorFlows,
                primaryCause: "Severe bottleneck at Floor 0",
                contributingCauses: new[] { "Elevator throughput insufficient" },
                recommendedAction: "Add capacity in Build mode");

            _presenter.UpdateOverlay(overlay);

            Assert.That(_presenter.CurrentOverlay, Is.SameAs(overlay));
            Assert.That(_presenter.CurrentOverlay.BottleneckFloor, Is.EqualTo(0));
            Assert.That(_presenter.CurrentOverlay.OverallSeverity, Is.EqualTo(CongestionTier.Severe));
        }
    }
}
