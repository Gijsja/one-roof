using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Overlays;
using OneRoof.Presentation.Overlays;
using OneRoof.Presentation.Tower;
using OneRoof.UI.Inspectors;
using OneRoof.UI.Modes;
using OneRoof.UI.Prediction;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerPlayableControllerTests
    {
        private GameObject _holder;
        private TowerPlayableController _controller;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_Tower_World");
            _controller = _holder.AddComponent<TowerPlayableController>();
            _controller.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }

            var cam = GameObject.Find("Tower Camera");
            if (cam != null)
            {
                Object.DestroyImmediate(cam);
            }
        }

        [Test]
        public void Controller_InitialState_WiresSubcomponentsAndDefaultsToInspect()
        {
            Assert.That(_controller.TransitSession, Is.Not.Null);
            Assert.That(_controller.ModeSession, Is.Not.Null);
            Assert.That(_controller.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Inspect));

            var modeBar = _holder.GetComponent<ModeShellBarController>();
            Assert.That(modeBar, Is.Not.Null);
            Assert.That(modeBar.Session, Is.SameAs(_controller.ModeSession));

            var overlay = _holder.GetComponent<ElevatorWaitOverlayPresenter>();
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.IsVisible, Is.False, "Overlay must be hidden until Data mode is active.");

            var inspector = _holder.GetComponent<CongestionInspectorCardView>();
            Assert.That(inspector, Is.Not.Null);
            Assert.That(inspector.IsOpen, Is.False);

            var placement = _holder.GetComponent<PlacementPreviewCardView>();
            Assert.That(placement, Is.Not.Null);
            Assert.That(placement.IsOpen, Is.False);
        }

        [Test]
        public void Controller_InitialState_InitializesFourDeepPresenters()
        {
            Assert.That(_controller.StructurePresenter, Is.Not.Null);
            Assert.That(_controller.ElevatorPresenter, Is.Not.Null);
            Assert.That(_controller.RoomPresenter, Is.Not.Null);
            Assert.That(_controller.ResidentPresenter, Is.Not.Null);

            Assert.That(_controller.StructurePresenter.RenderedFloorCount, Is.EqualTo(TowerStructurePresenter.InitialFloorCount));
            Assert.That(_controller.ElevatorPresenter.RenderedShaftFloorCount, Is.EqualTo(TowerStructurePresenter.InitialFloorCount));
            Assert.That(_controller.RoomPresenter.RenderedRoomIds.Count, Is.GreaterThan(0));
            Assert.That(_controller.ResidentPresenter.ResidentCount, Is.EqualTo(TowerPlayableController.InitialResidentCount));
        }

        [Test]
        public void ToggleDataOverlay_EnablesOverlayInRealtime()
        {
            var overlay = _holder.GetComponent<ElevatorWaitOverlayPresenter>();
            Assert.That(overlay.IsVisible, Is.False);

            _controller.ToggleDataOverlay();

            Assert.That(_controller.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Data));
            Assert.That(overlay.IsVisible, Is.True);
            Assert.That(overlay.CurrentOverlay, Is.Not.Null);
            Assert.That(overlay.CurrentOverlay.BottleneckFloor, Is.EqualTo(0));
            Assert.That(overlay.CurrentOverlay.OverallSeverity, Is.EqualTo(CongestionTier.Severe));
        }

        [Test]
        public void InspectBottleneck_OpensInspectorCardWithCauses()
        {
            var inspector = _holder.GetComponent<CongestionInspectorCardView>();
            Assert.That(inspector.IsOpen, Is.False);

            _controller.InspectBottleneck();

            Assert.That(_controller.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Inspect));
            Assert.That(inspector.IsOpen, Is.True);
            Assert.That(inspector.CurrentProjection, Is.Not.Null);
            Assert.That(inspector.CurrentProjection.FloorLevel, Is.EqualTo(0));
            Assert.That(inspector.CurrentProjection.CanDirectRouteToBuild, Is.True);
            Assert.That(inspector.CurrentProjection.ContributingCauses.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ShowPlacementPreview_OpensPredictionCardWithHighConfidence()
        {
            var placement = _holder.GetComponent<PlacementPreviewCardView>();
            Assert.That(placement.IsOpen, Is.False);

            _controller.ShowPlacementPreview();

            Assert.That(_controller.ModeSession.CurrentMode, Is.EqualTo(InteractionMode.Build));
            Assert.That(placement.IsOpen, Is.True);
            Assert.That(placement.CurrentProjection, Is.Not.Null);
            Assert.That(placement.CurrentProjection.IsValid, Is.True);
            Assert.That(placement.CurrentProjection.CurrentCarCount, Is.EqualTo(1));
            Assert.That(placement.CurrentProjection.PredictedCarCount, Is.EqualTo(2));
            Assert.That(placement.CurrentProjection.EstimatedImprovementPercentage, Is.GreaterThan(0f));
        }

        [Test]
        public void OnConfirmElevatorPlacement_AddsElevatorCarAndClosesCard()
        {
            var initialCars = _controller.TransitSession.Projection().Elevators.Count;
            Assert.That(initialCars, Is.EqualTo(1));

            _controller.ShowPlacementPreview();
            var placement = _holder.GetComponent<PlacementPreviewCardView>();
            Assert.That(placement.IsOpen, Is.True);

            _controller.OnConfirmElevatorPlacement();

            var updatedCars = _controller.TransitSession.Projection().Elevators.Count;
            Assert.That(updatedCars, Is.EqualTo(2));
            Assert.That(placement.IsOpen, Is.False);
        }

        [Test]
        public void ResetCommuteSimulation_ResetsStateAndClosesCards()
        {
            _controller.InspectBottleneck();
            var inspector = _holder.GetComponent<CongestionInspectorCardView>();
            Assert.That(inspector.IsOpen, Is.True);

            _controller.TransitSession.AdvanceOneTick();
            Assert.That(_controller.TransitSession.Projection().Tick, Is.GreaterThan(0));

            _controller.ResetCommuteSimulation();

            Assert.That(_controller.TransitSession.Projection().Tick, Is.EqualTo(0));
            Assert.That(inspector.IsOpen, Is.False);
        }
    }
}
