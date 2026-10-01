using System;
using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ExteriorTopologyLifecycleTests
    {
        private GameObject _exteriorHolder;

        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;

        private BuildingExteriorPresenter _exteriorPresenter;

        [SetUp]
        public void SetUp()
        {
            _exteriorHolder = new GameObject("Test_Exterior_Holder");

            var shader = Shader.Find("OneRoof/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            _worldMaterial = new Material(shader) { name = "Test_WorldMaterial" };
            _colorBlock = new MaterialPropertyBlock();

            _exteriorPresenter = new BuildingExteriorPresenter();
            _exteriorPresenter.Initialize(_exteriorHolder.transform, _worldMaterial, _colorBlock);

        }

        [TearDown]
        public void TearDown()
        {
            _exteriorPresenter?.Clear();

            if (_exteriorHolder != null) Object.DestroyImmediate(_exteriorHolder);
            if (_worldMaterial != null) Object.DestroyImmediate(_worldMaterial);
        }

        private static TowerTopologyProjection CreateTowerTopology(int floorCount, int minX = -14, int maxX = 16)
        {
            var slabs = new Dictionary<int, CellBounds>();
            for (var f = 0; f < floorCount; f++)
            {
                slabs[f] = new CellBounds(f, minX, maxX);
            }
            return new TowerTopologyProjection(slabs);
        }

        private static TowerTopologyProjection CreateSteppedTopology(params (int floor, int minX, int maxX)[] slabs)
        {
            var dict = new Dictionary<int, CellBounds>();
            foreach (var (floor, minX, maxX) in slabs)
            {
                dict[floor] = new CellBounds(floor, minX, maxX);
            }
            return new TowerTopologyProjection(dict);
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_RightSideStepbackGeneratesTerraceSlabAndRailing()
        {
            var topo = CreateSteppedTopology(
                (0, -18, 22),
                (1, -14, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            var railingR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Railing R 0");

            Assert.That(slabR, Is.Not.Null);
            Assert.That(railingR, Is.Not.Null);
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_LeftSideStepbackGeneratesTerraceSlabAndRailing()
        {
            var topo = CreateSteppedTopology(
                (0, -20, 16),
                (1, -14, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabL = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab L 0");
            var railingL = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Railing L 0");

            Assert.That(slabL, Is.Not.Null);
            Assert.That(railingL, Is.Not.Null);
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_TerraceWidthMatchesDeltaBetweenSlabsPlusWallThickness()
        {
            var lower = new CellBounds(0, -14, 22);
            var upper = new CellBounds(1, -14, 16);
            var topo = CreateSteppedTopology(
                (0, lower.MinX, lower.MaxX),
                (1, upper.MinX, upper.MaxX)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            Assert.That(slabR, Is.Not.Null);

            var expectedWidth = (lower.MaxX - upper.MaxX) * 0.5f + BuildingExteriorPresenter.WallThickness;
            Assert.That(slabR.localScale.x, Is.EqualTo(expectedWidth).Within(0.01f));
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_TerraceElevationAlignsWithUpperFloorBaseline()
        {
            var topo = CreateSteppedTopology(
                (0, -14, 20),
                (1, -14, 16)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var slabR = _exteriorPresenter.Root.Find("Setback Terraces/Terrace Slab R 0");
            Assert.That(slabR, Is.Not.Null);

            var expectedBaseY = TowerStructurePresenter.FloorY(1) - 0.74f;
            Assert.That(slabR.localPosition.y, Is.EqualTo(expectedBaseY + 0.08f).Within(0.01f));
        }

        [Test]
        public void T1_F9_SteppedTierSetbacks_NoTerraceCreatedWhenConsecutiveFloorsHaveIdenticalBounds()
        {
            var topo = CreateTowerTopology(4, -14, 16);
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 0; f < 3; f++)
            {
                var slabR = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab R {f}");
                var slabL = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab L {f}");
                Assert.That(slabR, Is.Null, $"No setback terrace should exist on floor {f} with identical bounds.");
                Assert.That(slabL, Is.Null, $"No setback terrace should exist on floor {f} with identical bounds.");
            }
        }

        [Test]
        public void T1_F16_NonDestructivePresentation_EnsureExteriorViewsDoesNotMutateDomainSlabs()
        {
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -14, 16) },
                { 1, new CellBounds(1, -14, 16) }
            };
            var topo = new TowerTopologyProjection(slabs);

            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.That(topo.FloorCount, Is.EqualTo(2));
            Assert.That(topo.TryGetFloorSlab(0, out var slab0), Is.True);
            Assert.That(slab0.MinX, Is.EqualTo(-14));
            Assert.That(slab0.MaxX, Is.EqualTo(16));
        }

        [Test]
        public void T1_F17_Lifecycle_ClearRemovesAllGeneratedGameObjects()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);
            Assert.That(_exteriorPresenter.Root, Is.Not.Null);

            _exteriorPresenter.Clear();

            Assert.That(_exteriorPresenter.Root, Is.Null);
            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(-1));
            Assert.That(_exteriorHolder.transform.childCount, Is.EqualTo(0));
        }

        [Test]
        public void T1_F17_Idempotence_RepeatedCallsWithSameTopologyDoNotDuplicateChildren()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);
            var initialCount = _exteriorPresenter.Root.GetComponentsInChildren<Transform>().Length;

            _exteriorPresenter.EnsureExteriorViews(topo);
            var secondCount = _exteriorPresenter.Root.GetComponentsInChildren<Transform>().Length;

            Assert.That(secondCount, Is.EqualTo(initialCount), "EnsureExteriorViews with identical topology must be completely idempotent.");
        }

        [Test]
        public void T1_F17_ExpansionSafety_ExpandingFloorCountLiftsRooflineCleanly()
        {
            var topo5 = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo5);
            var initialRoofY = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab").localPosition.y;

            var topo10 = CreateTowerTopology(10);
            _exteriorPresenter.EnsureExteriorViews(topo10);
            var newRoofY = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab").localPosition.y;

            var expectedDelta = 5 * TowerStructurePresenter.DefaultFloorHeight; // 5 * 1.75 = 8.75m
            Assert.That(newRoofY - initialRoofY, Is.EqualTo(expectedDelta).Within(0.01f));
        }

        [Test]
        public void T1_F17_StageBoundsSafety_CalculatesNonZeroExtentsAcrossAllAxes()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var bounds = _exteriorPresenter.StageBounds;
            Assert.That(bounds.LeftExteriorZone.size.magnitude, Is.GreaterThan(0f));
            Assert.That(bounds.RightExteriorZone.size.magnitude, Is.GreaterThan(0f));
            Assert.That(bounds.RooftopZone.size.magnitude, Is.GreaterThan(0f));
            Assert.That(bounds.InteriorCutawayEnclosure.size.magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void StructuralRenderers_ShareWorldMaterial_AndGlazingDisablesDepthWrites()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var renderers = _exteriorPresenter.Root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(10));
            foreach (var r in renderers)
            {
                if (r.name.Contains("Light Cone") || r.name == "Modular Window Fenestration" || r.name == "Batched Flank Window Glass")
                    Assert.That(r.sharedMaterial.GetInt("_ZWrite"), Is.EqualTo(0));
                else
                    Assert.That(r.sharedMaterial, Is.EqualTo(_worldMaterial));
            }
        }

        [Test]
        public void T2_Boundary_NullTopologyProjection_SafelyGeneratesDefaultFiveFloorTower()
        {
            _exteriorPresenter.EnsureExteriorViews(null);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(TowerStructurePresenter.InitialFloorCount));
            Assert.That(_exteriorPresenter.Root, Is.Not.Null);
            Assert.That(_exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab"), Is.Not.Null);
        }

        [Test]
        public void T2_Boundary_MinimalOneFloorBuilding_GeneratesFoundationCanopyAndRoofImmediatelyAbove()
        {
            var topo = CreateTowerTopology(1);
            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(1));
            Assert.That(_exteriorPresenter.Root.Find("Left Facade/Left Foundation Plinth"), Is.Not.Null);
            Assert.That(_exteriorPresenter.Root.Find("Right Facade/Lobby Cantilever Canopy"), Is.Not.Null);

            var roofSlab = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            Assert.That(roofSlab, Is.Not.Null);

            var expectedRoofBaseY = TowerStructurePresenter.FloorY(0) + 1.01f;
            Assert.That(roofSlab.localPosition.y, Is.EqualTo(expectedRoofBaseY + BuildingExteriorPresenter.RoofDeckThickness * 0.5f).Within(0.01f));
        }

        [Test]
        public void ThirtyFloorTopology_GeneratesEveryWallAndSetsRoofDeckHeight()
        {
            var topo = CreateTowerTopology(30);
            _exteriorPresenter.EnsureExteriorViews(topo);

            Assert.That(_exteriorPresenter.RenderedFloorCount, Is.EqualTo(30));

            for (var f = 0; f < 30; f++)
            {
                Assert.That(_exteriorPresenter.Root.Find($"Left Facade/Left Wall {f}"), Is.Not.Null, $"Left Wall {f} missing on 30-floor tower.");
            }

            var expectedTopDeckY = TowerStructurePresenter.FloorY(29) + 1.01f + BuildingExteriorPresenter.RoofDeckThickness;
            var deck = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            Assert.That(deck, Is.Not.Null);
            Assert.That(deck.localPosition.y + deck.localScale.y * 0.5f,
                Is.EqualTo(expectedTopDeckY).Within(0.01f));
            Assert.That(_exteriorPresenter.StageBounds.RoofDeckY,
                Is.EqualTo(expectedTopDeckY + BuildingExteriorPresenter.ParapetHeight).Within(0.01f),
                "Outside-stage roof boundary includes the parapet envelope above the structural deck.");
        }

        [Test]
        public void T2_Boundary_NarrowFourCellCrown_RoofFixturesAndParapetsFitWithinConstrainedWidth()
        {
            // 4-cell crown: MinX = -2, MaxX = 1 (width = 4 cells = 2.0m)
            var topo = CreateSteppedTopology(
                (0, -14, 16),
                (1, -2, 1)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            var roofSlab = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            Assert.That(roofSlab, Is.Not.Null);
            Assert.That(roofSlab.localScale.x, Is.GreaterThan(2.0f));

            var gravel = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Gravel Ballast");
            Assert.That(gravel, Is.Not.Null);
            Assert.That(gravel.localScale.x, Is.GreaterThan(0.4f), "Gravel ballast must have positive width even on narrow crown.");
        }

        [Test]
        public void T2_Boundary_MassiveMultiTierSetbacks_GeneratesTerracesAcrossAllFloorTransitions()
        {
            var topo = CreateSteppedTopology(
                (0, -20, 24),
                (1, -16, 20),
                (2, -12, 16),
                (3, -8, 12),
                (4, -4, 8)
            );
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 0; f < 4; f++)
            {
                var slabR = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab R {f}");
                var slabL = _exteriorPresenter.Root.Find($"Setback Terraces/Terrace Slab L {f}");
                Assert.That(slabR, Is.Not.Null, $"Right terrace missing at setback floor {f}.");
                Assert.That(slabL, Is.Not.Null, $"Left terrace missing at setback floor {f}.");
            }
        }
    }
}
