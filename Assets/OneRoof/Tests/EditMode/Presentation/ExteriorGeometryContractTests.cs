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
    public sealed class ExteriorGeometryContractTests
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

        private static float WorldLeft(CellBounds slab) => -2.4f + slab.MinX * 0.5f;
        private static float WorldRight(CellBounds slab) => -2.4f + (slab.MaxX + 1) * 0.5f;

        [Test]
        public void T1_F1_ExteriorWindowGeometry_WallEnvelopeThicknessAndBoundsCalculatedAccurately()
        {
            Assert.That(BuildingExteriorPresenter.WallThickness, Is.EqualTo(0.36f).Within(0.001f));
            Assert.That(BuildingExteriorPresenter.CladdingThickness, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(BuildingExteriorPresenter.SpandrelProtrusion, Is.EqualTo(0.06f).Within(0.001f));
        }

        [Test]
        public void T1_F1_ExteriorWindowCasing_HorizontalSlabOffsetsAlignWithFloorBounds()
        {
            var topo = CreateTowerTopology(5, -14, 16);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var groundLeft = WorldLeft(new CellBounds(0, -14, 16));
            var groundRight = WorldRight(new CellBounds(0, -14, 16));

            var leftWall0 = _exteriorPresenter.Root.Find("Left Facade/Left Wall 0");
            Assert.That(leftWall0, Is.Not.Null);
            Assert.That(leftWall0.localPosition.x, Is.EqualTo(groundLeft - BuildingExteriorPresenter.WallThickness * 0.5f).Within(0.01f));

            var rightWall1 = _exteriorPresenter.Root.Find("Right Facade/Right Wall 1");
            Assert.That(rightWall1, Is.Not.Null);
            Assert.That(rightWall1.localPosition.x, Is.EqualTo(groundRight + BuildingExteriorPresenter.WallThickness * 0.5f).Within(0.01f));
        }

        [Test]
        public void T1_F1_ExteriorWindowRecessedDepth_GlassPaneRecessedBehindCladding()
        {
            _exteriorPresenter.EnsureExteriorViews(CreateTowerTopology(5));
            var glass = _exteriorPresenter.Root.Find("Left Facade/Left Window Glass 1");
            var cladding = _exteriorPresenter.Root.Find("Left Facade/Left Cladding 1");
            var wall = _exteriorPresenter.Root.Find("Left Facade/Left Wall 1");
            Assert.That(glass, Is.Not.Null);
            Assert.That(cladding, Is.Not.Null);
            Assert.That(wall, Is.Not.Null);
            Assert.That(glass.position.z, Is.GreaterThan(cladding.position.z));
            Assert.That(glass.position.z, Is.LessThan(wall.position.z));
        }

        [Test]
        public void UpperFloorWalls_AlignWithFloorBaselinesAndHaveCladdingAndSpandrels()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 1; f < 5; f++)
            {
                var wall = _exteriorPresenter.Root.Find($"Right Facade/Right Wall {f}");
                var cladding = _exteriorPresenter.Root.Find($"Right Facade/Right Cladding {f}");
                var spandrel = _exteriorPresenter.Root.Find($"Right Facade/Right Spandrel {f}");

                Assert.That(wall, Is.Not.Null, $"Right Wall {f} must exist.");
                Assert.That(cladding, Is.Not.Null, $"Right Cladding {f} must exist.");
                Assert.That(spandrel, Is.Not.Null, $"Right Spandrel {f} must exist.");

                var expectedY = TowerStructurePresenter.FloorY(f);
                Assert.That(wall.localPosition.y, Is.EqualTo(expectedY).Within(0.01f));
            }
        }

        [Test]
        public void T1_F1_ExteriorWindowEnvelope_LeftFacadeWallSlicesCreatedForEveryFloor()
        {
            var topo = CreateTowerTopology(6);
            _exteriorPresenter.EnsureExteriorViews(topo);

            for (var f = 0; f < 6; f++)
            {
                var wall = _exteriorPresenter.Root.Find($"Left Facade/Left Wall {f}");
                Assert.That(wall, Is.Not.Null, $"Left Wall {f} must exist.");
                Assert.That(wall.localScale.y, Is.EqualTo(1.75f).Within(0.01f));
            }
        }

        [Test]
        public void RooftopZone_ProvidesMechanicalSpaceAboveDeck()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var stageBounds = _exteriorPresenter.StageBounds;
            Assert.That(stageBounds.RooftopZone.center.y, Is.GreaterThan(stageBounds.RoofDeckY));
            Assert.That(stageBounds.RooftopZone.size.y, Is.GreaterThanOrEqualTo(8.0f), "Rooftop zone must have adequate height clearance for water tanks.");
        }

        [Test]
        public void RoofGeometry_UsesAuthoredParapetAndDeckDimensions()
        {
            Assert.That(BuildingExteriorPresenter.ParapetHeight, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(BuildingExteriorPresenter.RoofDeckThickness, Is.EqualTo(0.28f).Within(0.001f));

            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var roofSlab = _exteriorPresenter.Root.Find("Roofline & Capping/Roof Structural Slab");
            Assert.That(roofSlab, Is.Not.Null);
        }

        [Test]
        public void T1_F6_CoolingTowersAndChillers_RooftopHVACChillerAndCowlingsCreated()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var chiller = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Rooftop HVAC Chiller");
            Assert.That(chiller, Is.Not.Null);

            var fanL = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling L");
            var fanR = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling R");
            Assert.That(fanL, Is.Not.Null);
            Assert.That(fanR, Is.Not.Null);
        }

        [Test]
        public void T1_F6_CoolingTowersAndChillers_HVACFanCowlingsSymmetricallyOffsetFromChillerCenter()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var chiller = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Rooftop HVAC Chiller");
            var fanL = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling L");
            var fanR = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/HVAC Fan Cowling R");

            Assert.That(chiller, Is.Not.Null);
            Assert.That(fanL, Is.Not.Null);
            Assert.That(fanR, Is.Not.Null);

            var deltaL = fanL.localPosition.x - chiller.localPosition.x;
            var deltaR = fanR.localPosition.x - chiller.localPosition.x;

            Assert.That(deltaL, Is.EqualTo(-0.20f).Within(0.01f));
            Assert.That(deltaR, Is.EqualTo(0.20f).Within(0.01f));
        }

        [Test]
        public void T1_F7_RooftopAntennaArrays_CommunicationsMastAndCrossArmCreatedWithCorrectElevation()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var mast = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Communications Mast");
            var crossArm = _exteriorPresenter.Root.Find("Roofline & Capping/Rooftop Fixtures/Mast Cross Arm");

            Assert.That(mast, Is.Not.Null);
            Assert.That(crossArm, Is.Not.Null);

            Assert.That(mast.localScale.y, Is.EqualTo(2.40f).Within(0.01f), "Mast structural height must be 2.40m.");
            Assert.That(crossArm.localScale.x, Is.EqualTo(0.40f).Within(0.01f), "Cross arm width must be 0.40m.");
        }

        [Test]
        public void T1_F11_VerticalUtilityConduits_ExteriorDownspoutsSpanFromRoofToGround()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var leftDownspout = _exteriorPresenter.Root.Find("Left Facade/Left Downspout");
            var rightDownspout = _exteriorPresenter.Root.Find("Right Facade/Right Downspout");

            Assert.That(leftDownspout, Is.Not.Null);
            Assert.That(rightDownspout, Is.Not.Null);

            Assert.That(leftDownspout.localScale.x, Is.EqualTo(0.04f).Within(0.01f));
            Assert.That(leftDownspout.localScale.y, Is.GreaterThan(5.0f));
        }

        [Test]
        public void T1_F11_VerticalUtilityConduits_DownspoutDepthFlushWithCladdingFace()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var leftDownspout = _exteriorPresenter.Root.Find("Left Facade/Left Downspout");
            Assert.That(leftDownspout, Is.Not.Null);
            Assert.That(leftDownspout.localPosition.z, Is.EqualTo(-0.05f).Within(0.01f));
        }

        [Test]
        public void T1_F13_BuildingSignage_LobbyEntrancePortalAndCantileverCanopyCreated()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var portal = _exteriorPresenter.Root.Find("Right Facade/Lobby Entrance Portal Frame");
            var canopy = _exteriorPresenter.Root.Find("Right Facade/Lobby Cantilever Canopy");

            Assert.That(portal, Is.Not.Null);
            Assert.That(canopy, Is.Not.Null);

            Assert.That(canopy.localScale.x, Is.EqualTo(1.40f).Within(0.01f), "Lobby canopy width must be 1.40m.");
            Assert.That(canopy.localScale.y, Is.EqualTo(0.14f).Within(0.01f));
        }

        [Test]
        public void ExteriorStageBounds_ProvideLeftAndRightAirZones()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var bounds = _exteriorPresenter.StageBounds;
            Assert.That(bounds.LeftExteriorZone.size.x, Is.GreaterThanOrEqualTo(10.0f));
            Assert.That(bounds.RightExteriorZone.size.x, Is.GreaterThanOrEqualTo(10.0f));
        }

        [Test]
        public void T1_F16_StrictNonPenetration_ExteriorWallCoreSlicesFlankInteriorWithoutOverlappingCutaway()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var ground = new CellBounds(0, -14, 16);
            var leftBound = WorldLeft(ground);
            var rightBound = WorldRight(ground);

            var wallL = _exteriorPresenter.Root.Find("Left Facade/Left Wall 0");
            var wallR = _exteriorPresenter.Root.Find("Right Facade/Right Wall 1");

            Assert.That(wallL.localPosition.x, Is.LessThan(leftBound), "Left Wall must be strictly to the left of interior cutaway.");
            Assert.That(wallR.localPosition.x, Is.GreaterThan(rightBound), "Right Wall must be strictly to the right of interior cutaway.");
        }

        [Test]
        public void T1_F16_StrictNonPenetration_InteriorEnclosureStageBoundsEnclosesAllRooms()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var enclosure = _exteriorPresenter.StageBounds.InteriorCutawayEnclosure;
            Assert.That(enclosure.size.x, Is.GreaterThan(15.0f));
            Assert.That(enclosure.size.y, Is.GreaterThan(5.0f));
        }

        [Test]
        public void T1_F16_StrictNonPenetration_FoundationPlinthPositionedStrictlyOutsideFloorZeroCutaway()
        {
            var topo = CreateTowerTopology(5);
            _exteriorPresenter.EnsureExteriorViews(topo);

            var plinth = _exteriorPresenter.Root.Find("Left Facade/Left Foundation Plinth");
            Assert.That(plinth, Is.Not.Null);

            var groundLeft = WorldLeft(new CellBounds(0, -14, 16));
            Assert.That(plinth.localPosition.x, Is.LessThan(groundLeft));
        }
    }
}
