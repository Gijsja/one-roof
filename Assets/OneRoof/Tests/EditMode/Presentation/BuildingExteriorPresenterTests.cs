using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class BuildingExteriorPresenterTests
    {
        private GameObject _holder;
        private BuildingExteriorPresenter _presenter;
        private Material _material;
        private MaterialPropertyBlock _colorBlock;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_Exterior_Holder");
            _material = new Material(Shader.Find("Sprites/Default"));
            _colorBlock = new MaterialPropertyBlock();
            _presenter = new BuildingExteriorPresenter();
            _presenter.Initialize(_holder.transform, _material, _colorBlock);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Clear();
            if (_material != null) Object.DestroyImmediate(_material);
            if (_holder != null) Object.DestroyImmediate(_holder);
        }

        [Test]
        public void EnsureExteriorViews_WithNullTopology_CreatesFiveFloorExteriorEnvelope()
        {
            _presenter.EnsureExteriorViews(null);

            Assert.That(_presenter.RenderedFloorCount, Is.EqualTo(TowerStructurePresenter.InitialFloorCount));
            Assert.That(_presenter.Root, Is.Not.Null);

            var root = _presenter.Root;
            Assert.That(root.Find("Left Facade"), Is.Not.Null);
            Assert.That(root.Find("Right Facade"), Is.Not.Null);
            Assert.That(root.Find("Roofline & Capping"), Is.Not.Null);

            // Left Foundation Plinth and Downspout
            Assert.That(root.Find("Left Facade/Left Foundation Plinth"), Is.Not.Null);
            Assert.That(root.Find("Left Facade/Left Downspout"), Is.Not.Null);

            // Right Lobby Canopy and Entrance Frame
            Assert.That(root.Find("Right Facade/Lobby Entrance Portal Frame"), Is.Not.Null);
            Assert.That(root.Find("Right Facade/Lobby Cantilever Canopy"), Is.Not.Null);

            // Roof Structural Slab and Rooftop Fixtures
            Assert.That(root.Find("Roofline & Capping/Roof Structural Slab"), Is.Not.Null);
            Assert.That(root.Find("Roofline & Capping/Left Parapet Wall"), Is.Not.Null);
            Assert.That(root.Find("Roofline & Capping/Right Parapet Wall"), Is.Not.Null);
            Assert.That(root.Find("Roofline & Capping/Rooftop Fixtures/Elevator Penthouse Housing"), Is.Not.Null);
            Assert.That(root.Find("Roofline & Capping/Rooftop Fixtures/Communications Mast"), Is.Not.Null);
        }

        [Test]
        public void EnsureExteriorViews_FloorExpansion_LiftsRooflineAndSpawnsWalls()
        {
            _presenter.EnsureExteriorViews(null); // 5 floors
            var roofline = _presenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            var initialRoofY = roofline.localPosition.y;

            // Expand to 8 floors
            var slabs = new Dictionary<int, CellBounds>();
            for (var f = 0; f < 8; f++)
            {
                slabs[f] = new CellBounds(f, -14, 16);
            }
            var projection = new TowerTopologyProjection(slabs);

            _presenter.EnsureExteriorViews(projection);

            Assert.That(_presenter.RenderedFloorCount, Is.EqualTo(8));
            var newRoofline = _presenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            var newRoofY = newRoofline.localPosition.y;

            var expectedDeltaY = 3 * TowerStructurePresenter.DefaultFloorHeight; // 3 * 1.75 = 5.25m
            Assert.That(newRoofY - initialRoofY, Is.EqualTo(expectedDeltaY).Within(0.01f));

            // Verify newly added wall slices exist
            Assert.That(_presenter.Root.Find("Left Facade/Left Wall 7"), Is.Not.Null);
            Assert.That(_presenter.Root.Find("Right Facade/Right Wall 7"), Is.Not.Null);
        }

        [Test]
        public void EnsureExteriorViews_SteppedTerrace_CreatesTerraceWhenGroundSlabIsWider()
        {
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -18, 22) },
                { 1, new CellBounds(1, -14, 16) }
            };
            var projection = new TowerTopologyProjection(slabs);

            _presenter.EnsureExteriorViews(projection);

            Assert.That(_presenter.Root.Find("Setback Terraces/Terrace Slab R 0"), Is.Not.Null);
            Assert.That(_presenter.Root.Find("Setback Terraces/Terrace Railing R 0"), Is.Not.Null);
            Assert.That(_presenter.Root.Find("Setback Terraces/Terrace Slab L 0"), Is.Not.Null);
            Assert.That(_presenter.Root.Find("Setback Terraces/Terrace Railing L 0"), Is.Not.Null);
        }

        [Test]
        public void StageBounds_ProvidesValidExteriorAndInteriorBounds()
        {
            _presenter.EnsureExteriorViews(null);

            var bounds = _presenter.StageBounds;
            Assert.That(bounds.LeftExteriorZone.size.x, Is.GreaterThan(0f));
            Assert.That(bounds.RightExteriorZone.size.x, Is.GreaterThan(0f));
            Assert.That(bounds.RooftopZone.size.y, Is.GreaterThan(0f));
            Assert.That(bounds.InteriorCutawayEnclosure.size.x, Is.GreaterThan(0f));
            Assert.That(bounds.RoofDeckY, Is.GreaterThan(TowerStructurePresenter.FloorY(4)));
        }

        [Test]
        public void Clear_RemovesAllGeneratedObjects()
        {
            _presenter.EnsureExteriorViews(null);
            Assert.That(_presenter.Root, Is.Not.Null);

            _presenter.Clear();

            Assert.That(_presenter.Root, Is.Null);
            Assert.That(_presenter.RenderedFloorCount, Is.EqualTo(-1));
            Assert.That(_holder.transform.childCount, Is.EqualTo(0));
        }
    }
}
