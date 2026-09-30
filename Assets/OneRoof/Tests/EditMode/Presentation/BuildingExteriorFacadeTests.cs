using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    [TestFixture]
    public sealed class BuildingExteriorFacadeTests
    {
        private GameObject _holder;
        private BuildingExteriorPresenter _presenter;
        private Material _worldMaterial;
        private MaterialPropertyBlock _colorBlock;
        private TowerTopologyProjection _topology30;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_Exterior_Holder");
            _worldMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Mock World Material" };
            _worldMaterial.SetInt("_ZWrite", 1);
            _colorBlock = new MaterialPropertyBlock();

            _presenter = new BuildingExteriorPresenter();
            _presenter.Initialize(_holder.transform, _worldMaterial, _colorBlock);

            // Construct 30-floor reference tower topology (GoldStandard scale)
            var slabs = new Dictionary<int, CellBounds>();
            for (var f = 0; f < 30; f++)
            {
                slabs[f] = new CellBounds(f, -20, 17); // [-12.4m, 6.6m]
            }
            _topology30 = new TowerTopologyProjection(slabs);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
            if (_worldMaterial != null) Object.DestroyImmediate(_worldMaterial);
            if (_holder != null) Object.DestroyImmediate(_holder);
        }

        [Test]
        public void F1_FrontFacadeEnvelope_CreatedUnderRoot_DedicatedHierarchy()
        {
            _presenter.EnsureExteriorViews(_topology30);

            Assert.That(_presenter.Root, Is.Not.Null);
            Assert.That(_presenter.FrontFacadeRoot, Is.Not.Null);
            Assert.That(_presenter.FrontFacadeRoot.parent, Is.EqualTo(_presenter.Root));
            Assert.That(_presenter.FrontFacadeRoot.name, Is.EqualTo("Front Facade Envelope"));

            // Must NOT place front facade elements into Left Facade or Right Facade
            var leftFacade = _presenter.Root.Find("Left Facade");
            var rightFacade = _presenter.Root.Find("Right Facade");
            Assert.That(leftFacade, Is.Not.Null);
            Assert.That(rightFacade, Is.Not.Null);

            Assert.That(leftFacade.Find("Front Facade Envelope"), Is.Null);
            Assert.That(rightFacade.Find("Front Facade Envelope"), Is.Null);
            Assert.That(leftFacade.Find("Curtain Wall Framing"), Is.Null);
            Assert.That(rightFacade.Find("Curtain Wall Framing"), Is.Null);
            Assert.That(leftFacade.Find("Transparent Vitrines (Light Cone)"), Is.Null);
            Assert.That(rightFacade.Find("Transparent Vitrines (Light Cone)"), Is.Null);
        }

        [Test]
        public void F1_CurtainWallFramingAndSpandrels_Opaque_ZDepthAndMaterial()
        {
            _presenter.EnsureExteriorViews(_topology30);

            Assert.That(_presenter.FramingObject, Is.Not.Null);
            Assert.That(_presenter.FramingMesh, Is.Not.Null);

            var renderer = _presenter.FramingObject.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterial, Is.EqualTo(_worldMaterial));
            Assert.That(renderer.sharedMaterial.GetInt("_ZWrite"), Is.EqualTo(1));

            // Verify Z-depth = -0.36m across all framing vertices
            var vertices = _presenter.FramingMesh.vertices;
            Assert.That(vertices.Length, Is.GreaterThan(100));
            for (var i = 0; i < vertices.Length; i++)
            {
                Assert.That(vertices[i].z, Is.EqualTo(-0.36f).Within(0.001f),
                    $"Framing vertex {i} Z must be exactly -0.36m");
            }
        }

        [Test]
        public void F2_ScenicElevatorVitrine_SpansAll30Floors_CorrectXBoundsAndTransparency()
        {
            _presenter.EnsureExteriorViews(_topology30);

            Assert.That(_presenter.VitrinesObject, Is.Not.Null);
            Assert.That(_presenter.VitrineMesh, Is.Not.Null);

            var renderer = _presenter.VitrinesObject.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null);

            // Glazing material must have ZWrite = 0, Transparent queue (3000)
            Assert.That(renderer.sharedMaterial.GetInt("_ZWrite"), Is.EqualTo(0));
            Assert.That(renderer.sharedMaterial.renderQueue, Is.EqualTo(3000));

            // Verify base alpha = 0.15f
            var effectiveAlpha = _presenter.GetEffectiveVitrineAlpha(BuildingExteriorPresenter.VitrineType.ScenicElevator);
            Assert.That(effectiveAlpha, Is.EqualTo(0.15f).Within(0.001f));

            // Elevator Vitrine anchor & bounds
            var elevatorAnchor = _presenter.FrontFacadeRoot.Find("Scenic Elevator Vitrine");
            Assert.That(elevatorAnchor, Is.Not.Null);
            Assert.That(elevatorAnchor.localPosition.x, Is.EqualTo(-1.90f).Within(0.01f));
            Assert.That(elevatorAnchor.localPosition.z, Is.EqualTo(-0.345f).Within(0.01f));

            // Verify vertices contain elevator shaft boundaries [-2.40f, -1.40f]
            var vertices = _presenter.VitrineMesh.vertices;
            var foundMinX = false;
            var foundMaxX = false;
            for (var i = 0; i < vertices.Length; i++)
            {
                if (Mathf.Abs(vertices[i].x - (-2.40f)) < 0.01f) foundMinX = true;
                if (Mathf.Abs(vertices[i].x - (-1.40f)) < 0.01f) foundMaxX = true;
                Assert.That(vertices[i].z, Is.InRange(-0.35f, -0.34f),
                    $"Vitrine vertex {i} Z must be in [-0.35m, -0.34m]");
            }
            Assert.That(foundMinX, Is.True, "Vitrine mesh must contain elevator left border x = -2.40f");
            Assert.That(foundMaxX, Is.True, "Vitrine mesh must contain elevator right border x = -1.40f");
        }

        [Test]
        public void F3_CirculationHallwayGlazingRibbons_Spans30Floors_CorrectYBoundsAndAlpha()
        {
            _presenter.EnsureExteriorViews(_topology30);

            // Effective alpha must be 0.16f
            var effectiveAlpha = _presenter.GetEffectiveVitrineAlpha(BuildingExteriorPresenter.VitrineType.HallwayRibbon);
            Assert.That(effectiveAlpha, Is.EqualTo(0.16f).Within(0.001f));

            var anchor = _presenter.FrontFacadeRoot.Find("Circulation Hallway Vitrines");
            Assert.That(anchor, Is.Not.Null);

            // Check floor 0, 10, 20, 29 hallway ribbons in vitrine mesh
            var testFloors = new[] { 0, 10, 20, 29 };
            var vertices = _presenter.VitrineMesh.vertices;

            foreach (var f in testFloors)
            {
                var floorY = TowerStructurePresenter.FloorY(f);
                var expectedMinY = floorY - 0.68f;
                var expectedMaxY = floorY + 0.22f;

                var foundMinY = false;
                var foundMaxY = false;
                for (var i = 0; i < vertices.Length; i++)
                {
                    if (Mathf.Abs(vertices[i].y - expectedMinY) < 0.01f) foundMinY = true;
                    if (Mathf.Abs(vertices[i].y - expectedMaxY) < 0.01f) foundMaxY = true;
                }

                Assert.That(foundMinY, Is.True, $"Floor {f} hallway ribbon must have minY = {expectedMinY:F2}");
                Assert.That(foundMaxY, Is.True, $"Floor {f} hallway ribbon must have maxY = {expectedMaxY:F2}");
            }
        }

        [Test]
        public void F4_GroundLevelStorefrontVitrines_Floor0_BoundsAndAlpha()
        {
            _presenter.EnsureExteriorViews(_topology30);

            // Effective alpha must be 0.14f
            var effectiveAlpha = _presenter.GetEffectiveVitrineAlpha(BuildingExteriorPresenter.VitrineType.Storefront);
            Assert.That(effectiveAlpha, Is.EqualTo(0.14f).Within(0.001f));

            var storefrontAnchor = _presenter.FrontFacadeRoot.Find("Ground Storefront Vitrines");
            Assert.That(storefrontAnchor, Is.Not.Null);

            // Storefront follows the ground slab with height 1.15m
            Assert.That(storefrontAnchor.localScale.x, Is.EqualTo(19.0f).Within(0.01f));
            Assert.That(storefrontAnchor.localScale.y, Is.EqualTo(1.15f).Within(0.01f));

            var groundY = TowerStructurePresenter.FloorY(0);
            var expectedMinY = groundY - 0.62f;
            var expectedMaxY = expectedMinY + 1.15f;

            var vertices = _presenter.VitrineMesh.vertices;
            var foundLeft = false;
            var foundRight = false;
            var foundMinY = false;
            var foundMaxY = false;

            for (var i = 0; i < vertices.Length; i++)
            {
                if (Mathf.Abs(vertices[i].x - (-12.4f)) < 0.01f) foundLeft = true;
                if (Mathf.Abs(vertices[i].x - 6.6f) < 0.01f) foundRight = true;
                if (Mathf.Abs(vertices[i].y - expectedMinY) < 0.01f) foundMinY = true;
                if (Mathf.Abs(vertices[i].y - expectedMaxY) < 0.01f) foundMaxY = true;
            }

            Assert.That(foundLeft, Is.True, "Storefront vitrine must reach the ground slab left edge");
            Assert.That(foundRight, Is.True, "Storefront vitrine must reach the ground slab right edge");
            Assert.That(foundMinY, Is.True, "Storefront vitrine must have bottom y = groundY - 0.62f");
            Assert.That(foundMaxY, Is.True, "Storefront vitrine must have top y = groundY + 0.53f");
        }

        [Test]
        public void F5_DynamicWindowIllumination_DayNightAndOccupancy_SmoothTransitions()
        {
            _presenter.EnsureExteriorViews(_topology30);

            // 1. Daytime (12:00 PM) -> All fenestration shows WindowDayColor
            var dayPhase = new DayPhase(1, 12, 0, false);
            _presenter.UpdateLighting(dayPhase);

            var dayColor = _presenter.GetFenestrationColor(1, 0);
            Assert.That(dayColor.r, Is.EqualTo(BuildingExteriorPresenter.WindowDayColor.r).Within(0.01f));
            Assert.That(dayColor.g, Is.EqualTo(BuildingExteriorPresenter.WindowDayColor.g).Within(0.01f));
            Assert.That(dayColor.b, Is.EqualTo(BuildingExteriorPresenter.WindowDayColor.b).Within(0.01f));
            Assert.That(dayColor.a, Is.EqualTo(BuildingExteriorPresenter.WindowDayColor.a).Within(0.01f));

            // 2. Nighttime (00:00 AM) -> Occupied rooms show warm amber, unoccupied show dark navy
            var nightPhase = new DayPhase(1, 0, 0, true);
            _presenter.UpdateLighting(nightPhase);

            // Find an occupied bay and an unoccupied bay on Floor 0 or 1
            // Formula: (floor * 7 + bay * 3 + 1) % 5 != 0
            // Floor 0, Bay 0: (0 + 0 + 1) % 5 = 1 != 0 -> occupied (lit)
            // Floor 0, Bay 3: (0 + 9 + 1) % 5 = 0 == 0 -> unoccupied (dark)
            var litColor = _presenter.GetFenestrationColor(0, 0);
            Assert.That(litColor.r, Is.EqualTo(BuildingExteriorPresenter.WindowNightLitColor.r).Within(0.01f));
            Assert.That(litColor.g, Is.EqualTo(BuildingExteriorPresenter.WindowNightLitColor.g).Within(0.01f));
            Assert.That(litColor.b, Is.EqualTo(BuildingExteriorPresenter.WindowNightLitColor.b).Within(0.01f));

            var darkColor = _presenter.GetFenestrationColor(0, 3);
            Assert.That(darkColor.r, Is.EqualTo(BuildingExteriorPresenter.WindowNightDarkColor.r).Within(0.01f));
            Assert.That(darkColor.g, Is.EqualTo(BuildingExteriorPresenter.WindowNightDarkColor.g).Within(0.01f));
            Assert.That(darkColor.b, Is.EqualTo(BuildingExteriorPresenter.WindowNightDarkColor.b).Within(0.01f));

            // 3. Dawn ramp: 6.0h (halfway between 5.5h night and 6.5h day -> smoothNight = 0.5f)
            var dawnPhase = new DayPhase(1, 6, 0, false);
            _presenter.UpdateLighting(dawnPhase);

            var dawnColor = _presenter.GetFenestrationColor(0, 0);
            var expectedDawnR = Mathf.Lerp(BuildingExteriorPresenter.WindowDayColor.r, BuildingExteriorPresenter.WindowNightLitColor.r, 0.5f);
            Assert.That(dawnColor.r, Is.EqualTo(expectedDawnR).Within(0.02f));
        }

        [Test]
        public void Crossfade_SetFacadeEnvelopeAlpha_CullingAndDrawCalls()
        {
            _presenter.EnsureExteriorViews(_topology30);

            // Full opacity
            _presenter.SetFacadeEnvelopeAlpha(1.0f);
            Assert.That(_presenter.FrontFacadeRoot.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.FacadeEnvelopeAlpha, Is.EqualTo(1.0f).Within(0.001f));

            // Partial opacity
            _presenter.SetFacadeEnvelopeAlpha(0.5f);
            Assert.That(_presenter.FrontFacadeRoot.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.FacadeEnvelopeAlpha, Is.EqualTo(0.5f).Within(0.001f));
            var elevatorAlpha = _presenter.GetEffectiveVitrineAlpha(BuildingExteriorPresenter.VitrineType.ScenicElevator);
            Assert.That(elevatorAlpha, Is.EqualTo(0.5f * 0.15f).Within(0.001f));

            // Culling threshold <= 0.001f disables root GameObject (0 draw calls)
            _presenter.SetFacadeEnvelopeAlpha(0.0005f);
            Assert.That(_presenter.FrontFacadeRoot.gameObject.activeSelf, Is.False,
                "Front Facade Envelope must be inactive when alpha <= 0.001f to eliminate all draw calls");

            _presenter.SetFacadeEnvelopeAlpha(0.0f);
            Assert.That(_presenter.FrontFacadeRoot.gameObject.activeSelf, Is.False);

            // Reactivating restores active state
            _presenter.SetFacadeEnvelopeAlpha(0.85f);
            Assert.That(_presenter.FrontFacadeRoot.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.FacadeEnvelopeAlpha, Is.EqualTo(0.85f).Within(0.001f));
        }

        [Test]
        public void Performance_DrawCalls_AtMostThreeDrawCallsAcross30Floors()
        {
            _presenter.EnsureExteriorViews(_topology30);

            // Assert that Front Facade Envelope adds at most 3 MeshRenderers across all 30 floors
            var renderers = _presenter.FrontFacadeRoot.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.EqualTo(3),
                "Front Facade Envelope must use exactly 3 combined meshes (framing, fenestration, vitrines) for 3 draw calls");

            Assert.That(_presenter.FramingObject, Is.Not.Null);
            Assert.That(_presenter.FenestrationObject, Is.Not.Null);
            Assert.That(_presenter.VitrinesObject, Is.Not.Null);
        }

        [Test]
        public void BackwardCompatibility_ExistingFlankTestsAndSingleDrawCallAssertion()
        {
            _presenter.EnsureExteriorViews(_topology30);

            // Flank window frames must still have x <= worldLeft and x >= worldRight
            const float worldLeft = -12.4f;
            const float worldRight = 6.6f;

            foreach (var frame in _presenter.LeftWindowFrames)
            {
                Assert.That(frame.transform.localPosition.x, Is.LessThanOrEqualTo(worldLeft));
            }
            foreach (var frame in _presenter.RightWindowFrames)
            {
                Assert.That(frame.transform.localPosition.x, Is.GreaterThanOrEqualTo(worldRight));
            }

            // T1_F17 Single Draw Call Material Sharing assertion across entire Root:
            // Renderers with 'Light Cone' have ZWrite = 0; all other structural quads share _worldMaterial
            var allRenderers = _presenter.Root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(allRenderers.Length, Is.GreaterThan(10));

            foreach (var r in allRenderers)
            {
                if (r.name.Contains("Light Cone") || r.name == "Modular Window Fenestration" || r.name == "Batched Flank Window Glass")
                {
                    Assert.That(r.sharedMaterial.GetInt("_ZWrite"), Is.EqualTo(0),
                        $"Renderer {r.name} with 'Light Cone' must have ZWrite = 0");
                }
                else
                {
                    Assert.That(r.sharedMaterial, Is.EqualTo(_worldMaterial),
                        $"Renderer {r.name} must share _worldMaterial");
                }
            }
        }
    }
}
