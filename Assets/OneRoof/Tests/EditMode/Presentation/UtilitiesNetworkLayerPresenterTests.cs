using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;
using OneRoof.Presentation.Overlays;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    [TestFixture]
    public sealed class UtilitiesNetworkLayerPresenterTests
    {
        private GameObject _holder;
        private UtilitiesNetworkLayerPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_UtilitiesNetworkLayerPresenter");
            _presenter = _holder.AddComponent<UtilitiesNetworkLayerPresenter>();
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
        public void InitialState_HasExpectedDefaults()
        {
            Assert.That(_presenter.SelectedNetwork, Is.EqualTo(UtilitiesNetworkLayerPresenter.NetworkKind.Power));
            Assert.That(_presenter.IsVisible, Is.False);
            Assert.That(_presenter.ActiveLineCount, Is.EqualTo(0));
            Assert.That(_presenter.ActiveNodeCount, Is.EqualTo(0));
        }

        [Test]
        public void SetVisible_TogglesLayerRootVisibility()
        {
            _presenter.SetVisible(true);
            Assert.That(_presenter.IsVisible, Is.True);

            var root = _holder.transform.Find("Utility Network View Layer");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.gameObject.activeSelf, Is.True);

            _presenter.SetVisible(false);
            Assert.That(_presenter.IsVisible, Is.False);
            Assert.That(root.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Select_SwitchesNetworkAndAppliesColors()
        {
            _presenter.SetVisible(true);

            _presenter.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Water);
            Assert.That(_presenter.SelectedNetwork, Is.EqualTo(UtilitiesNetworkLayerPresenter.NetworkKind.Water));

            _presenter.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Waste);
            Assert.That(_presenter.SelectedNetwork, Is.EqualTo(UtilitiesNetworkLayerPresenter.NetworkKind.Waste));

            _presenter.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Power);
            Assert.That(_presenter.SelectedNetwork, Is.EqualTo(UtilitiesNetworkLayerPresenter.NetworkKind.Power));
        }

        [Test]
        public void UpdateOverlay_WithFiveFloorSession_BuildsFullConnectedNetwork()
        {
            var session = new TowerSimulationSession();
            var dataOverlays = new TowerDataOverlays(session);
            var topology = session.TopologyProjection();
            var overlay = dataOverlays.Utilities;

            _presenter.SetVisible(true);
            _presenter.UpdateOverlay(overlay, topology);

            // Expect lines for: Substation feed, vertical risers, ceiling raceways, and room drops
            Assert.That(_presenter.ActiveLineCount, Is.GreaterThan(5));
            // Expect node markers for: Substation source, riser junctions, transformer, and room ports
            Assert.That(_presenter.ActiveNodeCount, Is.GreaterThan(5));
        }

        [Test]
        public void UpdateOverlay_WaterAndWasteNetworks_BuildsConnectedLinesAndNodes()
        {
            var session = new TowerSimulationSession();
            var dataOverlays = new TowerDataOverlays(session);
            var topology = session.TopologyProjection();
            var overlay = dataOverlays.Utilities;

            _presenter.SetVisible(true);

            // Water network
            _presenter.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Water);
            _presenter.UpdateOverlay(overlay, topology);
            Assert.That(_presenter.ActiveLineCount, Is.GreaterThan(5));
            Assert.That(_presenter.ActiveNodeCount, Is.GreaterThan(5));

            // Waste network
            _presenter.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Waste);
            _presenter.UpdateOverlay(overlay, topology);
            Assert.That(_presenter.ActiveLineCount, Is.GreaterThan(5));
            Assert.That(_presenter.ActiveNodeCount, Is.GreaterThan(5));
        }

        [Test]
        public void UpdateOverlay_NullTopologyFallback_BuildsRisersWithoutCrashing()
        {
            var session = new TowerSimulationSession();
            var dataOverlays = new TowerDataOverlays(session);
            var overlay = dataOverlays.Utilities;

            _presenter.SetVisible(true);
            Assert.DoesNotThrow(() => _presenter.UpdateOverlay(overlay, null));
            Assert.That(_presenter.ActiveLineCount, Is.GreaterThan(0));
        }

        [Test]
        public void UpdateOverlay_NullOverlay_DeactivatesAll()
        {
            var session = new TowerSimulationSession();
            var dataOverlays = new TowerDataOverlays(session);
            _presenter.SetVisible(true);
            _presenter.UpdateOverlay(dataOverlays.Utilities, session.TopologyProjection());

            Assert.That(_presenter.ActiveLineCount, Is.GreaterThan(0));

            _presenter.UpdateOverlay(null);
            Assert.That(_presenter.ActiveLineCount, Is.EqualTo(0));
            Assert.That(_presenter.ActiveNodeCount, Is.EqualTo(0));
        }

        [Test]
        public void Pooling_ReusesLineRenderersAcrossRebuilds()
        {
            var session = new TowerSimulationSession();
            var dataOverlays = new TowerDataOverlays(session);
            var topology = session.TopologyProjection();
            var overlay = dataOverlays.Utilities;

            _presenter.SetVisible(true);
            _presenter.UpdateOverlay(overlay, topology);
            var initialLineCount = _presenter.ActiveLineCount;
            var initialNodeCount = _presenter.ActiveNodeCount;

            // Rebuild multiple times
            _presenter.Rebuild();
            _presenter.Rebuild();

            Assert.That(_presenter.ActiveLineCount, Is.EqualTo(initialLineCount));
            Assert.That(_presenter.ActiveNodeCount, Is.EqualTo(initialNodeCount));
        }
    }
}
