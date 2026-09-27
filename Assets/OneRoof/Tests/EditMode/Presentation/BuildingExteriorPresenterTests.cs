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
        public void GroundDressingAndEntranceFollowExpandedSlab()
        {
            var slabs = new Dictionary<int, CellBounds> { { 0, new CellBounds(0, -14, 17) } };
            _presenter.EnsureExteriorViews(new TowerTopologyProjection(slabs));
            var root = _presenter.Root;
            var soil = root.Find("Ground Dressing/Foundation Soil");
            var green = root.Find("Ground Dressing/Ground Green Edge");
            var entrance = root.Find("Right Facade/Entrance Glass");
            Assert.That(soil, Is.Not.Null);
            Assert.That(green, Is.Not.Null);
            Assert.That(entrance, Is.Not.Null);
            var bay = root.Find("Ground Dressing/West Bay Mullion 1");
            Assert.That(bay, Is.Not.Null);
            Assert.That(bay.localPosition.z, Is.GreaterThan(0.7f).And.LessThan(1f));
            var oldWidth = soil.localScale.x;
            var oldEntranceX = entrance.localPosition.x;

            slabs[0] = new CellBounds(0, -20, 23);
            _presenter.EnsureExteriorViews(new TowerTopologyProjection(slabs));
            Assert.That(soil.localScale.x, Is.EqualTo(oldWidth).Within(0.001f),
                "The underground earth board stays independent when the tower slab expands.");
            Assert.That(entrance.localPosition.x, Is.EqualTo(oldEntranceX + 3f).Within(0.001f));
        }

        [Test]
        public void GroundExpansionKeepsUpperDownspoutOnUpperFacade()
        {
            var slabs = new Dictionary<int, CellBounds>
            {
                { 0, new CellBounds(0, -20, 22) },
                { 1, new CellBounds(1, -14, 16) }
            };
            _presenter.EnsureExteriorViews(new TowerTopologyProjection(slabs));
            var upperWall = _presenter.Root.Find("Left Facade/Left Wall 1");
            var downspout = _presenter.Root.Find("Left Facade/Left Downspout");
            Assert.That(downspout.localPosition.x, Is.EqualTo(
                upperWall.localPosition.x - BuildingExteriorPresenter.WallThickness * 0.5f - 0.02f).Within(0.001f));
            Assert.That(downspout.localPosition.y - downspout.localScale.y * 0.5f,
                Is.GreaterThan(TowerStructurePresenter.FloorY(0)));
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
            Assert.That(_presenter.LeftWindowFrames.Count, Is.EqualTo(0));
            Assert.That(_presenter.LeftWindowGlasses.Count, Is.EqualTo(0));
            Assert.That(_presenter.RightWindowFrames.Count, Is.EqualTo(0));
            Assert.That(_presenter.RightWindowGlasses.Count, Is.EqualTo(0));
            Assert.That(_presenter.VolumetricCones.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnsureExteriorViews_ExteriorWindows_RenderWithCorrectDepthAndNaming()
        {
            _presenter.EnsureExteriorViews(null);

            // Left Facade Window hierarchy (floor 0 through 4)
            for (var f = 0; f < 5; f++)
            {
                var frame = _presenter.Root.Find($"Left Facade/Left Window Frame {f}");
                var sill = _presenter.Root.Find($"Left Facade/Left Window Sill {f}");
                var glass = _presenter.Root.Find($"Left Facade/Left Window Glass {f}");
                var wall = _presenter.Root.Find($"Left Facade/Left Wall {f}");
                var cladding = _presenter.Root.Find($"Left Facade/Left Cladding {f}");

                Assert.That(frame, Is.Not.Null, $"Left Window Frame {f} must exist");
                Assert.That(sill, Is.Not.Null, $"Left Window Sill {f} must exist");
                Assert.That(glass, Is.Not.Null, $"Left Window Glass {f} must exist");
                Assert.That(wall, Is.Not.Null, $"Preserved Left Wall {f} must exist");
                Assert.That(cladding, Is.Not.Null, $"Preserved Left Cladding {f} must exist");

                // Z-Depth validation
                Assert.That(glass.localPosition.z, Is.EqualTo(0.00f).Within(0.001f), "Glass must be recessed at Z = 0.00m");
                Assert.That(frame.localPosition.z, Is.EqualTo(-0.05f).Within(0.001f), "Frame header must be at Z = -0.05m");
                Assert.That(sill.localPosition.z, Is.EqualTo(-0.05f).Within(0.001f), "Sill must be at Z = -0.05m");
                Assert.That(cladding.localPosition.z, Is.EqualTo(-0.04f).Within(0.001f), "Cladding must be at Z = -0.04m");
                Assert.That(wall.localPosition.z, Is.EqualTo(0.10f).Within(0.001f), "Wall core must be at Z = 0.10m");
            }

            // Right Facade Window hierarchy (floors 1 through 4)
            for (var f = 1; f < 5; f++)
            {
                var frame = _presenter.Root.Find($"Right Facade/Right Window Frame {f}");
                var sill = _presenter.Root.Find($"Right Facade/Right Window Sill {f}");
                var glass = _presenter.Root.Find($"Right Facade/Right Window Glass {f}");
                var wall = _presenter.Root.Find($"Right Facade/Right Wall {f}");

                Assert.That(frame, Is.Not.Null, $"Right Window Frame {f} must exist");
                Assert.That(sill, Is.Not.Null, $"Right Window Sill {f} must exist");
                Assert.That(glass, Is.Not.Null, $"Right Window Glass {f} must exist");
                Assert.That(wall, Is.Not.Null, $"Preserved Right Wall {f} must exist");

                Assert.That(glass.localPosition.z, Is.EqualTo(0.00f).Within(0.001f));
                Assert.That(frame.localPosition.z, Is.EqualTo(-0.05f).Within(0.001f));
                Assert.That(sill.localPosition.z, Is.EqualTo(-0.05f).Within(0.001f));
            }
        }

        [Test]
        public void EnsureExteriorViews_VolumetricLightCones_GeneratedUnderExteriorVolumetricLighting()
        {
            _presenter.EnsureExteriorViews(null);

            var volRoot = _presenter.Root.Find("Exterior Volumetric Lighting");
            Assert.That(volRoot, Is.Not.Null, "Exterior Volumetric Lighting root must exist under Root");

            // Left light cones (0..4) and Right light cones (1..4)
            Assert.That(_presenter.Root.Find("Exterior Volumetric Lighting/Left Light Cone 0"), Is.Not.Null);
            Assert.That(_presenter.Root.Find("Exterior Volumetric Lighting/Right Light Cone 1"), Is.Not.Null);

            for (var f = 0; f < 5; f++)
            {
                var cone = _presenter.Root.Find($"Exterior Volumetric Lighting/Left Light Cone {f}");
                Assert.That(cone, Is.Not.Null);
                Assert.That(cone.localPosition.z, Is.EqualTo(-0.20f).Within(0.001f), "Cone must be positioned at Z = -0.20m");

                var filter = cone.GetComponent<MeshFilter>();
                Assert.That(filter, Is.Not.Null);
                var mesh = filter.sharedMesh;
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.vertexCount, Is.EqualTo(4));

                // Vertex colors: inner vertices at window must have alpha 1.0, terminating in open air must have alpha 0.0
                var colors = mesh.colors;
                Assert.That(colors[0].a, Is.EqualTo(1.0f).Within(0.001f), "Inner top vertex alpha must be 1.0");
                Assert.That(colors[3].a, Is.EqualTo(1.0f).Within(0.001f), "Inner bottom vertex alpha must be 1.0");
                Assert.That(colors[1].a, Is.EqualTo(0.0f).Within(0.001f), "Terminating top vertex alpha must be 0.0");
                Assert.That(colors[2].a, Is.EqualTo(0.0f).Within(0.001f), "Terminating bottom vertex alpha must be 0.0");

                var renderer = cone.GetComponent<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null);
                var mat = renderer.sharedMaterial;
                Assert.That(mat, Is.Not.Null);
                if (mat.HasProperty("_ZWrite"))
                {
                    Assert.That(mat.GetInt("_ZWrite"), Is.EqualTo(0), "Transparent volumetric material must have ZWrite = 0");
                }
            }
        }

        [Test]
        public void UpdateLighting_DayAndNightTransition_UpdatesWindowGlassAndConeEmissiveGlow()
        {
            _presenter.EnsureExteriorViews(null);

            // 1. Daytime (12:00 PM)
            var dayPhase = new OneRoof.Domain.Time.DayPhase(1, 12, 0, false);
            _presenter.UpdateLighting(dayPhase);

            var block = new MaterialPropertyBlock();

            // All windows and cones should show daytime colors
            var leftGlass0 = _presenter.Root.Find("Left Facade/Left Window Glass 0").GetComponent<MeshRenderer>();
            leftGlass0.GetPropertyBlock(block);
            var glassDayColor = block.GetColor("_BaseColor");
            Assert.That(glassDayColor.r, Is.EqualTo(BuildingExteriorPresenter.WindowDayColor.r).Within(0.01f));
            Assert.That(glassDayColor.g, Is.EqualTo(BuildingExteriorPresenter.WindowDayColor.g).Within(0.01f));
            Assert.That(glassDayColor.b, Is.EqualTo(BuildingExteriorPresenter.WindowDayColor.b).Within(0.01f));

            var leftCone0 = _presenter.Root.Find("Exterior Volumetric Lighting/Left Light Cone 0").GetComponent<MeshRenderer>();
            leftCone0.GetPropertyBlock(block);
            var coneDayColor = block.GetColor("_BaseColor");
            Assert.That(coneDayColor.a, Is.EqualTo(BuildingExteriorPresenter.ConeDayColor.a).Within(0.01f));

            // 2. Nighttime (00:00 AM)
            var nightPhase = new OneRoof.Domain.Time.DayPhase(1, 0, 0, true);
            _presenter.UpdateLighting(nightPhase);

            // Floor 0 on Left: (0 * 7 + 3) % 5 = 3 != 0 -> occupied (lit)
            leftGlass0.GetPropertyBlock(block);
            var glassNightLit = block.GetColor("_BaseColor");
            Assert.That(glassNightLit.r, Is.EqualTo(BuildingExteriorPresenter.WindowNightLitColor.r).Within(0.01f));
            Assert.That(glassNightLit.g, Is.EqualTo(BuildingExteriorPresenter.WindowNightLitColor.g).Within(0.01f));
            Assert.That(glassNightLit.b, Is.EqualTo(BuildingExteriorPresenter.WindowNightLitColor.b).Within(0.01f));

            leftCone0.GetPropertyBlock(block);
            var coneNightLit = block.GetColor("_BaseColor");
            Assert.That(coneNightLit.r, Is.EqualTo(BuildingExteriorPresenter.ConeNightLitColor.r).Within(0.01f));
            Assert.That(coneNightLit.a, Is.EqualTo(BuildingExteriorPresenter.ConeNightLitColor.a).Within(0.01f));

            // Floor 1 on Left: (1 * 7 + 3) % 5 = 0 == 0 -> dark (unoccupied)
            var leftGlass1 = _presenter.Root.Find("Left Facade/Left Window Glass 1").GetComponent<MeshRenderer>();
            leftGlass1.GetPropertyBlock(block);
            var glassNightDark = block.GetColor("_BaseColor");
            Assert.That(glassNightDark.r, Is.EqualTo(BuildingExteriorPresenter.WindowNightDarkColor.r).Within(0.01f));

            var leftCone1 = _presenter.Root.Find("Exterior Volumetric Lighting/Left Light Cone 1").GetComponent<MeshRenderer>();
            leftCone1.GetPropertyBlock(block);
            var coneNightDark = block.GetColor("_BaseColor");
            Assert.That(coneNightDark.a, Is.EqualTo(0.0f).Within(0.001f), "Dark floor cone must have 0 alpha at night");
        }

        [Test]
        public void EnsureExteriorViews_ExteriorElements_DoNotPenetrateInteriorCutaway()
        {
            _presenter.EnsureExteriorViews(null);

            const float worldLeft = -9.4f;  // -2.4 + (-14 * 0.5)
            const float worldRight = 6.1f;  // -2.4 + (16 + 1) * 0.5

            // Left Facade elements must have X <= worldLeft
            foreach (var frame in _presenter.LeftWindowFrames)
            {
                Assert.That(frame.transform.localPosition.x, Is.LessThanOrEqualTo(worldLeft));
            }
            foreach (var sill in _presenter.LeftWindowSills)
            {
                Assert.That(sill.transform.localPosition.x, Is.LessThanOrEqualTo(worldLeft));
            }
            foreach (var glass in _presenter.LeftWindowGlasses)
            {
                Assert.That(glass.transform.localPosition.x, Is.LessThanOrEqualTo(worldLeft));
            }

            // Right Facade elements must have X >= worldRight
            foreach (var frame in _presenter.RightWindowFrames)
            {
                Assert.That(frame.transform.localPosition.x, Is.GreaterThanOrEqualTo(worldRight));
            }
            foreach (var sill in _presenter.RightWindowSills)
            {
                Assert.That(sill.transform.localPosition.x, Is.GreaterThanOrEqualTo(worldRight));
            }
            foreach (var glass in _presenter.RightWindowGlasses)
            {
                Assert.That(glass.transform.localPosition.x, Is.GreaterThanOrEqualTo(worldRight));
            }

            // Volumetric Cones vertices must not cross into (worldLeft, worldRight)
            for (var f = 0; f < 5; f++)
            {
                var cone = _presenter.Root.Find($"Exterior Volumetric Lighting/Left Light Cone {f}");
                var mesh = cone.GetComponent<MeshFilter>().sharedMesh;
                foreach (var v in mesh.vertices)
                {
                    Assert.That(v.x, Is.LessThanOrEqualTo(worldLeft), $"Left cone vertex {v.x} must be outside worldLeft");
                }
            }

            for (var f = 1; f < 5; f++)
            {
                var cone = _presenter.Root.Find($"Exterior Volumetric Lighting/Right Light Cone {f}");
                var mesh = cone.GetComponent<MeshFilter>().sharedMesh;
                foreach (var v in mesh.vertices)
                {
                    Assert.That(v.x, Is.GreaterThanOrEqualTo(worldRight), $"Right cone vertex {v.x} must be outside worldRight");
                }
            }
        }
    }
}
