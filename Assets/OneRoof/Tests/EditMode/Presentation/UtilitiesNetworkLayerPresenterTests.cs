using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Tower;
using EntityId = OneRoof.Domain.Identity.EntityId;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Infrastructure;
using OneRoof.Domain.Topology;
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
                UnityEngine.Object.DestroyImmediate(_holder);
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
        public void UpdateOverlay_WithBuiltFiveFloorNetwork_BuildsConnectedLines()
        {
            ConnectedFixture(out var topology, out var overlay);

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
            ConnectedFixture(out var topology, out var overlay);

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
            ConnectedFixture(out _, out var overlay);

            _presenter.SetVisible(true);
            Assert.DoesNotThrow(() => _presenter.UpdateOverlay(overlay, null));
            Assert.That(_presenter.ActiveLineCount, Is.GreaterThan(0));
        }

        [Test]
        public void UpdateOverlay_NullOverlay_DeactivatesAll()
        {
            ConnectedFixture(out var topology, out var overlay);
            _presenter.SetVisible(true);
            _presenter.UpdateOverlay(overlay, topology);

            Assert.That(_presenter.ActiveLineCount, Is.GreaterThan(0));

            _presenter.UpdateOverlay(null);
            Assert.That(_presenter.ActiveLineCount, Is.EqualTo(0));
            Assert.That(_presenter.ActiveNodeCount, Is.EqualTo(0));
        }

        [Test]
        public void Pooling_ReusesLineRenderersAcrossRebuilds()
        {
            ConnectedFixture(out var topology, out var overlay);

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

        [Test]
        public void BrokenPowerRiser_ShowsBuiltColumnWithoutInventingSourceOrDistribution()
        {
            var riser = new Room(new EntityId(1), ElectricalGridState.RiserContentId,
                new CellBounds(0, 10, 11), null, 0);
            var consumer = new Room(new EntityId(2), new ContentId("home:test"),
                new CellBounds(0, 14, 15), null, 2);
            var topology = Projection(riser, consumer);
            var overlay = new UtilitiesOverlayProjection(new[] {
                new UtilitiesFloorProjection(0, 0f, "NoSubstation", 0f, "NoGroundPump", "NoGroundCollection", 0)
            }, null);

            _presenter.SetVisible(true);
            _presenter.UpdateOverlay(overlay, topology);

            var riserLine = _holder.transform.Find("Utility Network View Layer/Power_Riser_FL0")
                ?.GetComponent<LineRenderer>();
            Assert.That(riserLine, Is.Not.Null);
            Assert.That(riserLine.GetPosition(0).x, Is.EqualTo(-2.4f + 10 * 0.5f + 0.25f).Within(.001f));
            Assert.That(_holder.transform.Find("Utility Network View Layer/Power_SubstationSource"), Is.Null);
            Assert.That(_holder.transform.Find("Utility Network View Layer/Power_SubstationFeed"), Is.Null);
            Assert.That(_holder.transform.Find("Utility Network View Layer/Power_CeilingRaceway_FL0"), Is.Null);
        }

        [Test]
        public void PumpWithoutRiser_ShowsSourceWithoutPhantomWaterPipes()
        {
            var pump = new Room(new EntityId(1), WaterWasteNetworkState.WaterPumpContentId,
                new CellBounds(0, 0, 3), null, 120);
            var consumer = new Room(new EntityId(2), new ContentId("home:test"),
                new CellBounds(0, 14, 15), null, 2);
            var topology = Projection(pump, consumer);
            var overlay = new UtilitiesOverlayProjection(new[] {
                new UtilitiesFloorProjection(0, 0f, "NoSubstation", 0f, "DisconnectedRiser", "NoGroundCollection", 0)
            }, null);

            _presenter.SetVisible(true);
            _presenter.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Water);
            _presenter.UpdateOverlay(overlay, topology);

            Assert.That(_holder.transform.Find("Utility Network View Layer/Water_PumpSource"), Is.Not.Null);
            Assert.That(_holder.transform.Find("Utility Network View Layer/Water_PumpFeed"), Is.Null);
            Assert.That(_holder.transform.Find("Utility Network View Layer/Water_SubfloorPipe_FL0"), Is.Null);
        }

        [Test]
        public void UndergroundSegments_OnlyFlowingSegmentsUseActiveMaterial()
        {
            var from = new UtilityPathPoint(UtilityPathPointKind.UndergroundCell, 16, 0);
            var to = new UtilityPathPoint(UtilityPathPointKind.UndergroundCell, 16, 1);
            var power = new UndergroundUtilitySegment("power:cell:16:0:16:1",
                UndergroundUtilityKind.Power, from, to, true, true);
            var water = new UndergroundUtilitySegment("water:cell:16:0:16:1",
                UndergroundUtilityKind.Water, from, to, true, false);
            var paths = new UndergroundUtilityPathSnapshot(new[] { power }, new[] { water },
                Array.Empty<UndergroundRoomUtilityStatus>());
            var overlay = new UtilitiesOverlayProjection(Array.Empty<UtilitiesFloorProjection>(), null, paths);

            _presenter.SetVisible(true);
            _presenter.UpdateOverlay(overlay, Projection());
            var powerLine = _holder.transform.Find("Utility Network View Layer/Underground_Power_power:cell:16:0:16:1")
                ?.GetComponent<LineRenderer>();
            Assert.That(powerLine, Is.Not.Null);
            Assert.That(powerLine.sharedMaterial, Is.SameAs(_presenter.ActiveLineMaterial));
            Assert.That(powerLine.GetPosition(0).x, Is.EqualTo(-.9f).Within(.001f));

            _presenter.Select(UtilitiesNetworkLayerPresenter.NetworkKind.Water);
            var waterLine = _holder.transform.Find("Utility Network View Layer/Underground_Water_water:cell:16:0:16:1")
                ?.GetComponent<LineRenderer>();
            Assert.That(waterLine, Is.Not.Null);
            Assert.That(waterLine.sharedMaterial, Is.Not.SameAs(_presenter.ActiveLineMaterial));
        }

        private static TowerTopologyProjection Projection(params Room[] rooms)
        {
            var slabs = new Dictionary<int, CellBounds> { { 0, new CellBounds(0, -14, 17) } };
            var byId = new Dictionary<EntityId, Room>();
            foreach (var room in rooms) byId.Add(room.Id, room);
            return new TowerTopologyProjection(slabs, byId);
        }

        private static void ConnectedFixture(out TowerTopologyProjection topology,
            out UtilitiesOverlayProjection overlay)
        {
            var slabs = new Dictionary<int, CellBounds>();
            var rooms = new Dictionary<EntityId, Room>();
            var floors = new List<UtilitiesFloorProjection>();
            var nextId = 1;
            for (var floor = 0; floor < 5; floor++)
            {
                slabs.Add(floor, new CellBounds(floor, 0, 31));
                Add(floor, ElectricalGridState.RiserContentId, 10, 11);
                Add(floor, ElectricalGridState.TransformerContentId, 14, 15);
                Add(floor, WaterWasteNetworkState.WaterRiserContentId, 18, 19);
                Add(floor, WaterWasteNetworkState.WasteChuteContentId, 22, 23);
                Add(floor, new ContentId("home:test"), 26, 27);
                if (floor == 0)
                {
                    Add(floor, ElectricalGridState.SubstationContentId, 0, 3);
                    Add(floor, WaterWasteNetworkState.WaterPumpContentId, 4, 7);
                    Add(floor, WaterWasteNetworkState.WasteCollectionContentId, 8, 9);
                }
                floors.Add(new UtilitiesFloorProjection(floor, 1f, "None", 1f, "None", "None", 0,
                    10, 18, 22, true, true, true));
            }
            topology = new TowerTopologyProjection(slabs, rooms);
            overlay = new UtilitiesOverlayProjection(floors, null);

            void Add(int floor, ContentId content, int minX, int maxX)
            {
                var id = new EntityId(nextId++);
                rooms.Add(id, new Room(id, content, new CellBounds(floor, minX, maxX), null, 2));
            }
        }
    }
}
